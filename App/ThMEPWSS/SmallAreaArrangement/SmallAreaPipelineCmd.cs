using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Command;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.DrainageSystemDiagram;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Linq;
using System.Collections.Generic;
using THMEPWSS.RoomOrderingService;

namespace ThMEPWSS.SmallAreaArrangement.Cmd
{
    public partial class ThSprinklerConnectNoUICmd
    {
        [CommandMethod("TIANHUACAD", "THSmallRoomPipeline", CommandFlags.Modal)]
        public void SmallAreaArrange()
        {
            var cmd = new SmallAreaPipelineCmd();
            cmd.Execute();
        }

           
    }
    public class SmallAreaPipelineCmd : ThMEPBaseCommand
    {
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();

        public SmallAreaPipelineCmd()
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
                var sprinklerPts = ThSprinklerConnectDataFactory.GetSprinklerConnectData(frame);

                if (sprinklerPts.Count == 0 )
                {
                    return;
                }


                var dataset = new ThSprinklerConnectDataFactory();
                var geos = dataset.Create(acadDatabase.Database, frame.Vertices()).Container;
                var dataQuery = new ThSprinklerDataQueryService(geos);
                dataQuery.ClassifyData();

                var geometry = new List<Polyline>();
                geometry.AddRange(dataQuery.ArchitectureWallList);
                geometry.AddRange(dataQuery.ShearWallList);
                geometry.AddRange(dataQuery.ColumnList);
                geometry.AddRange(dataQuery.RoomList);

                //var smallRoom = dataQuery.RoomList.Where(r => r.Area < 1e8).ToList();
                var bound = 1e8;
                var ptInSmallRoom = new HashSet<Point3d>();

                var orderService = new OrderingService();
                orderService.sprinkler.AddRange(sprinklerPts);
                orderService.PipelineArrange(dataQuery.RoomList, dataQuery.ShearWallList, bound);
                var allLines = orderService.gridLines;
                var allGrid = orderService.orthogonalGrid;
                //List<Polyline> temp1 = new List<Polyline>();
                //List<Polyline> temp2 = new List<Polyline>();
                //allGrid.ForEach(o => o.ForEach(l => temp1.Add(l.Buffer(1))));
                //allLines.ForEach(o => o.ForEach(l => temp2.Add(l.Buffer(1))));
                //var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(ptInSmallRoom.ToList(), out var dtSeg);
                //DrawUtils.ShowGeometry(temp1, "gridBuffered", 6);
                //DrawUtils.ShowGeometry(temp2, "linesBuffered", 1);
                allGrid.ForEach(o => DrawUtils.ShowGeometry(o, "OrthorDT", 1));
                allLines.ForEach(o=>DrawUtils.ShowGeometry(o, "AllDT", 3));
                DrawUtils.ShowGeometry(dataQuery.RoomList, "roomBoundary", 5);
                DrawUtils.ShowGeometry(dataQuery.ShearWallList, "shearwall", 4);
            }
        }

    }
}
