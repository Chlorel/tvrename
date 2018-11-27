// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
// 

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TVRename
{
    class FindNewShowsInLibrary : ScanActivity
    {
        public FindNewShowsInLibrary(TVDoc doc) : base(doc)
        {
        }

        public override void Check(SetProgressDelegate prog, int startpct, int totPct, ICollection<ShowItem> showList, TVDoc.ScanSettings settings)
        {
            BulkAddManager bam = new BulkAddManager(mDoc);
            bam.CheckFolders(settings.Token, prog,false);
            foreach (FoundFolder folder in bam.AddItems)
            {
                if (settings.Token.IsCancellationRequested)
                    break;

                if (folder.CodeKnown)
                    continue;

                BulkAddManager.GuessShowItem(folder, mDoc.Library);

                if (folder.CodeKnown)
                    continue;

                FolderMonitorEdit ed = new FolderMonitorEdit(folder);
                if ((ed.ShowDialog() != DialogResult.OK) || (ed.Code == -1))
                    continue;

                folder.TVDBCode = ed.Code;
            }

            if (!bam.AddItems.Any(s => s.CodeKnown)) return;

            bam.AddAllToMyShows();
            Logger.Info("Added new shows called: {0}", string.Join(",", bam.AddItems.Where(s => s.CodeKnown).Select(s => s.Folder)));

            mDoc.SetDirty();
            mDoc.DoDownloadsFG();
            mDoc.DoWhenToWatch(true);

            mDoc.WriteUpcoming();
            mDoc.WriteRecent();
        }

        public override bool Active() => TVSettings.Instance.DoBulkAddInScan;
    }
}
