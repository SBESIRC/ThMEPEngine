using System;
using System.Collections.Generic;
using System.Drawing;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;
using TianHua.Publics.BaseCode;

namespace TianHua.Hvac.UI
{
    public partial class fmDuctSpec : DevExpress.XtraEditors.XtraForm
    {
        public string SelectedInnerDuctSize { get; set; }
        public string SelectedOuterDuctSize { get; set; }
        public string AirVolume { get; set; }
        DuctSpecModel m_DuctSpecModel { get; set; }
        public fmDuctSpec()
        {
            InitializeComponent();
        }


        private void fmDuctSpec_Load(object sender, EventArgs e)
        {
            Rad_SelectedIndexChanged(null, null);
        }

        public void InitForm(DuctSpecModel _DuctSpecModel)
        {
            m_DuctSpecModel = _DuctSpecModel;

            TxtAirVolume.Text = FuncStr.NullToStr(_DuctSpecModel.AirVolume);

            TxtAirSpeed.Text = FuncStr.NullToStr(_DuctSpecModel.AirSpeed);

            ListBoxOuterTube.DataSource = _DuctSpecModel.ListOuterTube;

            ListBoxInnerTube.DataSource = _DuctSpecModel.ListInnerTube;

            ListBoxOuterTube.SelectedItem = _DuctSpecModel.OuterTube;

            ListBoxInnerTube.SelectedItem = _DuctSpecModel.InnerTube;

            //if (_DuctSpecModel.InnerAnalysisType != AnalysisResultType.OK)
            //{
            //    ListBoxInnerTube.Enabled = false;
            //    TxtInnerTube1.Enabled = false;
            //    TxtInnerTube2.Enabled = false;

            //}

            //if (_DuctSpecModel.OuterAnalysisType != AnalysisResultType.OK)
            //{
            //    ListBoxOuterTube.Enabled = false;
            //    TxtOuterTube1.Enabled = false;
            //    TxtOuterTube2.Enabled = false;
            //}
        }

        private void Rad_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Rad.Text == "推荐")
            {
                layoutControlItem5.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem6.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem7.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem4.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem8.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem9.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;


                layoutControlItem10.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem21.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem12.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem13.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem14.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem15.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem16.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem17.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem18.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem19.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;

                this.Size = new Size(170, 460);
            }
            else
            {
                layoutControlItem5.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem6.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem7.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem4.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem8.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem9.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;

                layoutControlItem10.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem21.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem12.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem13.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem14.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem15.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem16.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem17.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem18.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem19.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;

                this.Size = new Size(170, 390);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            AirVolume = TxtAirVolume.Text;
        }

        private void ListBoxOuterTube_SelectedValueChanged(object sender, EventArgs e)
        {
            SetIndexOutDuctSizeByDefault();
        }

        private void ListBoxInnerTube_SelectedValueChanged(object sender, EventArgs e)
        {
            SetIndexOutDuctSizeByDefault();
        }

        private void TxtOuterTube1_Properties_EditValueChanged(object sender, EventArgs e)
        {
            SetIndexOutDuctSizeFromUser();
        }

        private void TxtOuterTube2_Properties_EditValueChanged(object sender, EventArgs e)
        {
            SetIndexOutDuctSizeFromUser();
        }

        private void TxtInnerTube1_Properties_EditValueChanged(object sender, EventArgs e)
        {
            SetIndexOutDuctSizeFromUser();
        }

        private void TxtInnerTube2_Properties_EditValueChanged(object sender, EventArgs e)
        {
            SetIndexOutDuctSizeFromUser();
        }

