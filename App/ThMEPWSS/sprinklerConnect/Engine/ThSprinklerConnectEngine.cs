using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;

namespace ThMEPWSS.SprinklerConnect.Engine
{
    public class ThSprinklerConnectEngine
    {
        public ThSprinklerConnectEngine()
        {

        }

        // 喷头连管
        public List<Line> SprinklerConnectEngine(ThSprinklerParameter sprinklerParameter, List<Polyline> geometryWithoutColumn,
            List<Polyline> doubleStall, List<Polyline> smallRooms, List<Polyline> obstacle, List<Polyline> column, bool isVertical = true)
        {
            if (sprinklerParameter.SprinklerPt.Count <= 1)
            {
                return new List<Line>();
            }

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
                // < netList[i].PtsGraph.Count
                for (int j = 0; j < netList[i].PtsGraph.Count; j++)
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
                for (int j = 0; j < netList[i].PtsGraph.Count; j++)
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
                for (int j = 0; j < netList[i].PtsGraph.Count; j++)
                {
                    service.HandleSprinklerInSmallRoom(netList[i], j, rowConnection, smallRooms, pipeScatters, obstacle);
                }
            }

            var results = new List<Line>();
            rowConnection.ForEach(row =>
            {
                results.AddRange(row.ConnectLines);
            });
            results = results.Where(o => o.Length > 1.0).ToList();
            service.BreakMainLine(results);

            // 最终散点处理
            return results;
        }

        private List<Line> GetLaneLine(List<Polyline> doubleStall)
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
    }
}
