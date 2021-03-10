using System;
using System.Collections.Generic;
using ThMEPWSS.Pipe.Service;
using TianHua.Publics.BaseCode;

namespace TianHua.Plumbing.UI
{
    public partial class fmFDParam : DevExpress.XtraEditors.XtraForm
    {
        public fmFDParam()
        {
            InitializeComponent();
        }

        private void fmFDParam_Load(object sender, EventArgs e)
        {
            InitControl();
        }

        private void InitControl()
        {
            ComBoxScale.Properties.Items.AddRange(new List<string> { "1:50", "1:100", "1:150" });
            ComBoxScale.EditValue = "1:100";

            ComBoxSpec.Properties.Items.AddRange(new List<string> { "DN100", "DN125", "DN150", "DN200" });
            ComBoxSpec.EditValue = "DN100";

            ComBoxHutchWastewater.Properties.Items.AddRange(new List<string> { "DN100", "DN125", "DN150", "DN200" });
            ComBoxHutchWastewater.EditValue = "DN100";

            ComBoxSewagePipe.Properties.Items.AddRange(new List<string> { "DN100", "DN150", "DN200" });
            ComBoxSewagePipe.EditValue = "DN100";

            ComBoxVentStack.Properties.Items.AddRange(new List<string> { "DN100", "DN150", "DN200" });
            ComBoxVentStack.EditValue = "DN100";

            ComBoxBalconyWastewater.Properties.Items.AddRange(new List<string> { "DN100", "DN150", "DN200" });
            ComBoxBalconyWastewater.EditValue = "DN100";


            ComBoxRoofRainwater.Properties.Items.AddRange(new List<string> { "DN100", "DN150", "DN200" });
            ComBoxRoofRainwater.EditValue = "DN100";

            ComBoxBalconyRain.Properties.Items.AddRange(new List<string> { "DN100", "DN150", "DN200" });
            ComBoxBalconyRain.EditValue = "DN100";

            ComBoxCondensation.Properties.Items.AddRange(new List<string> { "DN50", "DN75", "DN100" });
            ComBoxCondensation.EditValue = "DN50";


            ComBoxBSpec.Properties.Items.AddRange(new List<string> { "DN75", "DN100", "DN150" });
            ComBoxBSpec.EditValue = "DN100";


            ComBoxBRoofDrain.Properties.Items.AddRange(new List<string> { "DN75", "DN100", "DN150" });
            ComBoxBRoofDrain.EditValue = "DN100";



            ComBoxLSpec.Properties.Items.AddRange(new List<string> { "DN75", "DN100", "DN150" });
            ComBoxLSpec.EditValue = "DN100";


            ComBoxLRoofDrain.Properties.Items.AddRange(new List<string> { "DN75", "DN100", "DN150" });
            ComBoxLRoofDrain.EditValue = "DN100";

            RidDrainageWay_EditValueChanged(null, null);
        }

        private void RidDrainageWay_EditValueChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(RidDrainageWay.EditValue) == "污废合流")
            {
                ComBoxSpec.Enabled = true;
                ComBoxHutchWastewater.Enabled = false;
                ComBoxSewagePipe.Enabled = false;
            }
            else
            {
                ComBoxSpec.Enabled = false;
                ComBoxHutchWastewater.Enabled = true;
                ComBoxSewagePipe.Enabled = true;
            }
        }

        private void ComBoxScale_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (ComBoxScale.SelectedItem.ToString() == "1:50")
            {
                ThTagParametersService.ScaleFactor = 1;
            }
            else if (ComBoxScale.SelectedItem.ToString() == "1:100")
            {
                ThTagParametersService.ScaleFactor = 2;
            }
            else
            {
                ThTagParametersService.ScaleFactor = 3;
            }
            ThTagParametersService.GravityBuckettag1 = ComBoxBSpec.SelectedItem.ToString();
            ThTagParametersService.GravityBuckettag = ComBoxBSpec.SelectedItem.ToString();
            ThTagParametersService.BucketStyle = RidBRoofDrain.EditValue.ToString();
            ThTagParametersService.BucketStyle1 = RidLRroofDrain.EditValue.ToString();
            ThTagParametersService.SideBuckettag = ComBoxLRoofDrain.EditValue.ToString();
            ThTagParametersService.SideBuckettag1 = ComBoxBRoofDrain.EditValue.ToString();
            ThTagParametersService.RoofRainpipe = ComBoxRoofRainwater.SelectedItem != null ? double.Parse(GetDoubleString(ComBoxRoofRainwater.SelectedItem.ToString())) : double.Parse(GetDoubleString(ComBoxSpec.SelectedItem.ToString()));
            ThTagParametersService.Rainpipe = ComBoxBalconyRain.SelectedItem != null ? double.Parse(GetDoubleString(ComBoxBalconyRain.SelectedItem.ToString())) : double.Parse(GetDoubleString(ComBoxSpec.SelectedItem.ToString()));
            ThTagParametersService.Npipe = ComBoxCondensation.SelectedItem != null ? double.Parse(GetDoubleString(ComBoxCondensation.SelectedItem.ToString())) : double.Parse(GetDoubleString(ComBoxSpec.SelectedItem.ToString()));
            ThTagParametersService.KaTFpipe = ComBoxHutchWastewater.SelectedItem != null ? double.Parse(GetDoubleString(ComBoxHutchWastewater.SelectedItem.ToString())) : double.Parse(GetDoubleString(ComBoxSpec.SelectedItem.ToString()));
            ThTagParametersService.BalconyFpipe = ComBoxBalconyWastewater.SelectedItem != null ? double.Parse(GetDoubleString(ComBoxBalconyWastewater.SelectedItem.ToString())) : double.Parse(GetDoubleString(ComBoxSpec.SelectedItem.ToString()));
            ThTagParametersService.ToiletWpipe = ComBoxSewagePipe.SelectedItem != null ? double.Parse(GetDoubleString(ComBoxSewagePipe.SelectedItem.ToString())) : double.Parse(GetDoubleString(ComBoxSpec.SelectedItem.ToString()));
            ThTagParametersService.ToiletTpipe = ComBoxVentStack.SelectedItem != null ? double.Parse(GetDoubleString(ComBoxVentStack.SelectedItem.ToString())) : double.Parse(GetDoubleString(ComBoxSpec.SelectedItem.ToString()));
            ThTagParametersService.IsSeparation = RidDrainageWay.EditValue as string == "污废分流";
            ThTagParametersService.IsCaisson = CheckToiletCaisson.Checked;
            ThTagParametersService.PipeLayer = "W-DRAI-SEWA-PIPE";
        }
        private static string GetDoubleString(string s)
        {
            string result = "";
            if (s != null)
            {
                result = s.Substring(2, s.Length - 2);
            }
            return result;
        }
        private void RidDrainageWay_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void ComBoxBSpec_SelectedIndexChanged(object sender, EventArgs e)
        {


        }

        private void ComBoxLSpec_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void RidLRroofDrain_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void ComBoxLRoofDrain_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
