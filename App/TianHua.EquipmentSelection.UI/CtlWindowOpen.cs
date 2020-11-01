using System;
using TianHua.FanSelection.Model;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI
{
    public partial class CtlWindowOpen : CtlExhaustControlBase
    {
        private FanDataModel Model { get; set; }
        private Action OnMinVolumChanged { get; set; }

        public CtlWindowOpen()
        {
            InitializeComponent();
        }

        public override void InitForm(FanDataModel _FanDataModel,Action action)
        {
            Model = _FanDataModel;
            OnMinVolumChanged = action;
            this.TxtWindowArea.Text = Model.ExhaustModel.Window_WindowArea;
            this.TxtWindowHeight.Text = Model.ExhaustModel.Window_WindowHeight;
            this.TxtSmokeToBottom.Text = Model.ExhaustModel.Window_SmokeBottom;
        }

        private void WindowAreaChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.Window_WindowArea = this.TxtWindowArea.Text;
            UpdateCalcAirVolum(Model.ExhaustModel);
        }

        private void WindowHeightChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.Window_WindowHeight = this.TxtWindowHeight.Text;
            UpdateCalcAirVolum(Model.ExhaustModel);
        }

        private void SmokeToBottomChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.Window_SmokeBottom = this.TxtSmokeToBottom.Text;
            UpdateCalcAirVolum(Model.ExhaustModel);
        }

        public override void UpdateCalcAirVolum(ExhaustCalcModel model)
        {
            if (Model == null)
            {
                return;
            }
            this.TxtCalculateVolume.Text = ExhaustModelCalculator.GetCalcAirVolum(model);
            Model.ExhaustModel.Window_CalcAirVolum = this.TxtCalculateVolume.Text;
            Model.ExhaustModel.Final_CalcAirVolum = Model.ExhaustModel.Window_CalcAirVolum;
            OnMinVolumChanged();
        }
    }
}
