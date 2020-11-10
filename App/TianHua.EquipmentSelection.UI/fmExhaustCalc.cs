using System;
using System.IO;
using ThCADExtension;
using System.Windows.Forms;
using TianHua.FanSelection.Function;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.UI
{
    public partial class fmExhaustCalc : DevExpress.XtraEditors.XtraForm
    {
        public FanDataModel Model { get; set; }
        private Action OnVmaxChanged { get; set; }
        private Action OnGeneralChanged { get; set; }
        private HeatReleaseInfoLoader Loader { get; set; }

        public fmExhaustCalc()
        {
            InitializeComponent();
        }

        public void InitForm(FanDataModel _FanDataModel, string type)
        {
            var _Json = FuncJson.Serialize(_FanDataModel);
            Model = FuncJson.Deserialize<FanDataModel>(_Json);
            Loader = new HeatReleaseInfoLoader();
            Loader.LoadFromFile(Path.Combine(ThCADCommon.SupportPath(), "火灾达到静态时的热释放速率.json"));

            OnGeneralChanged += TotalUpdate;
            OnVmaxChanged += UpdateVmax;
            Model.ExhaustModel.ExhaustCalcType = type;
            InitsidePanel2(type, OnGeneralChanged);
            InitsidePanel1(Model.ExhaustModel.PlumeSelection, OnVmaxChanged);
            //if (type == "空间-净高小于等于6m")
            //{
            //    this.TxtHRR.Enabled = false;
            //    this.ComBoxPlume.Enabled = false;
            //}
            this.TxtHRR.Text = ExhaustModelCalculator.GetHeatReleaseRate(Loader, Model.ExhaustModel).ToString();
            Model.ExhaustModel.HeatReleaseRate = this.TxtHRR.Text;

            if (Model.ExhaustModel.PlumeSelection.IsNullOrEmptyOrWhiteSpace())
            {
                this.ComBoxPlume.SelectedIndex = 0;
            }
            else
            {
                this.ComBoxPlume.Text = Model.ExhaustModel.PlumeSelection;
            }

            this.TxtLength.Text = Model.ExhaustModel.SmokeLength;
            this.TxtWidth.Text = Model.ExhaustModel.SmokeWidth;
            this.TxtDiameter.Text = Model.ExhaustModel.SmokeDiameter;
            this.TxtSmokePosition.Text = Model.ExhaustModel.SmokeFactorValue;
            if (Model.ExhaustModel.SmokeFactorOption.IsNullOrEmptyOrWhiteSpace())
            {
                this.ComBoxWZ.SelectedIndex = 0;
            }
            else
            {
                this.ComBoxWZ.Text = Model.ExhaustModel.SmokeFactorOption;
            }
            this.TxtSmokeLayerThickness.Text = Model.ExhaustModel.SmokeThickness;
            this.TxtVmax.Text = ExhaustModelCalculator.GetMaxSmoke(Model.ExhaustModel);
            switch (type)
            {
                case "空间-净高小于等于6m":
                    this.Height = 780;
                    break;
                case "空间-净高大于6m":
                    this.Height = 720;
                    break;
                case "空间-汽车库":
                    this.Height = 690;
                    break;
                case "走道回廊-仅走道或回廊设置排烟":
                    this.Height = 710;
                    break;
                case "走道回廊-房间内和走道或回廊都设置排烟":
                    this.Height = 780;
                    break;
                case "中庭-周围场所设有排烟系统":
                case "中庭-周围场所不设排烟系统":
                    this.Height = 780;
                    break;
                default:
                    break;
            }
        }

        public void InitsidePanel2(string type, Action action)
        {
            this.sidePanel2.Controls.Clear();
            CtlExhaustControlBase sidePanelcontrol = new CtlExhaustControlBase();
            switch (type)
            {
                case "空间-净高小于等于6m":
                    sidePanelcontrol = new CtlLessThan6();
                    break;
                case "空间-净高大于6m":
                    sidePanelcontrol = new CtlGreaterThan6();
                    break;
                case "空间-汽车库":
                    sidePanelcontrol = new CtlGarage();
                    break;
                case "走道回廊-仅走道或回廊设置排烟":
                    sidePanelcontrol = new CtlCloister();
                    break;
                case "走道回廊-房间内和走道或回廊都设置排烟":
                    sidePanelcontrol = new CtlCloistersAndRooms();
                    break;
                case "中庭-周围场所设有排烟系统":
                case "中庭-周围场所不设排烟系统":
                    sidePanelcontrol = new CtlSmokeExtraction();
                    break;
                default:
                    break;
            }
            sidePanelcontrol.InitForm(Model, action);
            this.sidePanel2.Controls.Add(sidePanelcontrol);
            sidePanelcontrol.Dock = DockStyle.Fill;

        }

        public void InitsidePanel1(string Plume, Action action)
        {
            this.sidePanel1.Controls.Clear();
            CtlExhaustControlBase sidePanelcontrol = new CtlExhaustControlBase();
            switch (Plume)
            {
                case "轴对称型":
                    sidePanelcontrol = new CtlAxisymmetric();
                    break;
                case "阳台溢出型":
                    sidePanelcontrol = new CtlBalconyOverFlow();
                    break;
                case "窗口型":
                    sidePanelcontrol = new CtlWindowOpen();
                    break;
                default:
                    sidePanelcontrol = new CtlAxisymmetric();
                    break;
            }
            sidePanelcontrol.InitForm(Model, action);
            this.sidePanel1.Controls.Add(sidePanelcontrol);
            sidePanelcontrol.Dock = DockStyle.Fill;
            //if (Model.ExhaustModel.ExhaustCalcType == "空间-净高小于等于6m")
            //{
            //    sidePanelcontrol.Enabled = false;
            //}
        }

        private void TxtHRRChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.HeatReleaseRate = TxtHRR.Text;
        }

        private void sidePanel2_Leave(object sender, EventArgs e)
        {
            TotalUpdate();
        }

        private void PlumeSelectedChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.PlumeSelection = this.ComBoxPlume.Text;
            InitsidePanel1(this.ComBoxPlume.Text, OnVmaxChanged);
            TotalUpdate();
        }

        private void TxtLengthValueChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SmokeLength = this.TxtLength.Text;
            Model.ExhaustModel.SmokeDiameter = ExhaustModelCalculator.GetSmokeDiameter(Model.ExhaustModel).ToString();
            this.TxtDiameter.Text = Model.ExhaustModel.SmokeDiameter;
        }

        private void TxtWidthValueChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SmokeWidth = this.TxtWidth.Text;
            Model.ExhaustModel.SmokeDiameter = ExhaustModelCalculator.GetSmokeDiameter(Model.ExhaustModel).ToString();
            this.TxtDiameter.Text = Model.ExhaustModel.SmokeDiameter;
        }

        private void SmokeFactorOptionSelectChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SmokeFactorOption = this.ComBoxWZ.Text;
            Model.ExhaustModel.SmokeFactorValue = ExhaustModelCalculator.GetSmokeFactor(Model.ExhaustModel).ToString();
            this.TxtSmokePosition.Text = ExhaustModelCalculator.GetSmokeFactor(Model.ExhaustModel).ToString();
        }

        private void SmokeLayerThicknessChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SmokeThickness = this.TxtSmokeLayerThickness.Text;
            this.TxtVmax.Text = ExhaustModelCalculator.GetMaxSmoke(Model.ExhaustModel);
            Model.ExhaustModel.MaxSmokeExtraction = this.TxtVmax.Text;
        }

        private void SmokePositionChanged(object sender, EventArgs e)
        {
            this.TxtVmax.Text = ExhaustModelCalculator.GetMaxSmoke(Model.ExhaustModel);
            Model.ExhaustModel.MaxSmokeExtraction = this.TxtVmax.Text;
        }

        private void sidePanel1_Leave(object sender, EventArgs e)
        {
            TotalUpdate();
        }

        private void TotalUpdate()
        {
            this.TxtHRR.Text = ExhaustModelCalculator.GetHeatReleaseRate(Loader, Model.ExhaustModel).ToString();
            Model.ExhaustModel.HeatReleaseRate = this.TxtHRR.Text;

            CtlExhaustControlBase sidePanelcontrol = sidePanel1.Controls[0] as CtlExhaustControlBase;
            sidePanelcontrol.UpdateCalcAirVolum(Model.ExhaustModel);

            UpdateVmax();
        }

        private void UpdateVmax()
        {
            this.TxtHRR.Text = ExhaustModelCalculator.GetHeatReleaseRate(Loader, Model.ExhaustModel).ToString();
            Model.ExhaustModel.HeatReleaseRate = this.TxtHRR.Text;

            this.TxtVmax.Text = ExhaustModelCalculator.GetMaxSmoke(Model.ExhaustModel);
            Model.ExhaustModel.MaxSmokeExtraction = this.TxtVmax.Text;
        }
    }
}
