using System.Collections.Generic;

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
                    TchPipeData = dataFactory.TchPipeData,
                    SprinklerPt = dataFactory.SprinklerPtData,

                };

                // dataQuery.Transform(transformer);
                dataProcess.CreateTchPipe();
                dataProcess.ProjectOntoXYPlane();
                dataProcess.Print();

            }
        }


        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThSprinklerDimNoUI", CommandFlags.Modal)]
        public void ThSprinklerDimNoUI()
        {
            using (var cmd = new ThSprinklerDimCmd())
            {
                cmd.Execute();
            }
        }
    }
}
