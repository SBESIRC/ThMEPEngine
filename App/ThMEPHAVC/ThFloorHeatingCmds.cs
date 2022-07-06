using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using Dreambuild.AutoCAD;

using ThMEPEngineCore.Algorithm;

using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Service;
using ThMEPHVAC.FloorHeatingCoil.Model;


namespace ThMEPHVAC
{
    public class ThFloorHeatingCmds
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThFloorHeatingData", CommandFlags.Modal)]
        public void ThFloorHeatingData()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var blkNameDict = new Dictionary<string, List<string>> {
                    {"单盆洗手台",new List<string> {"A-Toilate-1"} }
                                };


                //画框，提数据，转数据
                var selectPts = ThSelectFrameUtil.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                var transformer = ThMEPHVACCommonUtils.GetTransformer(selectPts);
                transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

                var dataFactory = new ThFloorHeatingDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = blkNameDict,
                };

                dataFactory.GetElements(acadDatabase.Database, selectPts);

                var dataQuery = new ThFloorHeatingDataProcessService()
                {
                    WithUI = false,
                    InputExtractors = dataFactory.Extractors,
                    FurnitureObstacleData = dataFactory.SanitaryTerminal,
                    RoomSeparateLine = dataFactory.RoomSeparateLine,
                    RoomSuggestDist = dataFactory.RoomSuggestDist,
                    WaterSeparatorData = dataFactory.WaterSeparator,
                    FurnitureObstacleDataTemp = dataFactory.SenitaryTerminalOBBTemp,
                    RoomSetFrame = dataFactory.RoomSetFrame,
                };

                //dataQuery.Transform(transformer);
                dataQuery.ProcessData();
                dataQuery.Print();
                //dataQuery.Reset(transformer);

            }
        }

        [CommandMethod("TIANHUACAD", "ThFloorHeating", CommandFlags.Modal)]
        public void ThFloorHeating()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                ThFloorHeatingCoilSetting.Instance.BlockNameDict = new Dictionary<string, List<string>>
                {
                    { "单盆洗手台",new List<string> {"A-Toilate-1"} },
                };
                ThFloorHeatingCoilSetting.Instance.WithUI = false;

                using (var cmd = new ThFloorHeatingCmd())
                {
                    cmd.Execute();
                }


            }
        }



    }
}
