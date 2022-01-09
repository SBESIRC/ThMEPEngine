using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;

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

            var spatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
            // < netList.Count
            for (int i = 0; i < netList.Count; i++)
            {
                // < netList[i].PtsGraph.Count
                for (int j = 0; j < netList[i].PtsGraph.Count; j++)
                {
                    var graphPtsConnect = service.GraphPtsConnect(netList[i], j, pipeScatters, isVertical);
                    var addLines = new DBObjectCollection();
                    for (int k = 0; k < graphPtsConnect.Count; k++)
                    {
                        if (graphPtsConnect[k].Base.Length > 15.0)
                        {
                            var frame = graphPtsConnect[k].Base.ExtendLine(-10.0).Buffer(1.0);
                            var filter = spatialIndex.SelectCrossingPolygon(frame);
                            if (filter.Count == 0)
                            {
                                rowConnection.Add(graphPtsConnect[k]);
                                addLines.Add(graphPtsConnect[k].Base);
                            }
                            else if (filter.Count == 1)
                            {
                                var filterLine = filter.OfType<Line>().First();
                                var filterRow = rowConnection.Where(row => row.Base.StartPoint == filterLine.StartPoint
                                    && row.Base.EndPoint == filterLine.EndPoint).FirstOrDefault();
                                if (filterRow != null)
                                {
                                    if (graphPtsConnect[k].Count > filterRow.Count)
                                    {
                                        rowConnection.Add(graphPtsConnect[k]);
                                        addLines.Add(graphPtsConnect[k].Base);

                                        filterRow.OrderDict.Values.ForEach(list =>
                                        {
                                            pipeScatters.Remove(list[0]);
                                            service.SprinklerSearched.Remove(list[0]);
                                        });
                                        rowConnection.Remove(filterRow);
                                        spatialIndex.Update(new DBObjectCollection(), new DBObjectCollection { filterLine });
                                    }
                                    else
                                    {
                                        graphPtsConnect[k].OrderDict.Values.ForEach(list =>
                                        {
                                            pipeScatters.Remove(list[0]);
                                            service.SprinklerSearched.Remove(list[0]);
                                        });
                                    }
                                }
                                else
                                {
                                    rowConnection.Add(graphPtsConnect[k]);
                                    addLines.Add(graphPtsConnect[k].Base);
                                }
                            }
                            else
                            {
                                graphPtsConnect[k].OrderDict.Values.ForEach(list =>
                                {
                                    pipeScatters.Remove(list[0]);
                                    service.SprinklerSearched.Remove(list[0]);
                                });
                            }
                        }
                    }

                    spatialIndex.Update(addLines, new DBObjectCollection());
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
