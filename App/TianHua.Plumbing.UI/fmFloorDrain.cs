using System;
using AcHelper;
using Linq2Acad;
using AcHelper.Commands;
using ThMEPWSS.Pipe.Service;
using DevExpress.XtraEditors;
using System.Collections.Generic;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Plumbing.UI
{
    public partial class fmFloorDrain : XtraForm
    {
        public fmFloorDrain()
        {
            InitializeComponent();
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
            var floorNames = new List<string>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var storey = new ThReadStoreyInformationService();
                storey.Read(acadDatabase.Database);
                if (storey.StoreyNames.Count == 0)
                {
                    return;
                }
                storey.StoreyNames.ForEach(o => floorNames.Add(o.Item1));
            }
            ListBox.DataSource = floorNames;
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
            ThTagParametersService.sourceFloor = ListBox.SelectedItem as string;
            using (var dlg = new fmFDUse())
            {
                AcadApp.ShowModalDialog(dlg);
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

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
