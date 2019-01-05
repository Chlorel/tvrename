// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using FileSystemInfo = Alphaleonis.Win32.Filesystem.FileSystemInfo;

namespace TVRename
{
    internal class FindNewShowsInDownloadFolders : ScanActivity
    {
        public FindNewShowsInDownloadFolders(TVDoc doc) : base(doc)
        {
        }

        protected override void Check(SetProgressDelegate prog, ICollection<ShowItem> showList,TVDoc.ScanSettings settings)
        {
            //for each directory in settings directory
            //for each file in directory
            //for each saved show (order by recent)
            //does show match selected file?
            //if so add series to list of series scanned
            if (!Active())
            {
                LOGGER.Info("Not looking for new shows as 'Auto-Add' is turned off");
                return;
            }

            //Dont support unattended mode
            if (settings.Unattended)
            {
                LOGGER.Info("Not looking for new shows as app is unattended");
                return;
            }

            List<string> possibleShowNames = new List<string>();

            foreach (string dirPath in TVSettings.Instance.DownloadFolders)
            {
                LOGGER.Info("Parsing {0} for new shows", dirPath);
                if (!Directory.Exists(dirPath)) continue;
                try
                {
                    foreach (string filePath in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
                    {
                        if (!File.Exists(filePath)) continue;

                        FileInfo fi = new FileInfo(filePath);

                        if (fi.IgnoreFile()) continue;

                        if (!LookForSeries(fi)) possibleShowNames.Add(fi.RemoveExtension() + ".");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    LOGGER.Warn(ex, $"Could not access files in {dirPath}");
                }
            }

            List<ShowItem> addedShows = new List<ShowItem>();

            foreach (string hint in possibleShowNames)
            {
                //if hint doesn't match existing added shows
                if (LookForSeries(hint, addedShows))
                {
                    LOGGER.Info($"Ignoring {hint} as it matches existing shows.");
                    continue;
                }

                //If the hint contains certain terms then we'll ignore it
                if (IgnoreHint(hint))
                {
                    LOGGER.Info($"Ignoring {hint} as it is in the ignore list (from Settings).");
                    continue;
                }

                //If the hint contains certain terms then we'll ignore it
                if (TVSettings.Instance.IgnoredAutoAddHints.Contains(hint))
                {
                    LOGGER.Info(
                        $"Ignoring {hint} as it is in the list of ignored terms the user has selected to ignore from prior Auto Adds.");

                    continue;
                }

                //Remove any (nnnn) in the hint - probably a year
                string refinedHint = Regex.Replace(hint, @"\(\d{4}\)", "");

                //Remove anything we can from hint to make it cleaner and hence more likely to match
                refinedHint = Helpers.RemoveSeriesEpisodeIndicators(refinedHint, MDoc.Library.SeasonWords());

                if (string.IsNullOrWhiteSpace(refinedHint))
                {
                    LOGGER.Info($"Ignoring {hint} as it refines to nothing.");
                    continue;
                }

                //If there are no LibraryFolders then we cant use the simplified UI
                if (TVSettings.Instance.LibraryFolders.Count == 0)
                {
                    MessageBox.Show(
                        "Please add some monitor (library) folders under 'Bulk Add Shows' to use the 'Auto Add' functionity (Alternatively you can add them or turn it off in settings).",
                        "Can't Auto Add Show", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return;
                }

                LOGGER.Info("****************");
                LOGGER.Info("Auto Adding New Show");

                TheTVDB.Instance.GetLock("AutoAddShow");
                //popup dialog
                AutoAddShow askForMatch = new AutoAddShow(refinedHint);

                DialogResult dr = askForMatch.ShowDialog();
                TheTVDB.Instance.Unlock("AutoAddShow");
                if (dr == DialogResult.OK)
                {
                    //If added add show to collection
                    addedShows.Add(askForMatch.ShowItem);
                }
                else if (dr == DialogResult.Abort)
                {
                    LOGGER.Info("Skippng Auto Add Process");
                    break;
                }
                else if (dr == DialogResult.Ignore)
                {
                    LOGGER.Info($"Permenantly Ignoring 'Auto Add' for: {hint}");
                    TVSettings.Instance.IgnoredAutoAddHints.Add(hint);
                }
                else LOGGER.Info($"Cancelled Auto adding new show {hint}");
            }

            if (addedShows.Count <= 0) return;

            MDoc.Library.AddRange(addedShows);
            MDoc.ShowAddedOrEdited(true);
            LOGGER.Info("Added new shows called: {0}", string.Join(",", addedShows.Select(s => s.ShowName)));

            //add each new show into the shows being scanned
            foreach (ShowItem si in addedShows)
            {
                showList.Add(si);
            }
        }

        public override bool Active() => TVSettings.Instance.AutoSearchForDownloadedFiles;

        private static bool IgnoreHint(string hint)
        {
            return TVSettings.Instance.AutoAddMovieTermsArray.Any(term =>
                hint.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        private bool LookForSeries(FileSystemInfo name) => LookForSeries(name, MDoc.Library.Values);

        private static bool LookForSeries(FileSystemInfo test, IEnumerable<ShowItem> shows)
        {
            return shows.Any(si => si.NameMatch(test));
        }

        private static bool LookForSeries(string test, IEnumerable<ShowItem> shows)
        {
            return shows.Any(si => si.NameMatch(test));
        }
    }
}