        private void Rad_Properties_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Rad.EditValue.ToString() == "推荐")
            {
                SetIndexOutDuctSizeByDefault();
            }
            else
            {
                SetIndexOutDuctSizeFromUser();
            }
        }

        private void SetIndexOutDuctSizeFromUser()
        {
            SelectedOuterDuctSize = TxtOuterTube1.Text.NullToStr() + "x" + TxtOuterTube2.Text.NullToStr();
            SelectedInnerDuctSize = TxtInnerTube1.Text.NullToStr() + "x" + TxtInnerTube2.Text.NullToStr();
        }

        private void SetIndexOutDuctSizeByDefault()
        {
            SelectedOuterDuctSize = FuncStr.NullToStr(ListBoxOuterTube.SelectedValue);
            SelectedInnerDuctSize = FuncStr.NullToStr(ListBoxInnerTube.SelectedValue);
        }

        private void TxtAirSpeed_EditValueChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(TxtAirSpeed.Text) == string.Empty) { return; }

            var _AirSpeed = FuncStr.NullToStr(TxtAirSpeed.Text);

            if (FuncStr.NullToDouble(TxtAirSpeed.Text) > m_DuctSpecModel.MaxAirSpeed) { TxtAirSpeed.Text = FuncStr.NullToStr(m_DuctSpecModel.MaxAirSpeed); _AirSpeed = FuncStr.NullToStr(m_DuctSpecModel.MaxAirSpeed); }

            if (FuncStr.NullToDouble(TxtAirSpeed.Text) < m_DuctSpecModel.MinAirSpeed) { TxtAirSpeed.Text = FuncStr.NullToStr(m_DuctSpecModel.MinAirSpeed); _AirSpeed = FuncStr.NullToStr(m_DuctSpecModel.MinAirSpeed); }

            //if (FuncStr.NullToDouble(TxtAirSpeed.Text) == 0) { return; }

            ThDuctParameter _ThDuctSelectionEngine = new ThDuctParameter(FuncStr.NullToDouble(TxtAirVolume.Text), FuncStr.NullToDouble(_AirSpeed));

            m_DuctSpecModel.ListOuterTube = new List<string>(_ThDuctSelectionEngine.DuctSizeInfor.DefaultDuctsSizeString);
            m_DuctSpecModel.ListInnerTube = new List<string>(_ThDuctSelectionEngine.DuctSizeInfor.DefaultDuctsSizeString);
            m_DuctSpecModel.OuterTube = _ThDuctSelectionEngine.DuctSizeInfor.RecommendOuterDuctSize;
            m_DuctSpecModel.InnerTube = _ThDuctSelectionEngine.DuctSizeInfor.RecommendInnerDuctSize;

            ListBoxOuterTube.DataSource = m_DuctSpecModel.ListOuterTube;

            ListBoxInnerTube.DataSource = m_DuctSpecModel.ListInnerTube;

            ListBoxOuterTube.SelectedItem = m_DuctSpecModel.OuterTube;

            ListBoxInnerTube.SelectedItem = m_DuctSpecModel.InnerTube;
        }

        private void TxtOuterTube1_EditValueChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(TxtOuterTube1.Text) == string.Empty || FuncStr.NullToStr(TxtOuterTube2.Text) == string.Empty) { return; }

            var _AirSpeed = FuncStr.NullToDouble(TxtAirVolume.Text) / 3600 / (FuncStr.NullToDouble(TxtOuterTube1.Text) * FuncStr.NullToDouble(TxtOuterTube2.Text) / 1000000);

            LabAirSpeedOuter.Text = string.Format("计算风速 {0} m/s", _AirSpeed.ToString("0.#"));

        }

        private void TxtOuterTube2_EditValueChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(TxtOuterTube1.Text) == string.Empty || FuncStr.NullToStr(TxtOuterTube2.Text) == string.Empty) { return; }

            var _AirSpeed = FuncStr.NullToDouble(TxtAirVolume.Text) / 3600 / (FuncStr.NullToDouble(TxtOuterTube1.Text) * FuncStr.NullToDouble(TxtOuterTube2.Text) / 1000000);

            LabAirSpeedOuter.Text = string.Format("计算风速 {0} m/s", _AirSpeed.ToString("0.#"));
        }

        private void TxtInnerTube1_EditValueChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(TxtInnerTube1.Text) == string.Empty || FuncStr.NullToStr(TxtInnerTube2.Text) == string.Empty) { return; }

            var _AirSpeed = FuncStr.NullToDouble(TxtAirVolume.Text) / 3600 / (FuncStr.NullToDouble(TxtInnerTube1.Text) * FuncStr.NullToDouble(TxtInnerTube2.Text) / 1000000);

            LabAirSpeedInner.Text = string.Format("计算风速 {0} m/s", _AirSpeed.ToString("0.#"));
        }

        private void TxtInnerTube2_EditValueChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(TxtInnerTube1.Text) == string.Empty || FuncStr.NullToStr(TxtInnerTube2.Text) == string.Empty) { return; }

            var _AirSpeed = FuncStr.NullToDouble(TxtAirVolume.Text) / 3600 / (FuncStr.NullToDouble(TxtInnerTube1.Text) * FuncStr.NullToDouble(TxtInnerTube2.Text) / 1000000);

            LabAirSpeedInner.Text = string.Format("计算风速 {0} m/s", _AirSpeed.ToString("0.#"));
        }

        private void TxtAirSpeed_ParseEditValue(object sender, DevExpress.XtraEditors.Controls.ConvertEditValueEventArgs e)
        {

        }

        private void TxtHeight_EditValueChanged(object sender, EventArgs e)
        {
            if (TxtHeight.Text == string.Empty || FuncStr.NullToDouble(TxtHeight.Text) == 0) { return; }
            TxtHeight.Text = FuncStr.NullToDouble(TxtHeight.Text).ToString("#.00");
        }
    }
}
