using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.UI
{
    public partial class fmSceneScreening : DevExpress.XtraEditors.XtraForm
    {

        public List<string> m_List { get; set; }

        public fmSceneScreening()
        {
            InitializeComponent();


            foreach (Control _Ctrl in this.layoutControl1.Controls)
            {
                if (_Ctrl is CheckEdit)
                {
                    var _Edit = _Ctrl as CheckEdit;
                    if (_Edit.Name == "CheckAll") continue;
                    _Edit.CheckedChanged += Check_CheckedChanged;
                }
            }

        }


        public void Init(List<string> _List)
        {
            m_List = _List;
            if (_List == null) { return; }
            foreach (Control _Ctrl in this.layoutControl1.Controls)
            {
                if (_Ctrl is CheckEdit)
                {
                    var _Edit = _Ctrl as CheckEdit;
                    if (m_List.Contains(_Edit.Text))
                    {
                        _Edit.Checked = true;
                    }
                    else
                    {
                        _Edit.Checked = false;
                    }
                }
            }
        }


        private void CheckAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (Control _Ctrl in this.layoutControl1.Controls)
            {
                if (_Ctrl is CheckEdit)
                {

                    var _Edit = _Ctrl as CheckEdit;
                    _Edit.Checked = CheckAll.Checked;

                }
            }
        }

        private void Check_CheckedChanged(object sender, EventArgs e)
        {
            CheckEdit _CheckEdit = sender as CheckEdit;
            if (_CheckEdit.Checked == true)
            {
                foreach (Control _Ctrl in this.layoutControl1.Controls)
                {
                    if (_Ctrl is CheckEdit)
                    {
                        var _Edit = _Ctrl as CheckEdit;
                        if (_Edit.Name != "CheckAll" && _Edit.Checked == false)
                            return;
                    }
                }
                this.CheckAll.CheckedChanged -= new System.EventHandler(this.CheckAll_CheckedChanged);
                CheckAll.Checked = true;
                this.CheckAll.CheckedChanged += new System.EventHandler(this.CheckAll_CheckedChanged);
            }
            else
            {
                this.CheckAll.CheckedChanged -= new System.EventHandler(this.CheckAll_CheckedChanged);
                CheckAll.EditValue = false;
                this.CheckAll.CheckedChanged += new System.EventHandler(this.CheckAll_CheckedChanged);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            m_List = new List<string>();
            foreach (Control _Ctrl in this.layoutControl1.Controls)
            {
                if (_Ctrl is CheckEdit)
                {
                    var _Edit = _Ctrl as CheckEdit;
                    if (_Edit.Checked)
                        m_List.Add(_Edit.Text);
                }
            }
        }

        private void Check_EditValueChanging(object sender, DevExpress.XtraEditors.Controls.ChangingEventArgs e)
        {
            //var _CheckEdit = sender as CheckEdit;
            //if (_CheckEdit == null) { return; };
            //this.CheckAll.CheckedChanged -= new System.EventHandler(this.CheckAll_CheckedChanged);
            //if (Convert.ToBoolean(e.NewValue) == false)
            //{
            //    CheckAll.EditValue = false;
            //    this.CheckAll.CheckedChanged += new System.EventHandler(this.CheckAll_CheckedChanged);
            //    return;
            //}
            //foreach (Control _Ctrl in this.layoutControl1.Controls)
            //{
            //    if (_Ctrl is CheckEdit)
            //    {
            //        var _Edit = _Ctrl as CheckEdit;
            //        if (_Edit.Text == "全选" || _CheckEdit.Name == _Edit.Name) continue;
            //        if (!_Edit.Checked)
            //        {
            //            CheckAll.EditValue = false;
            //            this.CheckAll.CheckedChanged += new System.EventHandler(this.CheckAll_CheckedChanged);
            //            return;
            //        }
            //    }
            //}

            //CheckAll.Checked = true;
            //this.CheckAll.CheckedChanged += new System.EventHandler(this.CheckAll_CheckedChanged);
        }
    }
}
