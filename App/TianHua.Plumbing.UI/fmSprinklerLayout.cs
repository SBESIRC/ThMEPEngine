﻿using System;
using System.Drawing;
using System.Windows.Forms;
using AcHelper;
using AcHelper.Commands;
using ThMEPWSS.Service;
using DevExpress.XtraEditors;
using TianHua.Publics.BaseCode;

namespace TianHua.Plumbing.UI
{
    public partial class fmSprinklerLayout : DevExpress.XtraEditors.XtraForm
    {
        public fmSprinklerLayout()
        {
            InitializeComponent();
        }

        private void RidApplications_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(RidApplications.EditValue) == "除走道外")
            {
                this.Size = new Size(195, 385);
             
                layoutControlItem10.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem11.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem13.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                //emptySpaceItem2.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;

                layoutControlItem1.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem2.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem3.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                //emptySpaceItem1.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem7.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem9.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem4.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem5.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem6.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
            }
            if (FuncStr.NullToStr(RidApplications.EditValue) == "走道&坡道")
            {
                this.Size = new Size(195, 163);

                layoutControlItem10.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem11.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutControlItem13.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                //emptySpaceItem2.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;

                layoutControlItem1.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem2.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem3.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                //emptySpaceItem1.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem7.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem9.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem4.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem5.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutControlItem6.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
            }
        }

        private void fmSprinklerLayout_Load(object sender, EventArgs e)
        {
            RidApplications_SelectedIndexChanged(null, null);
        }

        private void BtnLayout_Click(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(ComBoxDeadZone.EditValue) == "圆形-高精度")
            {
                if (XtraMessageBox.Show("高精度的圆形盲区检测耗时较长,是否继续？", "提示", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }

            //聚焦到CAD
            SetFocusToDwgView();

            //获取更改信息
            CreateChangedInfo();

            //发送命令
            CommandHandlerBase.ExecuteFromCommandLine(false, "THPLPT");
        }

        private void BtnCheck_Click(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(ComBoxDeadZone.EditValue) == "圆形-高精度")
            {
                if (XtraMessageBox.Show("高精度的圆形盲区检测耗时较长,是否继续？", "提示", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }

            //聚焦到CAD
            SetFocusToDwgView();

            //获取更改信息
            CreateChangedInfo();

            //发送命令
            CommandHandlerBase.ExecuteFromCommandLine(false, "THPLMQ");
        }

        private void BtnArea_Click(object sender, EventArgs e)
        {
            //聚焦到CAD
            SetFocusToDwgView();

            //获取更改信息
            CreateChangedInfo();

            //发送命令
            CommandHandlerBase.ExecuteFromCommandLine(false, "THPLKQ");
        }

        private void BtnAlongTheLine_Click(object sender, EventArgs e)
        {
            //聚焦到CAD
            SetFocusToDwgView();

            //获取更改信息
            CreateChangedInfo();

            //喷头间距
            ThWSSUIService.Instance.Parameter.distance = Convert.ToDouble(TxtSpacing.Text);

            //发送命令
            CommandHandlerBase.ExecuteFromCommandLine(false, "THPLCD");
        }

        private void TxtSpacing_EditValueChanged(object sender, EventArgs e)
        {
            if(FuncStr.NullToInt(TxtSpacing.EditValue) < 1800)
            {
                TxtSpacing.EditValue = 1800;
            }
        }

        private void CreateChangedInfo()
        {
            ComBoxHazardLevel_SelectedIndexChanged();       //危险等级
            RidSprinklerScope_SelectedIndexChanged();       //喷头覆盖范围
            RidSprinklerType_SelectedIndexChanged();        //上喷下喷
            CheckGirder_CheckedChanged();                   //是否考虑梁
            ComBoxDeadZone_SelectedIndexChanged();          //盲区表达方式
        }

        /// <summary>
        /// 危险等级
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComBoxHazardLevel_SelectedIndexChanged()
        {
            if (ComBoxHazardLevel.Text == "轻危险级")
            {
                ThWSSUIService.Instance.Parameter.hazardLevel = ThMEPWSS.Model.HazardLevel.FirstLevel;
            }
            else if (ComBoxHazardLevel.Text == "中危险等级I级")
            {
                ThWSSUIService.Instance.Parameter.hazardLevel = ThMEPWSS.Model.HazardLevel.SecondLevel;
            }
            else if (ComBoxHazardLevel.Text == "中危险等级II级")
            {
                ThWSSUIService.Instance.Parameter.hazardLevel = ThMEPWSS.Model.HazardLevel.ThirdLevel;
            }
            else if (ComBoxHazardLevel.Text == "严重危险级")
            {
                ThWSSUIService.Instance.Parameter.hazardLevel = ThMEPWSS.Model.HazardLevel.SeriousLevel;
            }
        }
        
        /// <summary>
        /// 喷头覆盖范围
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RidSprinklerScope_SelectedIndexChanged()
        {
            if (this.RidSprinklerScope.SelectedIndex == 0)
            {
                ThWSSUIService.Instance.Parameter.layoutRange = ThMEPWSS.Model.LayoutRange.StandardRange;
            }
            else if (this.RidSprinklerScope.SelectedIndex == 1)
            {
                ThWSSUIService.Instance.Parameter.layoutRange = ThMEPWSS.Model.LayoutRange.ExpandRange;
            }
        }

        /// <summary>
        /// 上喷下喷
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RidSprinklerType_SelectedIndexChanged()
        {
            if (RidSprinklerType.SelectedIndex == 0)
            {
                ThWSSUIService.Instance.Parameter.layoutType = ThMEPWSS.Model.LayoutType.UpSpray;
            }
            else if (RidSprinklerType.SelectedIndex == 1)
            {
                ThWSSUIService.Instance.Parameter.layoutType = ThMEPWSS.Model.LayoutType.DownSpray;
            }
        }

        /// <summary>
        /// 是否考虑梁
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckGirder_CheckedChanged()
        {
            if (CheckGirder.Checked)
            {
                ThWSSUIService.Instance.Parameter.ConsiderBeam = true;
            }
            else
            {
                ThWSSUIService.Instance.Parameter.ConsiderBeam = false;
            }
        }

        /// <summary>
        /// 盲区表达方式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComBoxDeadZone_SelectedIndexChanged()
        {
            if (ComBoxDeadZone.Text == "矩形")
            {
                ThWSSUIService.Instance.Parameter.blindAreaType = ThMEPWSS.Model.BlindAreaType.Rectangle;
            }
            else if (ComBoxDeadZone.Text == "圆形-低精度")
            {
                ThWSSUIService.Instance.Parameter.blindAreaType = ThMEPWSS.Model.BlindAreaType.SmallCircle;
            }
            else if (ComBoxDeadZone.Text == "圆形-中精度")
            {
                ThWSSUIService.Instance.Parameter.blindAreaType = ThMEPWSS.Model.BlindAreaType.MedianCircle;
            }
            else if (ComBoxDeadZone.Text == "圆形-高精度")
            {
                ThWSSUIService.Instance.Parameter.blindAreaType = ThMEPWSS.Model.BlindAreaType.BigCircle;
            }
        }

        /// <summary>
        /// 聚焦到CAD
        /// </summary>
         private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
