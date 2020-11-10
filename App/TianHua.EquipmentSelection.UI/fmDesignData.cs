using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using TianHua.Publics.BaseCode;
using TianHua.FanSelection.UI.IO;

namespace TianHua.FanSelection.UI
{
    public partial class fmDesignData : DevExpress.XtraEditors.XtraForm
    {
        public List<FanDesignDataModel> m_ListFanDesign { get; set; }

        public FanDesignDataModel m_FanDesign { get; set; }

        public FanDesignDataModel m_CurrentFanDesign { get; set; }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bSCan, int dwFlags, int dwExtraInfo);

        public string m_ActionType = string.Empty;

        public string m_Path = string.Empty;

        public double m_FilterDate = 0;

        public fmDesignData()
        {
            InitializeComponent();
        }

        public void InitForm(List<FanDesignDataModel> _ListFanDesign, string _ActionType, string _Path, FanDesignDataModel _FanDesignDataModel)
        {
            m_Path = _Path;
            m_ActionType = _ActionType;
            m_ListFanDesign = _ListFanDesign;
            m_CurrentFanDesign = _FanDesignDataModel;
            if (m_ListFanDesign == null) m_ListFanDesign = new List<FanDesignDataModel>();

            if (m_ActionType == "保存")
            {
                this.Text = "保存设计数据";
                m_FanDesign = new FanDesignDataModel();
                m_FanDesign.ID = Guid.NewGuid().ToString();
                m_FanDesign.CreateDate = DateTime.Now;
                m_FanDesign.LastOperationDate = DateTime.Now;
                m_FanDesign.Name = "新建设计数据";
                m_FanDesign.Status = "1";

                m_FanDesign.Name = SetFanDesignDataName(m_FanDesign);
                m_FanDesign.Path = GetPath(m_FanDesign);
                m_ListFanDesign.ForEach(p => p.Status = "0");
                m_ListFanDesign.Insert(0, m_FanDesign);
            }
            if (m_ActionType == "另存")
            {
                this.Text = "另存设计数据";
                m_FanDesign = new FanDesignDataModel();
                m_FanDesign.ID = Guid.NewGuid().ToString();
                m_FanDesign.CreateDate = DateTime.Now;
                m_FanDesign.LastOperationDate = DateTime.Now;
                m_FanDesign.Name = "新建设计数据";
                m_FanDesign.Status = "1";

                m_FanDesign.Name = SetFanDesignDataName(m_FanDesign);
                m_FanDesign.Path = GetPath(m_FanDesign);
                m_ListFanDesign.Insert(0, m_FanDesign);
            }
            if (m_ActionType == "打开")
            {
                this.Text = "打开设计数据";

            }

            Gdc.DataSource = m_ListFanDesign;
            Gdc.Refresh();

            Gdv.FocusedRowHandle = 0;
            Gdv.FocusedColumn = Gdv.Columns["Name"];
            Gdv.ShowEditor();


        }

        public string SetFanDesignDataName(FanDesignDataModel _FanData)
        {
            var _List = m_ListFanDesign.FindAll(p => p.Name.Contains(_FanData.Name));
            if (_List == null || _List.Count == 0) { return _FanData.Name + "(1)"; }
            for (int i = 1; i < 10000; i++)
            {
                if (i == 1)
                {
                    var _ListTemp1 = m_ListFanDesign.FindAll(p => p.Name == _FanData.Name + "(1)");
                    if (_ListTemp1 == null || _ListTemp1.Count == 0) { return _FanData.Name + "(1)"; }
                }
                else
                {
                    var _ListTemp = m_ListFanDesign.FindAll(p => p.Name == _FanData.Name + "(" + i + ")");
                    if (_ListTemp == null || _ListTemp.Count == 0) { return _FanData.Name + "(" + i + ")"; }
                }

            }
            return string.Empty;
        }


        private string GetPath(FanDesignDataModel _FanDesign)
        {
            if (_FanDesign == null || FuncStr.NullToStr(_FanDesign.Name) == string.Empty) { return string.Empty; }
            return Path.Combine(m_Path, FuncStr.NullToStr(_FanDesign.Name) + ".json");
        }

        private void fmDesignData_Load(object sender, EventArgs e)
        {
            if (m_ActionType == "保存" || m_ActionType == "另存")
                keybd_event((byte)Keys.Tab, 0, 0, 0);
        }

