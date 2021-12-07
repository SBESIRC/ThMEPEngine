﻿using System;
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
        /// <summary>
        /// 机房外标高
        /// </summary>
        public string Elevation { get; set; }
        /// <summary>
        /// 机房内标高
        /// </summary>
        public string Elevation2 { get; set; }
        public string TextSize { get; set; }
        public DuctSpecModel Model { get; set; }
        public fmDuctSpec()
        {
            InitializeComponent();
        } 

        private void fmDuctSpec_Load(object sender, EventArgs e)
        {
            Rad_SelectedIndexChanged(null, null);
        }

        public void InitForm(DuctSpecModel _DuctSpecModel, bool is_exhaust)
        {
            Model = _DuctSpecModel;

            TxtAirVolume.Text = _DuctSpecModel.StrAirVolume;
            //TxtAirSpeed.Text = TxtAirSpeed.Text.ToString();
            TxtAirSpeed.Text = FuncStr.NullToStr(_DuctSpecModel.AirSpeed);

            ListBoxOuterTube.DataSource = _DuctSpecModel.ListOuterTube;

            ListBoxInnerTube.DataSource = _DuctSpecModel.ListInnerTube;

            ListBoxOuterTube.SelectedItem = is_exhaust ? _DuctSpecModel.OuterTube : _DuctSpecModel.InnerTube;

            ListBoxInnerTube.SelectedItem = is_exhaust ? _DuctSpecModel.InnerTube : _DuctSpecModel.OuterTube;
            AcceptButton = BtnOK;
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

                this.Size = new Size(170, 500);
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

                this.Size = new Size(170, 430);

            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Elevation = TxtHeight.Text;
            Elevation2 = TxtHeight2.Text;
            AirVolume = TxtAirVolume.Text;
            TextSize = ComBoxDrawingRatio.Text;
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

            if (FuncStr.NullToDouble(TxtAirSpeed.Text) > Model.MaxAirSpeed) { TxtAirSpeed.Text = FuncStr.NullToStr(Model.MaxAirSpeed); _AirSpeed = FuncStr.NullToStr(Model.MaxAirSpeed); }

            if (FuncStr.NullToDouble(TxtAirSpeed.Text) < Model.MinAirSpeed) { TxtAirSpeed.Text = FuncStr.NullToStr(Model.MinAirSpeed); _AirSpeed = FuncStr.NullToStr(Model.MinAirSpeed); }

            //if (FuncStr.NullToDouble(TxtAirSpeed.Text) == 0) { return; }

            //ThDuctParameter _ThDuctSelectionEngine = new ThDuctParameter(FuncStr.NullToDouble(TxtAirVolume.Text));

            //Model.ListOuterTube = new List<string>(_ThDuctSelectionEngine.DuctSizeInfor.DefaultDuctsSizeString);
            //Model.ListInnerTube = new List<string>(_ThDuctSelectionEngine.DuctSizeInfor.DefaultDuctsSizeString);
            //Model.OuterTube = _ThDuctSelectionEngine.DuctSizeInfor.RecommendOuterDuctSize;
            //Model.InnerTube = _ThDuctSelectionEngine.DuctSizeInfor.RecommendInnerDuctSize;

            //ListBoxOuterTube.DataSource = Model.ListOuterTube;

            //ListBoxInnerTube.DataSource = Model.ListInnerTube;

            //ListBoxOuterTube.SelectedItem = Model.OuterTube;

            //ListBoxInnerTube.SelectedItem = Model.InnerTube;
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

        private void TxtHeight2_EditValueChanged(object sender, EventArgs e)
        {
            if (TxtHeight2.Text == string.Empty || FuncStr.NullToDouble(TxtHeight2.Text) == 0) { return; }
            TxtHeight2.Text = FuncStr.NullToDouble(TxtHeight2.Text).ToString("#.00");
        }
    }
}
