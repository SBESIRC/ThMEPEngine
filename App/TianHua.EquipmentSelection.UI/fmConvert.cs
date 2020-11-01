using DevExpress.XtraEditors;
using System;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using System.IO;
using TianHua.Publics.BaseCode;
using TianHua.FanSelection.UI.IO;

namespace TianHua.FanSelection.UI
{
    public partial class fmConvert : XtraForm
    {
        private DataManager m_DataMgr;

        public fmConvert()
        {
            InitializeComponent();
        }

        private void fmConvert_Load(object sender, EventArgs e)
        {
            m_DataMgr = new DataManager();
        }

        private void Panel_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.RestoreDirectory = true;
            dlg.Filter = "Excel File(*.xlsx)|*.xlsx";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadExcelAsync(dlg.FileName);
            }
        }


        /// <summary>
        /// 使用BackgroundWorker加载Excel文件，使用UI中的Options设置
        /// </summary>
        /// <param name="_Path">Excel文件路径</param>
        private void LoadExcelAsync(string _Path)
        {
            var mCurrentXlsx = _Path;
            var FileName = System.IO.Path.GetFileName(_Path);
            this.labelExcelFile.Text = FileName;
            this.BGWorker.RunWorkerAsync(_Path);
        }

        private void BGWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            lock (this.m_DataMgr)
            {
                this.m_DataMgr.LoadExcel((string)e.Argument);
            }

        }

        private void BGWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this.labelExcelFile.Text = "Open you .xlsx file here!";
                XtraMessageBox.Show(e.Error.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lock (this.m_DataMgr)
            {
                if (m_DataMgr.m_ListFan_Forerake_Single != null && m_DataMgr.m_ListFan_Forerake_Single.Count > 0)
                {
                    CheckFan_Forerake_Single.Enabled = true;
                    CheckFan_Forerake_Single.Checked = true;
                }
                else
                {
                    CheckFan_Forerake_Single.Enabled = false;
                    CheckFan_Forerake_Single.Checked = false;
                }

                if (m_DataMgr.m_ListFan_Forerake_Double != null && m_DataMgr.m_ListFan_Forerake_Double.Count > 0)
                {
                    CheckFan_Forerake_Double.Enabled = true;
                    CheckFan_Forerake_Double.Checked = true;
                }
                else
                {
                    CheckFan_Forerake_Double.Enabled = false;
                    CheckFan_Forerake_Double.Checked = false;
                }

                if (m_DataMgr.m_ListFan_Hypsokinesis_Single != null && m_DataMgr.m_ListFan_Hypsokinesis_Single.Count > 0)
                {

                    CheckFan_Hypsokinesis_Single.Enabled = true;
                    CheckFan_Hypsokinesis_Single.Checked = true;
                }
                else
                {
                    CheckFan_Hypsokinesis_Single.Enabled = false;
                    CheckFan_Hypsokinesis_Single.Checked = false;
                }

                if (m_DataMgr.m_ListAxialFan_Single != null && m_DataMgr.m_ListAxialFan_Single.Count > 0)
                {

                    CheckAxialFan_Single.Enabled = true;
                    CheckAxialFan_Single.Checked = true;
                }
                else
                {
                    CheckAxialFan_Single.Enabled = false;
                    CheckAxialFan_Single.Checked = false;
                }


                if (m_DataMgr.m_ListAxialFan_Double != null && m_DataMgr.m_ListAxialFan_Double.Count > 0)
                {

                    CheckAxialFan_Double.Enabled = true;
                    CheckAxialFan_Double.Checked = true;
                }
                else
                {
                    CheckAxialFan_Double.Enabled = false;
                    CheckAxialFan_Double.Checked = false;
                }
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {

            if (!CheckFan_Forerake_Single.Checked && !CheckFan_Hypsokinesis_Single.Checked && !CheckFan_Forerake_Double.Checked && !CheckAxialFan_Single.Checked)
            {
                XtraMessageBox.Show("请正确选择需要转换的 .Xlsx 数据!", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (CheckFan_Forerake_Single.Checked)
            {
                var _Json = FuncJson.Serialize(m_DataMgr.m_ListFan_Forerake_Single);
                m_DataMgr.m_Json.SaveToFile(
                    Path.Combine(Environment.CurrentDirectory, "离心-前倾-单速.json"),
                    Encoding.UTF8,
                    _Json);
            }
            if (CheckFan_Forerake_Double.Checked)
            {
                var _Json = FuncJson.Serialize(m_DataMgr.m_ListFan_Forerake_Double);
                m_DataMgr.m_Json.SaveToFile(
                    Path.Combine(Environment.CurrentDirectory, "离心-前倾-双速.json"),
                    Encoding.UTF8,
                    _Json);
            }
            if (CheckFan_Hypsokinesis_Single.Checked)
            {
                var _Json = FuncJson.Serialize(m_DataMgr.m_ListFan_Hypsokinesis_Single);
                m_DataMgr.m_Json.SaveToFile(
                    Path.Combine(Environment.CurrentDirectory, "离心-后倾-单速.json"),
                    Encoding.UTF8,
                    _Json);
            }

            if (CheckAxialFan_Single.Checked)
            {
                var _Json = FuncJson.Serialize(m_DataMgr.m_ListAxialFan_Single);
                m_DataMgr.m_Json.SaveToFile(
                    Path.Combine(Environment.CurrentDirectory, "轴流-单速.json"),
                    Encoding.UTF8,
                    _Json);
            }

            if (CheckAxialFan_Double.Checked)
            {
                var _Json = FuncJson.Serialize(m_DataMgr.m_ListAxialFan_Double);
                m_DataMgr.m_Json.SaveToFile(
                    Path.Combine(Environment.CurrentDirectory, "轴流-双速.json"),
                    Encoding.UTF8,
                    _Json);
            }

            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {

        }
    }
}
