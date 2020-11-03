using System;
using ThMEPElectrical;
using AcHelper.Commands;
using ThMEPElectrical.Model;
using TianHua.Publics.BaseCode;
using System.Collections.Generic;
using DevExpress.XtraEditors;

namespace TianHua.Electrical.UI
{
    public partial class fmSmokeLayout : DevExpress.XtraEditors.XtraForm, ISmokeLayout
    {

        public SmokeLayoutDataModel m_SmokeLayout { get; set; }

        public List<string> m_ListSmokeRoomArea { get; set; }

        public List<string> m_ListThalposisRoomArea { get; set; }

        public List<string> m_ListSmokeRoomHeight { get; set; }

        public List<string> m_ListThalposisRoomHeight { get; set; }


        public PresenterSmokeLayout m_Presenter;

        public void RessetPresenter()
        {
            if (m_Presenter != null)
            {
                this.Dispose();
                m_Presenter = null;
            }
            m_Presenter = new PresenterSmokeLayout(this);
        }

        public fmSmokeLayout()
        {
            InitializeComponent();
        }


        private void fmSmokeLayout_Load(object sender, EventArgs e)
        {
            RessetPresenter();
            RidSmoke_CheckedChanged(null, null);
        }

        private void PicSmoke_Click(object sender, EventArgs e)
        {
            RidSmoke.PerformClick();
        }

        private void PicThalposis_Click(object sender, EventArgs e)
        {
            RidTemperature.PerformClick();
        }

        private void RidSmoke_CheckedChanged(object sender, EventArgs e)
        {
            if (RidSmoke.Checked)
            {
                ComBoxArea.Properties.Items.Clear();

                ComBoxArea.Properties.Items.AddRange(m_ListSmokeRoomArea);

                ComBoxArea.EditValue = "S＞80";

                ComBoxHeight.Properties.Items.Clear();

                ComBoxHeight.Properties.Items.AddRange(m_ListSmokeRoomHeight);

                ComBoxHeight.EditValue = "h≤6";
            }
        }

        private void RidTemperature_CheckedChanged(object sender, EventArgs e)
        {
            if (RidTemperature.Checked)
            {
                ComBoxArea.Properties.Items.Clear();

                ComBoxArea.Properties.Items.AddRange(m_ListThalposisRoomArea);

                ComBoxArea.EditValue = "S＞30";

                ComBoxHeight.Properties.Items.Clear();

                ComBoxHeight.Properties.Items.AddRange(m_ListThalposisRoomHeight);

                ComBoxHeight.EditValue = "h≤8";
            }
        }

        private void RidGarage_CheckedChanged(object sender, EventArgs e)
        {
            if (RidGarage.Checked)
            {
                BtnLayout.Enabled = true;
                RidNoCeiling.Enabled = true;
                RidCeiling.Enabled = true;
                RidNoBeam.Enabled = true;
                TxtBeam.Enabled = true;
                TxtRoofThickness.Enabled = true;
                ComBoxArea.Enabled = true;
                ComBoxHeight.Enabled = true;
                ComBoxSlope.Enabled = true;
            }
        }

        private void RidAisle_CheckedChanged(object sender, EventArgs e)
        {
            if (RidAisle.Checked)
            {
                BtnLayout.Enabled = false;
                RidNoCeiling.Enabled = false;
                RidCeiling.Enabled = false;
                RidNoBeam.Enabled = false;
                TxtBeam.Enabled = false;
                TxtRoofThickness.Enabled = false;
                ComBoxArea.Enabled = false;
                ComBoxHeight.Enabled = false;
                ComBoxSlope.Enabled = false;
            }
        }

        private void RidNoCeiling_CheckedChanged(object sender, EventArgs e)
        {
            if (RidNoCeiling.Checked)
            {
                BtnLayout.Enabled = true;
                RidNoCeiling.Enabled = true;
                RidCeiling.Enabled = true;
                RidNoBeam.Enabled = true;
                TxtBeam.Enabled = true;
                TxtRoofThickness.Enabled = true;
                ComBoxArea.Enabled = true;
                ComBoxHeight.Enabled = true;
                ComBoxSlope.Enabled = true;
            }
        }

        private void RidCeiling_CheckedChanged(object sender, EventArgs e)
        {
            if (RidCeiling.Checked)
            {
                TxtBeam.Enabled = false;
                TxtRoofThickness.Enabled = false;
            }
        }

        private void RidNoBeam_CheckedChanged(object sender, EventArgs e)
        {
            if (RidNoBeam.Checked)
            {
                TxtBeam.Enabled = false;
                TxtRoofThickness.Enabled = false;
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (m_SmokeLayout == null) { m_SmokeLayout = new SmokeLayoutDataModel(); }

            m_SmokeLayout.LayoutType = RidSmoke.Checked ? RidSmoke.Text : RidTemperature.Text;

            m_SmokeLayout.AreaLayout = RidGarage.Checked ? RidGarage.Text : RidAisle.Text;

            if (RidNoCeiling.Checked)
                m_SmokeLayout.LayoutLogic = RidNoCeiling.Text;

            if (RidCeiling.Checked)
                m_SmokeLayout.LayoutLogic = RidCeiling.Text;

            if (RidNoBeam.Checked)
                m_SmokeLayout.LayoutLogic = RidNoBeam.Text;

            m_SmokeLayout.BeamDepth = FuncStr.NullToInt(TxtBeam.Text);

            m_SmokeLayout.RoofThickness = FuncStr.NullToInt(TxtRoofThickness.Text);

            m_SmokeLayout.RoomArea = FuncStr.NullToStr(ComBoxArea.Text);

            m_SmokeLayout.RoomHeight = FuncStr.NullToStr(ComBoxHeight.Text);

            m_SmokeLayout.SlopeRoof = FuncStr.NullToStr(ComBoxSlope.Text);


            // 设置参数
            ThMEPElectricalService.Instance.Parameter = new PlaceParameter()
            {
                RoofThickness = m_SmokeLayout.RoofThickness,
            };
            // 发送命令
            switch(m_SmokeLayout.LayoutLogic)
            {
                case "无吊顶避梁":
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFDL");
                    break;
                case "有吊顶避梁":
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFDCP");
                    break;
                case "无梁楼盖":
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFDFS");
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void CheckProtect_CheckedChanged(object sender, EventArgs e)
        {
            var checkbox = (CheckEdit)sender;
            var parameters = new string[]
            {
                checkbox.Checked ? "_ON" : "_OFF",
                "E-FD-PR", 
                "\n", 
                "\n"
            };
            CommandHandlerBase.ExecuteFromCommandLine(false, "-LAYER", parameters);
        }

        private void BtnLayout_Click(object sender, EventArgs e)
        {
            CommandHandlerBase.ExecuteFromCommandLine(false, "THUCSCOMPASS");
        }
    }
}
