// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
// 

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Humanizer;
using NLog;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

namespace TVRename
{
    public partial class ChooseFile : Form
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public enum ChooseFileDialogResult
        {
            left,right,ignore
        }

        public ChooseFileDialogResult Answer;
        private readonly FileInfo leftFile;
        private readonly FileInfo rightFile;

        public ChooseFile(FileInfo left, FileInfo right)
        {
            InitializeComponent();
            
            leftFile = left;
            rightFile = right;

            (int leftFilmLength,bool leftFrameUnknown,int leftFrameWidth) = UpdateFields(left, txtNameLeft, txtDimensionsLeft, txtLengthLeft, txtSizeLeft, txtPathLeft);
            (int rightFilmLength, bool rightFrameUnknown, int rightFrameWidth) = UpdateFields(right, lblNameRight, lblDimensionsRight, lblLengthRight, lblSizeRight, txtPathRight);

            SetBoldFileSize(left, right);

            SetBoldFilmLength(leftFilmLength, rightFilmLength);

            if (rightFrameUnknown || leftFrameUnknown) return;

            SetBoldFrameWidth(leftFrameWidth, rightFrameWidth);
        }

        private static (int,bool,int) UpdateFields(FileInfo file, Label nameLabel, Label dimensionsLabel, Label lengthLabel, Label sizeLabel, Label pathLabel)
        {
            nameLabel.Text = file.Name;
            int leftFrameWidth = file.GetFrameWidth();
            bool leftFrameUnknown = leftFrameWidth == -1;
            dimensionsLabel.Text = "Dimensions: " + (leftFrameUnknown ? "Unknown" : leftFrameWidth + "x" + file.GetFrameHeight());
            int leftFilmLength = file.GetFilmLength();
            try
            {
                lengthLabel.Text =
                    "Length: " + ((leftFilmLength == -1) ? "Unknown" : leftFilmLength.Seconds().Humanize(2));
            }
            catch (ArgumentException) //bug in Humanizer causes this in Polish
            {
                lengthLabel.Text =
                    "Length: " + ((leftFilmLength == -1) ? "Unknown" : leftFilmLength.Seconds() + " s");
            }

            sizeLabel.Text = GetFileSize(file);
            pathLabel.Text = file.DirectoryName;

            return (leftFrameWidth, leftFrameUnknown, leftFilmLength);
        }

        private static string GetFileSize(FileInfo file)
        {
            try
            {
                return file.Length.Bytes().Humanize("#.#");
            }
            catch (FileNotFoundException fnfex)
            {
                Logger.Fatal(fnfex,$"Can't find File in ChooseFile called {file.Name}");
                return "Unknown";
            }
        }

        private void SetBoldFileSize(FileInfo left, FileInfo right)
        {
            if (left.Length > right.Length)
            {
                txtSizeLeft.Font = new Font(txtSizeLeft.Font.Name, txtSizeLeft.Font.Size, FontStyle.Bold);
                lblSizeRight.Font = new Font(lblSizeRight.Font.Name, lblSizeRight.Font.Size, FontStyle.Regular);
            }
            else if (left.Length < right.Length)
            {
                txtSizeLeft.Font = new Font(txtSizeLeft.Font.Name, txtSizeLeft.Font.Size, FontStyle.Regular);
                lblSizeRight.Font = new Font(lblSizeRight.Font.Name, lblSizeRight.Font.Size, FontStyle.Bold);
            }
            else
            {
                txtSizeLeft.Font = new Font(txtSizeLeft.Font.Name, txtSizeLeft.Font.Size, FontStyle.Regular);
                lblSizeRight.Font = new Font(lblSizeRight.Font.Name, lblSizeRight.Font.Size, FontStyle.Regular);
            }
        }

        private void SetBoldFilmLength(int leftFilmLength, int rightFilmLength)
        {
            if (leftFilmLength > rightFilmLength)
            {
                txtLengthLeft.Font = new Font(txtLengthLeft.Font.Name, txtLengthLeft.Font.Size, FontStyle.Bold);
                lblLengthRight.Font = new Font(lblLengthRight.Font.Name, lblLengthRight.Font.Size, FontStyle.Regular);
            }
            else if (leftFilmLength < rightFilmLength)
            {
                txtLengthLeft.Font = new Font(txtLengthLeft.Font.Name, txtLengthLeft.Font.Size, FontStyle.Regular);
                lblLengthRight.Font = new Font(lblLengthRight.Font.Name, lblLengthRight.Font.Size, FontStyle.Bold);
            }
            else
            {
                txtLengthLeft.Font = new Font(txtLengthLeft.Font.Name, txtLengthLeft.Font.Size, FontStyle.Regular);
                lblLengthRight.Font = new Font(lblLengthRight.Font.Name, lblLengthRight.Font.Size, FontStyle.Regular);
            }
        }

        private void SetBoldFrameWidth(int leftFrameWidth, int rightFrameWidth)
        {
            if (leftFrameWidth > rightFrameWidth)
            {
                txtDimensionsLeft.Font = new Font(txtDimensionsLeft.Font.Name, txtDimensionsLeft.Font.Size, FontStyle.Bold);
                lblDimensionsRight.Font = new Font(lblDimensionsRight.Font.Name, lblDimensionsRight.Font.Size, FontStyle.Regular);
            }
            else if (leftFrameWidth < rightFrameWidth)
            {
                txtDimensionsLeft.Font = new Font(txtDimensionsLeft.Font.Name, txtDimensionsLeft.Font.Size, FontStyle.Regular);
                lblDimensionsRight.Font = new Font(lblDimensionsRight.Font.Name, lblDimensionsRight.Font.Size, FontStyle.Bold);
            }
            else
            {
                txtDimensionsLeft.Font = new Font(txtDimensionsLeft.Font.Name, txtDimensionsLeft.Font.Size, FontStyle.Regular);
                lblDimensionsRight.Font = new Font(lblDimensionsRight.Font.Name, lblDimensionsRight.Font.Size, FontStyle.Regular);
            }
        }

        private void lnkOpenLeftFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Helpers.SysOpen(leftFile.DirectoryName);
        }

        private void lnkOpenRightFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Helpers.SysOpen(rightFile.DirectoryName);
        }

        private void btnLeft_Click(object sender, EventArgs e)
        {
            Answer = ChooseFileDialogResult.left;
            Close();
        }

        private void Ignore_Click(object sender, EventArgs e)
        {
            Answer = ChooseFileDialogResult.ignore;
            Close();
        }

        private void btnKeepRight_Click(object sender, EventArgs e)
        {
            Answer = ChooseFileDialogResult.right;
            Close();
        }
    }
}
