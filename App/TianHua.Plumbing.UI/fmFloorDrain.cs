using System;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using AcHelper.Commands;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Service;
using DevExpress.XtraEditors;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Plumbing.UI
{
    public partial class fmFloorDrain : XtraForm
    {
        public fmFloorDrain()
        {
            InitializeComponent();
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }


        private void fmFloorDrain_Load(object sender, EventArgs e)
        {
            InitForm();
        }

        public void InitForm()
        {
            ListBox.DataSource = new List<string>(); 
        }

        private void BtnFloorFocus_Click(object sender, EventArgs e)
        {
            //聚焦到CAD
            SetFocusToDwgView();

            //发送命令
            CommandHandlerBase.ExecuteFromCommandLine(false, "THLGLC");
        }

        private void BtnGetFloor_Click(object sender, EventArgs e)
        {
            //聚焦到CAD
            SetFocusToDwgView();

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取楼层框线图块
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择楼层框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filter = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var storeys = result.Value.GetObjectIds()
                    .Select(o => acadDatabase.Element<BlockReference>(o))
                    .Where(o => o.GetEffectiveName() == ThWPipeCommon.STOREY_BLOCK_NAME)
                    .Select(o => o.ObjectId)
                    .ToObjectIdCollection();

                // 获取楼层名称
                var service = new ThReadStoreyInformationService();
                service.Read(storeys);

                // 绑定控件
                ListBox.DataSource = service.StoreyNames.Select(o => o.Item1).ToList();
            }
        }    

        private void BtnParam_Click(object sender, EventArgs e)
        {
            using (var dlg = new fmFDParam())
            {
                AcadApp.ShowModalDialog(dlg);
            }
        }

        private void BtnLayoutRiser_Click(object sender, EventArgs e)
        {
            //聚焦到CAD
            SetFocusToDwgView();

            //发送命令
            CommandHandlerBase.ExecuteFromCommandLine(false, "THLGBZ");
        }

        private void BtnUse_Click(object sender, EventArgs e)
        {
            bool apply = false;
            using (var dlg = new fmFDUse())
            {
                ThTagParametersService.sourceFloor = ListBox.SelectedItem as string;
                var result = AcadApp.ShowModalDialog(dlg);
                apply = result == DialogResult.OK;
            }
            if (apply)
            {
                //聚焦到CAD
                SetFocusToDwgView();

                //发送命令
                CommandHandlerBase.ExecuteFromCommandLine(false, "THLGYY");
            }
        }

        private void ListBox_DoubleClick(object sender, EventArgs e)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var storey = ListBox.SelectedItem as string;
                ThBlockSelectionEngine.ZoomToModels(storey);
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
    }
}
