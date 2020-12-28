using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThMEPHVAC;
using TianHua.Publics.BaseCode;

namespace TianHua.Hvac.UI
{
    public partial class fmDuctSpec : DevExpress.XtraEditors.XtraForm
    {
        public string SelectedInnerDuctSize { get; set; }
        public string SelectedOuterDuctSize { get; set; }
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
            TxtAirVolume.Text = FuncStr.NullToStr(_DuctSpecModel.AirVolume);

            TxtAirSpeed.Text = FuncStr.NullToStr(_DuctSpecModel.AirSpeed);

            ListBoxOuterTube.DataSource = _DuctSpecModel.ListOuterTube;

      

            ListBoxInnerTube.DataSource = _DuctSpecModel.ListInnerTube;

      

            ListBoxOuterTube.SelectedItem = _DuctSpecModel.OuterTube;

            ListBoxInnerTube.SelectedItem = _DuctSpecModel.InnerTube;
        }

        private void Rad_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Rad.Text == "推荐")
            {
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

                this.Size = new Size(170, 400);
            }
            else
            {
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

                this.Size = new Size(170, 370);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {

 
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
            if(Rad.EditValue.ToString() == "推荐")
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


    }
}
