using System;
using System.Windows.Forms;

namespace TianHua.AutoCAD.ThCui
{
    public partial class ThFeedBackDlg : Form
    {
        public ThFeedBackDlg()
        {
            InitializeComponent();
        }

        private void LinkLab_Click(object sender, EventArgs e)
        {
            LinkLab.LinkVisited = true;
            System.Diagnostics.Process.Start("mailto:" + LinkLab.Text);
        }

        private void fmFeedBack_Load(object sender, EventArgs e)
        {
            //
        }
    }
}
