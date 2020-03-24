// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// Copyright (c) TV Rename. This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
// 

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

// Talk to the TVmaze web API, and get tv series info

// Hierarchy is:
//   TVmaze -> Series (class SeriesInfo) -> Seasons (class Season) -> Episodes (class Episode)

namespace TVRename.TVmaze
{
    // ReSharper disable once InconsistentNaming
    public class LocalCache : iTVSource
    {
        private FileInfo cacheFile;

        private ConcurrentDictionary<int, ExtraEp> removeEpisodeIds; // IDs of episodes that should be removed

        public static readonly object SERIES_LOCK = new object();
        private ConcurrentDictionary<int, int> forceReloadOn;

        private readonly ConcurrentDictionary<int, SeriesInfo> series = new ConcurrentDictionary<int, SeriesInfo>();

        // ReSharper disable once InconsistentNaming
        public string CurrentDLTask;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        //We are using the singleton design pattern
        //http://msdn.microsoft.com/en-au/library/ff650316.aspx

        private static volatile LocalCache IntenalInstance;
        private static readonly object SyncRoot = new object();

        [NotNull]
        public static LocalCache Instance
        {
            get
            {
                if (IntenalInstance is null)
                {
                    lock (SyncRoot)
                    {
                        if (IntenalInstance is null)
                        {
                            IntenalInstance = new LocalCache();
                        }
                    }
                }

                return IntenalInstance;
            }
        }

        public string LastErrorMessage { get; set; }

        public bool LoadOk;

        public void Setup([CanBeNull] FileInfo loadFrom, [NotNull] FileInfo cache, CommandLineArgs cla)
        {
            System.Diagnostics.Debug.Assert(cache != null);
            cacheFile = cache;

            LastErrorMessage = "";
            forceReloadOn = new ConcurrentDictionary<int, int>();

            LoadOk = loadFrom is null || CachePersistor.LoadCache(loadFrom, this);
        }

        public bool Connect(bool showErrorMsgBox) => true;

        public void SaveCache()
        {
            lock (SERIES_LOCK)
            {
                CachePersistor.SaveCache(series, cacheFile, 0);
            }
        }

        private bool DoWeForceReloadFor(int code)
        {
            return forceReloadOn.ContainsKey(code) || !series.ContainsKey(code);
        }

        public bool EnsureUpdated([NotNull] SeriesSpecifier s, bool bannersToo)
        {
            if (s.Provider != ShowItem.ProviderType.TVmaze)
            {
                throw new SourceConsistencyException($"Asked to update {s.Name} from TV Maze, but the Id is not for TV maze.", ShowItem.ProviderType.TVmaze);
            }
            Say($"Downloading {s.Name} from TVmaze");
            try
            {
                SeriesInfo downloadedSi = API.GetSeriesDetails(s);

                if (downloadedSi.TvMazeCode != s.TvMazeSeriesId && s.TvMazeSeriesId ==-1)
                {
                    lock (SERIES_LOCK)
                    {
                        series.TryRemove(-1, out _);
                    }
                }

                lock (SERIES_LOCK)
                {
                    AddSeriesToCache(downloadedSi);
                }
            }
            catch (SourceConsistencyException sce)
            {
                Logger.Error(sce.Message);
                return true;
            }

            return true;
        }

        private void AddSeriesToCache([NotNull] SeriesInfo si)
        {
            int id = si.TvMazeCode;
            lock (SERIES_LOCK)
            {
                if (series.ContainsKey(id))
                {
                    series[id].Merge(si, -1);
                }
                else
                {
                    series[id] = si;
                }
            }
        }

        public bool GetUpdates(bool showErrorMsgBox, CancellationToken cts, [NotNull] IEnumerable<SeriesSpecifier> ss)
        {
            Say("Validating TVmaze cache");
            foreach (SeriesSpecifier downloadShow in ss.Where(downloadShow => !HasSeries(downloadShow.TvMazeSeriesId)))
            {
                AddPlaceholderSeries(downloadShow);
            }

            Say("Updates list from TVmaze");
            IEnumerable<KeyValuePair<string,long>> updateTimes = API.GetShowUpdates();

            Say("Processing updates from TVmaze");
            foreach (KeyValuePair<string, long> showUpdateTime in updateTimes)
            {
                if (!cts.IsCancellationRequested)
                {
                    int showId = int.Parse(showUpdateTime.Key);

                    if (showId > 0 && HasSeries(showId))
                    {
                        SeriesInfo x = GetSeries(showId);
                        if (!(x is null))
                        {
                            if (x.SrvLastUpdated < showUpdateTime.Value)
                            {
                                x.Dirty = true;
                            }
                        }
                        else
                        {
                            Logger.Fatal("");
                        }
                    }
                }
                else
                {
                    SayNothing();
                    return false;
                }
            }
/*

            foreach (SeriesInfo si in series.Values.Where(info => info.Dirty))
            {
                if (!cts.IsCancellationRequested)
                {
                    try
                    {
                        Say($"Downloading {si.Name} from TVmaze");
                        SeriesInfo newSi = API.GenerateSeriesInfo(si);

                        Say($"Downloading {newSi.Name} from TVmaze");
                        si.Merge(newSi, -1);
                    }
                    catch (SourceConsistencyException sce)
                    {
                        Logger.Error(sce.Message);
                    }
                }
                else
                {
                    SayNothing();
                    return false;
                }
            }*/

            SayNothing();
            return true;
        }

