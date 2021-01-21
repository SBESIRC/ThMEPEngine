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

namespace TianHua.Electrical.UI
{
    public partial class fmBasementLighting : DevExpress.XtraEditors.XtraForm
    {
        public fmBasementLighting()
        {
            InitializeComponent();
        }

        public void InitForm()
        {
            RadLUX_SelectedIndexChanged(null, null);
            RadCircuit_SelectedIndexChanged(null,null);
        }
        private void fmBasementLighting_Load(object sender, EventArgs e)
        {
            InitForm();

        }

        private void BtnLaneCenterline_Click(object sender, EventArgs e)
        {
      
        }

        private void BtnDraw_Click(object sender, EventArgs e)
        {

        }

        private void BtnNoDraw_Click(object sender, EventArgs e)
        {

        }

        private void RadLUX_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(RadLUX.EditValue) == "30lx(住宅非人防)")
            {
                var _Single = RadLamp.Properties.Items.GetItemByValue("单排");
                var _Double = RadLamp.Properties.Items.GetItemByValue("双排");
                _Single.Enabled = true;
                _Double.Enabled = true;
                RadLamp.EditValue = "单排";
            }
            else
            {
                var _Single = RadLamp.Properties.Items.GetItemByValue("单排");
                var _Double = RadLamp.Properties.Items.GetItemByValue("双排");
                _Single.Enabled = false;
                _Double.Enabled = true;
                RadLamp.EditValue = "双排";
            }

        }

        private void RadLamp_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(RadLamp.EditValue) == "单排")
            {
                TxtSpacing.Text = "2700";

                TxtCircuitNum.Text = "3";
            }
            else
            {
                if (FuncStr.NullToStr(RadLUX.EditValue) == "30lx(住宅非人防)")
                {
                    TxtSpacing.Text = "5400";
                }
                else
                {
                    TxtSpacing.Text = "4000";
                }

          
                TxtCircuitNum.Text = "4";
            }
        }

        private void RadCircuit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(FuncStr.NullToStr(RadCircuit.EditValue) == "自动计算")
            {
                TxtCircuitNum.Enabled = false;
            }
            else
            {
                TxtCircuitNum.Enabled = true;
            }

      
        }

        private void fmBasementLighting_Activated(object sender, EventArgs e)
        {
            BtnLayout.Focus();
        }
    }
}
