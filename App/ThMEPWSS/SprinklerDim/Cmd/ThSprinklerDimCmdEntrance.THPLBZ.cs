using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Relate;

using AcHelper.Commands;
using AcHelper;
using Linq2Acad;

using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.Common;
using ThMEPWSS.SprinklerDim.Data;
using ThMEPWSS.SprinklerDim.Engine;
using ThMEPWSS.SprinklerDim.Model;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Cmd
{

    public partial class ThSprinklerDimCmdEntrance
    {

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThSprinklerDimData", CommandFlags.Modal)]
        public void ThSprinklerDimData()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var frames = ThSelectFrameUtil.GetRoomFrame();
                if (frames.Count == 0)
                {
                    return;
                }

                //转换器
                //var transformer = ThMEPWSSUtils.GetTransformer(frames);
                var transformer = new ThMEPEngineCore.Algorithm.ThMEPOriginTransformer(new Point3d(0, 0, 0));


                //提取数据
                var dataFactory = new ThSprinklerDimDataFactory()
                {
                    Transformer = transformer,
                };

                dataFactory.GetElements(acadDatabase.Database, frames);

                //处理数据
                var dataProcess = new ThSprinklerDimDataProcessService()
                {
                    InputExtractors = dataFactory.Extractors,
                    RoomIFCData = dataFactory.RoomsData,
                    TchPipeData = dataFactory.TchPipeData,
                    LinePipeData = dataFactory.LinePipeData,
                    LinePipeTextData = dataFactory.LinePipeTextData,
                    SprinklerPt = dataFactory.SprinklerPtData,
                    AxisCurvesData = dataFactory.AxisCurves,
                    Transformer = transformer,
                };

                // dataQuery.Transform(transformer);
                dataProcess.ProcessData();
                dataProcess.Print();

            }
        }

        [CommandMethod("TIANHUACAD", "-THPLBZ", CommandFlags.Modal)]
        public void ThSprinklerDimNoUI()
        {
            var vm = new ThSprinklerDimViewModel();
            vm.UseTCHDim = 0;

            using (var cmd = new ThSprinklerDimCmd(vm))
            {
                cmd.Execute();
                ThMEPWSS.Common.Utils.FocusToCAD();
                if (vm.UseTCHDim == 1)
                {
                    Active.Document.SendCommand("THTCHPIPIMP" + "\n");
                    ThMEPWSS.Common.Utils.FocusToCAD();
                    //vm.DeleteDBFile();
                }
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThSprinklerDimTestDimCAD", CommandFlags.Modal)]
        public void ThSprinklerDimTestDimCAD()
        {
            var vm = new ThSprinklerDimViewModel();
            vm.UseTCHDim = 0;
            vm.Scale = 150;

            var pts = new List<Point3d>();
            pts.Add(new Point3d(0, 0, 0));
            pts.Add(new Point3d(1000, 1000, 0));
            pts.Add(new Point3d(3000, 3000, 0));
            var dir = (pts.Last() - pts.First()).GetNormal().RotateBy(90 * System.Math.PI / 180, Vector3d.ZAxis);
            var dist = 800;

            var dim = new ThSprinklerDimension(pts, dir, dist);

            ThSprinklerDimInsertService.ToCADDim(new List<ThSprinklerDimension>() { dim }, vm.Scale);

        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThSprinklerDimTestDimTCH", CommandFlags.Modal)]
        public void ThSprinklerDimTestDimTCH()
        {
            var vm = new ThSprinklerDimViewModel();
            vm.UseTCHDim = 1;
            vm.Scale = 150;

            var pts = new List<Point3d>();
            pts.Add(new Point3d(0, 0, 0));
            pts.Add(new Point3d(1000, 1000, 0));
            pts.Add(new Point3d(3000, 3000, 0));
            var dir = (pts.Last() - pts.First()).GetNormal().RotateBy((-1) * 90 * System.Math.PI / 180, Vector3d.ZAxis);
            var dist = 800;

            var dim = new ThSprinklerDimension(pts, dir, dist);

            ThSprinklerDimInsertService.ToTCHDim(new List<ThSprinklerDimension>() { dim }, vm.Scale, vm.TCHDBPath);
        }
    }
}
