using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Relate;

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
                var transformer = ThMEPWSSUtils.GetTransformer(frames);
                //var transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));


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
                    TchPipeData = dataFactory.TchPipeData,
                    SprinklerPt = dataFactory.SprinklerPtData,
                    AxisCurvesData = dataFactory.AxisCurves,
                };

                // dataQuery.Transform(transformer);
                dataProcess.ProcessData();
                dataProcess.Print();

            }
        }

        [CommandMethod("TIANHUACAD", "-THPLBZ", CommandFlags.Modal)]
        public void ThSprinklerDimNoUI()
        {
            using (var cmd = new ThSprinklerDimCmd())
            {
                cmd.Execute();
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThSprinklerDimTestDim", CommandFlags.Modal)]
        public void ThSprinklerDimTestDim()
        {

            var pts = new List<Point3d>();
            pts.Add(new Point3d(0, 0, 0));
            pts.Add(new Point3d(1000, 1000, 0));
            pts.Add(new Point3d(2000, 2000, 0));
            var dir = (pts.Last() - pts.First()).GetNormal().RotateBy(90 * System.Math.PI / 180, Vector3d.ZAxis);
            var dist = 800;

            var dim = new ThSprinklerDimension(pts, dir, dist);

            var caddim = ThInsertDimToDBService.ToCADDim(new List<ThSprinklerDimension>() { dim });
            ThInsertDimToDBService.InsertDim(caddim);
        }
    }
}