        private void AddPlaceholderSeries([NotNull] SeriesSpecifier ss)
            => AddPlaceholderSeries(ss.TvdbSeriesId, ss.TvMazeSeriesId, ss.Name, ss.CustomLanguageCode);

        private void SayNothing() => Say(string.Empty);
        private void Say(string s)
        {
            CurrentDLTask = s;
            if (s.HasValue())
            {
                Logger.Info("Status on screen updated: {0}", s);
            }
        }

        public void UpdatesDoneOk()
        {            
        }

        public SeriesInfo GetSeries(string showName, bool showErrorMsgBox) => throw new NotImplementedException(); //todo when we can offer sarch for TV Maze

        [CanBeNull]
        public SeriesInfo GetSeries(int id) => HasSeries(id) ? series[id] : null;

        public bool HasSeries(int id) => series.ContainsKey(id);

        public void Tidy(ICollection<ShowItem> libraryValues)
        {
            // remove any shows from thetvdb that aren't in My Shows
            List<int> removeList = new List<int>();

            lock (SERIES_LOCK)
            {
                foreach (KeyValuePair<int, SeriesInfo> kvp in series)
                {
                    bool found = libraryValues.Any(si => si.TVmazeCode == kvp.Key);
                    if (!found)
                    {
                        removeList.Add(kvp.Key);
                    }
                }

                foreach (int i in removeList)
                {
                    ForgetShow(i);
                }
            }

            SaveCache();
        }

        public void ForgetEverything()
        {
            lock (SERIES_LOCK)
            {
                series.Clear();
            }

            SaveCache();
            Logger.Info($"Forget the whole TVMaze everything");
        }

        public void ForgetShow(int id)
        {
            lock (SERIES_LOCK)
            {
                if (series.ContainsKey(id))
                {
                    series.TryRemove(id, out _);
                }
            }
        }

        public void ForgetShow(int tvdb,int tvmaze, bool makePlaceholder, bool useCustomLanguage, string langCode)
        {
            lock (SERIES_LOCK)
            {
                if (series.ContainsKey(tvmaze))
                {
                    series.TryRemove(tvmaze, out SeriesInfo oldSeries);
                    string name = oldSeries.Name;
                    if (makePlaceholder)
                    {
                        if (useCustomLanguage)
                        {
                            AddPlaceholderSeries(tvdb,tvmaze, name, langCode);
                        }
                        else
                        {
                            AddPlaceholderSeries(tvdb, tvmaze, name);
                        }

                        forceReloadOn.TryAdd(tvmaze, tvmaze);
                    }
                }
                else
                {
                    if (tvmaze > 0)
                    {
                        AddPlaceholderSeries(tvdb, tvmaze, "");
                    }
                }
            }
        }

        private void AddPlaceholderSeries(int tvdb, int tvmaze, [CanBeNull] string name)
        {
            series[tvmaze] = new SeriesInfo(name ?? string.Empty, tvdb,tvmaze) { Dirty = true };
        }

        private void AddPlaceholderSeries(int tvdb, int tvmaze, [CanBeNull] string name, string customLanguageCode)
        {
            series[tvmaze] = new SeriesInfo(name ?? string.Empty, tvdb,tvmaze, customLanguageCode) { Dirty = true };
        }

        public void UpdateSeries([NotNull] SeriesInfo si)
        {
            lock (SERIES_LOCK)
            {
                series[si.TvMazeCode] = si;
            }
        }

        public void AddOrUpdateEpisode([NotNull] Episode e)
        {
            lock (SERIES_LOCK)
            {
                if (!series.ContainsKey(e.SeriesId))
                {
                    throw new SourceConsistencyException(
                        $"Can't find the series to add the episode to. EpId:{e.EpisodeId} SeriesId:{e.SeriesId} {e.Name}", ShowItem.ProviderType.TVmaze);
                }

                SeriesInfo ser = series[e.SeriesId];

                ser.AddEpisode(e);
            }
        }

        public void AddBanners(int seriesId, IEnumerable<Banner> seriesBanners)
        {
            lock (SERIES_LOCK)
            {
                if (series.ContainsKey(seriesId))
                {
                    foreach (Banner b in seriesBanners)
                    {
                        if (!series.ContainsKey(b.SeriesId))
                        {
                            throw new SourceConsistencyException(
                                $"Can't find the series to add the banner {b.BannerId} to. {seriesId},{b.SeriesId}", ShowItem.ProviderType.TVmaze);
                        }

                        SeriesInfo ser = series[b.SeriesId];

                        ser.AddOrUpdateBanner(b);
                    }

                    series[seriesId].BannersLoaded = true;
                }
                else
                {
                    Logger.Warn($"Banners were found for series {seriesId} - Ignoring them.");
                }
            }
        }

        public void LatestUpdateTimeIs(string time)
        {
        }

        public Language PreferredLanguage => throw new NotImplementedException();

        public ConcurrentDictionary<int,SeriesInfo> CachedData => series;
        public Language GetLanguageFromCode(string customLanguageCode) => throw new NotImplementedException();
    }
}
