using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using NetTopologySuite.Operation.Relate;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Command;

using ThMEPWSS.DrainageSystemDiagram;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.SprinklerConnect.Model;

namespace ThMEPWSS.SprinklerConnect.Cmd
{
    public class ThSprinklerConnectCmd_test : ThMEPBaseCommand
    {
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();

        public ThSprinklerConnectCmd_test()
        {

        }

        public override void SubExecute()
        {
            SprinklerConnectExecute();
        }

        public void SprinklerConnectExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var frame = ThSprinklerDataService.GetFrame();
                if (frame == null || frame.Area < 10)
                {
                    return;
                }

                //简略的写提取管子和点位（需要改）
                var SprinklerPts = ThSprinklerConnectDataFactory.GetSprinklerConnectData(frame);
                var mainPipe = ThSprinklerConnectDataFactory.GetPipeData(frame, ThSprinklerConnectCommon.Layer_MainPipe);
                var subMainPipe = ThSprinklerConnectDataFactory.GetPipeData(frame, ThSprinklerConnectCommon.Layer_SubMainPipe);

                if (SprinklerPts.Count == 0 || mainPipe.Count == 0 || subMainPipe.Count == 0)
                {
                    return;
                }

                // 提取车位外包框
                var parkingStallService = new ThSprinklerConnectParkingStallService();
                parkingStallService.BlockNameDict = BlockNameDict;
                var parkingStall = parkingStallService.GetParkingStallOBB(acadDatabase.Database, frame);

                // 打印车位外包框
                //parkingStall.ForEach(o =>
                //{
                //    acadDatabase.ModelSpace.Add(o);
                //});

                var dataset = new ThSprinklerConnectDataFactory();
                var geos = dataset.Create(acadDatabase.Database, frame.Vertices()).Container;
                var dataQuery = new ThSprinklerDataQueryService(geos);
                dataQuery.ClassifyData();

                //转回原点
                //var transformer = ThSprinklerConnectUtil.transformToOrig(pts, geos);


                //打散管线
                ThSprinklerPipeService.ThSprinklerPipeToLine(mainPipe, subMainPipe, out var mainLine, out var subMainLine, out var allLines);
                //DrawUtils.ShowGeometry(mainLine, "l0mainline", 22, 30);
                //DrawUtils.ShowGeometry(subMainLine, "l0submainline", 142, 30);
                //DrawUtils.ShowGeometry(allLines, "l0all", 2, 30);


                var sprinklerParameter = new ThSprinklerParameter();
                sprinklerParameter.SprinklerPt = SprinklerPts;
                sprinklerParameter.MainPipe = mainLine;
                sprinklerParameter.SubMainPipe = subMainLine;
                sprinklerParameter.AllPipe = allLines;

                ThSprinklerConnectEngine.SprinklerConnectEngine(sprinklerParameter);

            }
        }
    }
}
