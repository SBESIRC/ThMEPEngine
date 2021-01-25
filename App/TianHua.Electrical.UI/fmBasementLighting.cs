using AcHelper;
using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThMEPElectrical;
using ThMEPLighting;
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
        protected override void OnClosing(CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
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
        #region----------Commands----------
        private void BtnLaneCenterline_Click(object sender, EventArgs e)
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THTCD");
        }

        private void BtnDraw_Click(object sender, EventArgs e)
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THDXC");
        }

        private void BtnNoDraw_Click(object sender, EventArgs e)
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THFDXC");
        }

        private void BtnLayout_Click(object sender, EventArgs e)
        {
            CollectParameter();
            ThMEPLightingService.Instance.LightArrangeUiParameter.AutoGenerate = true;
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THCDZMBZ");
        }

        private void BtnCircuitLabel_Click(object sender, EventArgs e)
        {
            CollectParameter();
            ThMEPLightingService.Instance.LightArrangeUiParameter.AutoGenerate = false;
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THCDBH");
        }

        private void BtnCircuitInfo_Click(object sender, EventArgs e)
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THCDTJ");
        }

        private void CollectParameter()
        {
            ThMEPLightingService.Instance.LightArrangeUiParameter.IsSingleRow = FuncStr.NullToStr(RadLamp.EditValue) == "单排";
            double width = 0.0;
            if(double.TryParse(TxtWidth.Text,out width))
            {
                ThMEPLightingService.Instance.LightArrangeUiParameter.Width = width;
            }
            double racywaySpace = 0.0;
            if (double.TryParse(TxtDistance.Text, out racywaySpace))
            {
                ThMEPLightingService.Instance.LightArrangeUiParameter.RacywaySpace = racywaySpace;
            }
            double lightInterval = 0.0;
            if (double.TryParse(TxtSpacing.Text, out lightInterval))
            {
                ThMEPLightingService.Instance.LightArrangeUiParameter.Interval = lightInterval;
            }
            if (FuncStr.NullToStr(RadCircuit.EditValue) == "自动计算")
            {
                ThMEPLightingService.Instance.LightArrangeUiParameter.AutoCalculate = true;
            }
            else
            {
                int loopNumber = 0;
                if (int.TryParse(TxtCircuitNum.Text, out loopNumber))
                {
                    ThMEPLightingService.Instance.LightArrangeUiParameter.LoopNumber = loopNumber;
                }
            }
        }
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
        #endregion
    }
}
