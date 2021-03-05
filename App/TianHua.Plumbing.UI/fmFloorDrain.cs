using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TianHua.Plumbing.UI
{
    public partial class fmFloorDrain :  DevExpress.XtraEditors.XtraForm
    {
        public fmFloorDrain()
        {
            InitializeComponent();
        }

        private void fmFloorDrain_Load(object sender, EventArgs e)
        {
            InitForm();
        }

        public void InitForm()
        {
            List<string> _List = new List<string> { "小屋面", "大屋面", "43F（标）", "44F（标）", "43F（非）" };
            ListBox.DataSource = _List;
        }

        private void BtnFloorFocus_Click(object sender, EventArgs e)
        {

        }

        private void BtnGetFloor_Click(object sender, EventArgs e)
        {

        }

        private void BtnParam_Click(object sender, EventArgs e)
        {
            fmFDParam _fmFDParam = new fmFDParam();
            _fmFDParam.ShowDialog();
        }

        private void BtnLayoutRiser_Click(object sender, EventArgs e)
        {

        }

        private void BtnUse_Click(object sender, EventArgs e)
        {
            fmFDUse _fmFDUse = new fmFDUse();
            _fmFDUse.ShowDialog();
        }


    }
}
