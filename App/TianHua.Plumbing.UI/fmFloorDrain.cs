using System;
using AcHelper;
using Linq2Acad;
using AcHelper.Commands;
using ThMEPWSS.Pipe.Service;
using DevExpress.XtraEditors;
using System.Collections.Generic;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.EditorInput;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;

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
                var input=SelectPoints();
                var points =new Point3dCollection();
                points.Add(input.Item1);
                points.Add(new Point3d(input.Item1.X, input.Item2.Y,0));
                points.Add(input.Item2);
                points.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
                ThTagParametersService.framePoints = points;
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
        private Tuple<Point3d,Point3d> SelectPoints()
        {
           var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if(ptRightRes.Status== PromptStatus.OK)
            {
                return Tuple.Create(leftDownPt, ptRightRes.Value);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
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
