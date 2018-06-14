// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
// 
namespace TVRename
{
    using System.Windows.Forms;

    public class LVResults
    {
        #region WhichResults enum

        public enum WhichResults
        {
            Checked,
            Selected,
            All
        }

        #endregion

        public bool AllSameType;

        public System.Collections.Generic.List<ActionCopyMoveRename> CopyMove;
        public int Count;
        public System.Collections.Generic.List<ActionDownloadImage> Download;
        public ItemList FlatList;
        public System.Collections.Generic.List<ItemMissing> Missing;
        public System.Collections.Generic.List<ActionNFO> NFO;
        public System.Collections.Generic.List<ActionPyTivoMeta> PyTivoMeta;
        public System.Collections.Generic.List<ActionRSS> RSS;
        public System.Collections.Generic.List<ActionCopyMoveRename> Rename;
        //public System.Collections.Generic.List<ItemuTorrenting> uTorrenting;

        public LVResults(ListView lv, bool isChecked) // if not checked, then selected items
        {
            Go(lv, isChecked ? WhichResults.Checked : WhichResults.Selected);
        }

        public LVResults(ListView lv, WhichResults which)
        {
            Go(lv, which);
        }

        public void Go(ListView lv, WhichResults which)
        {
            //this.uTorrenting = new System.Collections.Generic.List<ItemuTorrenting>();
            Missing = new System.Collections.Generic.List<ItemMissing>();
            RSS = new System.Collections.Generic.List<ActionRSS>();
            CopyMove = new System.Collections.Generic.List<ActionCopyMoveRename>();
            Rename = new System.Collections.Generic.List<ActionCopyMoveRename>();
            Download = new System.Collections.Generic.List<ActionDownloadImage>();
            NFO = new System.Collections.Generic.List<ActionNFO>();
            PyTivoMeta = new System.Collections.Generic.List<ActionPyTivoMeta>();
            FlatList = new ItemList();

            System.Collections.Generic.List<ListViewItem> sel = new System.Collections.Generic.List<ListViewItem>();
            if (which == WhichResults.Checked)
            {
                ListView.CheckedListViewItemCollection ss = lv.CheckedItems;
                foreach (ListViewItem lvi in ss)
                    sel.Add(lvi);
            }
            else if (which == WhichResults.Selected)
            {
                ListView.SelectedListViewItemCollection ss = lv.SelectedItems;
                foreach (ListViewItem lvi in ss)
                    sel.Add(lvi);
            }
            else // all
            {
                foreach (ListViewItem lvi in lv.Items)
                    sel.Add(lvi);
            }

            Count = sel.Count;

            if (sel.Count == 0)
                return;

            System.Type firstType = ((Item) (sel[0].Tag)).GetType();

            AllSameType = true;
            foreach (ListViewItem lvi in sel)
            {
                if (lvi == null)
                    continue;

                Item action = (Item) (lvi.Tag);
                if (action is Item)
                    FlatList.Add(action as Item);

                if (action.GetType() != firstType)
                    AllSameType = false;

                if (action is ActionCopyMoveRename)
                {
                    ActionCopyMoveRename cmr = action as ActionCopyMoveRename;
                    if (cmr.Operation == ActionCopyMoveRename.Op.Rename)
                        Rename.Add(cmr);
                    else // copy/move
                        CopyMove.Add(cmr);
                }
                else if (action is ActionDownloadImage)
                    Download.Add((ActionDownloadImage) (action));
                else if (action is ActionRSS)
                    RSS.Add((ActionRSS) (action));
                else if (action is ItemMissing)
                    Missing.Add((ItemMissing) (action));
                else if (action is ActionNFO)
                    NFO.Add((ActionNFO) (action));
                else if (action is ActionPyTivoMeta)
                    PyTivoMeta.Add((ActionPyTivoMeta) (action));
                //else if (action is ItemuTorrenting)
                //    this.uTorrenting.Add((ItemuTorrenting) (action));
            }
        }
    }
}
