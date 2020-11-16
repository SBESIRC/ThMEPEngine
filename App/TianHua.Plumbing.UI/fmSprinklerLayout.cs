using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TianHua.Publics.BaseCode;

namespace TianHua.Plumbing.UI
{
    public partial class fmSprinklerLayout : DevExpress.XtraEditors.XtraForm
    {
        //TODO:
        //ComBoxHazardLevel.EditValue  危险等级
        //RidSprinklerScope.EditValue  喷头范围
        //RidSprinklerType.EditValue   喷头类型
        //CheckGirder.Check            考虑梁
        //ComBoxDeadZone.EditValue     盲区表达
        //TxtSpacing.Text              喷头间距
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


        }

        private void BtnArea_Click(object sender, EventArgs e)
        {
              fmWaiting.WaitingExcute(() => { Test(); }, "正在处理，请稍后....");   
        }

        public void Test()
        {
            Thread.Sleep(5000);
        }

        private void BtnAlongTheLine_Click(object sender, EventArgs e)
        {
   
        }

        private void TxtSpacing_EditValueChanged(object sender, EventArgs e)
        {
            if(FuncStr.NullToInt(TxtSpacing.EditValue) < 1800)
            {
                TxtSpacing.EditValue = 1800;
            }
        }
    }
}
