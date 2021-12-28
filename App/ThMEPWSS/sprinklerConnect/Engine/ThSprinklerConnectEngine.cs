using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPEngineCore;

namespace ThMEPWSS.SprinklerConnect.Engine
{
    public class ThSprinklerConnectEngine
    {
        public ThSprinklerConnectEngine()
        {

        }

        // 喷头连管
        public void SprinklerConnectEngine(ThSprinklerParameter sprinklerParameter, List<Polyline> geometryWithoutColumn,
            List<Polyline> doubleStall, List<Polyline> smallRooms, List<Polyline> obstacle, List<Polyline> column, bool isVertical = true)
        {
            var netList = ThSprinklerPtNetworkEngine.GetSprinklerPtNetwork(sprinklerParameter, geometryWithoutColumn, out double dtTol);
            var geometry = geometryWithoutColumn;
            geometry.AddRange(column);
            var service = new ThSprinklerConnectService(sprinklerParameter, geometry, dtTol);

            var rowConnection = new List<ThSprinklerRowConnect>();
            var pipeScatters = new List<Point3d>();

            if (doubleStall.Count > 0)
            {
                service.LaneLine = GetLaneLine(doubleStall);
            }
            else
            {
                service.LaneLine = new List<Line>();
            }

            // < netList.Count
            for (int i = 0; i < netList.Count; i++)
            {
                // < netList[i].ptsGraph.Count
                for (int j = 0; j < netList[i].ptsGraph.Count; j++)
                {
                    rowConnection.AddRange(service.GraphPtsConnect(netList[i], j, pipeScatters, isVertical));
                }
            }

            // 处理环
            service.HandleLoopRow(rowConnection);
            // 散点处理
            service.HandleScatter(rowConnection, pipeScatters);
            // 列分割
            rowConnection = service.RowSeparation(rowConnection, isVertical);

            // 散点直接连管
            service.ConnScatterToPipe(rowConnection, pipeScatters);

            // < netList.Count
            for (int i = 0; i < netList.Count; i++)
            {
                // < netList[i].ptsGraph.Count
                for (int j = 0; j < netList[i].ptsGraph.Count; j++)
                {
                    service.HandleConsequentScatter(netList[i], j, rowConnection, pipeScatters, smallRooms, obstacle);
                }
            }

            var connTolerance = 300.0;
            service.SprinklerConnect(rowConnection, geometry, obstacle, connTolerance);

            service.HandleSingleScatter(rowConnection, pipeScatters, connTolerance);

            for (int i = 0; i < netList.Count; i++)
            {
                // < netList[i].ptsGraph.Count
                for (int j = 0; j < netList[i].ptsGraph.Count; j++)
                {
                    service.HandleSprinklerInSmallRoom(netList[i], j, rowConnection, smallRooms, pipeScatters, obstacle);
                }
            }

            var results = new List<Line>();
            rowConnection.ForEach(row =>
            {
                results.AddRange(row.ConnectLines);
            });

            // 最终散点处理
            results = results.Where(o => o.Length > 1.0).ToList();
            Present(results);
        }

        private static List<Line> GetLaneLine(List<Polyline> doubleStall)
        {
            var laneLine = new List<Line>();
            doubleStall.ForEach(o =>
            {
                var pts = o.Vertices();
                laneLine.Add(new Line(pts[0], pts[1]));
                laneLine.Add(new Line(pts[2], pts[3]));
            });
            return laneLine;
        }

        private static void Present(List<Line> results)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAILayer(ThWSSCommon.Sprinkler_Connect_Pipe, 2);
                results.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.LayerId = layerId;
                });
            }
        }
    }
}
