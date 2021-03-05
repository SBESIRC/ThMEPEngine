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

            ComBoxSewagePipe.Properties.Items.AddRange(new List<string> { "DN100",  "DN150", "DN200" });
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
            if(FuncStr.NullToStr( RidDrainageWay.EditValue) == "污废合流")
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
    }
}
