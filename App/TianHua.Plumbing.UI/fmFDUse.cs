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
    public partial class fmFDUse : DevExpress.XtraEditors.XtraForm
    {
        public fmFDUse()
        {
            InitializeComponent();
        }

        public void InitForm()
        {
            List<string> _List = new List<string> { "小屋面", "大屋面", "43F（标）", "44F（标）", "43F（非）" };
            CheckList.DataSource = _List;
        }

        private void fmFDUse_Load(object sender, EventArgs e)
        {
            InitForm();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            //CheckList.CheckedItems
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