        private void ComBoxName_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (m_ListFanDesign == null || m_ListFanDesign.Count == 0) return;
            if (m_ActionType == "另存" || m_ActionType == "保存") { return; }
            var _FanDesign = Gdv.GetRow(Gdv.FocusedRowHandle) as FanDesignDataModel;

            if (m_CurrentFanDesign != null && m_CurrentFanDesign.ID == _FanDesign.ID) { XtraMessageBox.Show(" 当前打开文件无法进行删除！ ", "提示"); return; }
            if (_FanDesign.Status == "1") { XtraMessageBox.Show(" 未保存文件无法进行删除！ ", "提示"); return; }
            if (XtraMessageBox.Show(" 设计数据[" + _FanDesign.Name + "]将被删除，是否继续？ ", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                File.Delete(_FanDesign.Path);
                m_ListFanDesign.Remove(_FanDesign);
                Gdc.DataSource = m_ListFanDesign;

                Gdv.RefreshData();
                Gdv.FocusedColumn = ColLastOperationName;
                Gdv.FocusedColumn = ColName;
            }


        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Gdv.PostEditor();
            if (m_FanDesign != null) m_FanDesign.LastOperationDate = DateTime.Now;
            if (!Directory.Exists(m_Path))
            {
                Directory.CreateDirectory(m_Path);
            }
            m_ListFanDesign.ForEach(p => p.Status = "0");
            var _Json = FuncJson.Serialize(m_ListFanDesign);

            JsonExporter.Instance.SaveToFile(Path.Combine(m_Path, ThFanSelectionUICommon.MODEL_EXPORTCATALOG), Encoding.UTF8, _Json);

            if (m_ActionType == "打开")
            {
                m_FanDesign = Gdv.GetRow(Gdv.FocusedRowHandle) as FanDesignDataModel;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (m_ActionType == "保存" || m_ActionType == "另存")
            {
                m_ListFanDesign.Remove(m_FanDesign);
            }

        }

        private void Gdv_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column.FieldName == "Name")
            {

                var _FanDesign = Gdv.GetRow(Gdv.FocusedRowHandle) as FanDesignDataModel;

                //if (m_CurrentFanDesign != null && m_CurrentFanDesign.ID == _FanDesign.ID) { return; }

                if (m_ActionType == "打开")
                {
                    var _Path = _FanDesign.Path;
                    _FanDesign.Path = GetPath(_FanDesign);
                    File.Move(_Path, _FanDesign.Path);
                }
                else
                {
                    _FanDesign.Path = GetPath(m_FanDesign);
                }



            }
        }

        private void TxtSearch_EditValueChanging(object sender, DevExpress.XtraEditors.Controls.ChangingEventArgs e)
        {
            Filter(FuncStr.NullToStr(e.NewValue));
        }

        private void Filter(string _FilterStr)
        {

            //var _DateTime = DateTime.Now;

            var _FilterString = @" Name LIKE '%" + _FilterStr + "%'";

            //if (m_FilterDate != 0)
            //    _FilterString += @"  And ( LastOperationDate  >  '" + _DateTime.AddDays(m_FilterDate) + "')";

            //if (m_FilterDate != 0)
            //    _FilterString += @"  And ( LastOperationDate  Between '" + _DateTime.AddDays(m_FilterDate) + "' And '" + _DateTime + "')";


            (Gdv as ColumnView).ActiveFilterString = _FilterString;
        }



        private void Gdv_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {

            var _FanDesignDataModel = Gdv.GetFocusedRow() as FanDesignDataModel;
            if (_FanDesignDataModel == null) { return; }
            var _FocusedColumn = Gdv.FocusedColumn;
            if (_FocusedColumn.FieldName == "Name")
            {

                var _List = m_ListFanDesign.FindAll(p => FuncStr.NullToStr(p.Name) == FuncStr.NullToStr(e.Value) && p.ID != _FanDesignDataModel.ID);
                if (_List.Count > 0)
                {
                    e.Valid = false;
                    e.ErrorText = "文件名称不能重复!";
                    return;
                }

            }
        }

        private void Gdv_ShowingEditor(object sender, CancelEventArgs e)
        {
            var _FanDesignDataModel = Gdv.GetFocusedRow() as FanDesignDataModel;
            if (_FanDesignDataModel == null) { return; }





            if (m_ActionType == "另存" || m_ActionType == "保存")
            {
                var _FocusedColumn = Gdv.FocusedColumn;
                if (_FanDesignDataModel.Status != "1")
                {
                    e.Cancel = true;
                    return;
                }
            }
        }
    }
}
