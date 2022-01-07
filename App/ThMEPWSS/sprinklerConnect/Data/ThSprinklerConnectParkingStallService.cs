using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.FlushPoint.Data;
using ThMEPWSS.SprinklerConnect.Service;

namespace ThMEPWSS.SprinklerConnect.Data
{
    public class ThSprinklerConnectParkingStallService
    {
        public DBObjectCollection ParkingStalls { get; set; } = new DBObjectCollection();
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();

        public ThSprinklerConnectParkingStallService()
        {
            //
        }

        public List<Polyline> GetParkingStallOBB(Polyline pline)
        {
            var parkingStalls = new List<Polyline>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(ParkingStalls);
            var filter = spatialIndex.SelectCrossingPolygon(pline);
            AssortParkingStall(filter).ForEach(list =>
            {
                // 单排车位
                var singleRow = list.Select(o => Extend(o, 700))
                    .ToCollection()
                    .Outline()
                    .OfType<Polyline>()
                    .Select(o => o.OBB())
                    .Select(o => Extend(o, -700))
                    .Where(o => o.Area > 2.8e7)
                    .ToList();
                // 双排车位

                singleRow = singleRow.OrderByDescending(o => o.Area).ToList();
                if (singleRow.Count() >= 1)
                {
                    var singleRowSort = new List<List<Polyline>>();
                    singleRowSort.Add(new List<Polyline> { singleRow[0] });
                    for (int i = 1; i < singleRow.Count(); i++)
                    {
                        if (singleRow[i].Area > singleRowSort[singleRowSort.Count - 1][0].Area * 0.8)
                        {
                            singleRowSort[singleRowSort.Count - 1].Add(singleRow[i]);
                        }
                        else
                        {
                            singleRowSort.Add(new List<Polyline> { singleRow[i] });
                        }
                    }

                    singleRowSort.ForEach(row =>
                    {
                        var doubleRow = row.Select(o => o.Buffer(200).OfType<Polyline>().OrderByDescending(poly => poly.Area).First())
                            .ToCollection()
                            .Outline()
                            .OfType<Polyline>()
                            .Select(o => o.OBB().Buffer(-200).OfType<Polyline>().OrderByDescending(poly => poly.Area).First())
                            .ToList();
                        parkingStalls.AddRange(doubleRow);
                    });
                }

            });
            return Normalize(parkingStalls);
        }

        public void ParkingStallExtractor(Database database, Polyline pline)
        {
            //提取停车位
            var parkingStallBlkNames = new List<string>();
            parkingStallBlkNames.AddRange(QueryBlkNames("机械车位"));
            parkingStallBlkNames.AddRange(QueryBlkNames("非机械车位"));
            if (parkingStallBlkNames.Count == 0)
            {
                return;
            }

            var parkingStallExtractor = new ThParkingStallExtractor()
            {
                BlockNames = parkingStallBlkNames,
                LayerNames = new List<string>(),
            };
            parkingStallExtractor.Extract(database, pline.Vertices());

            if (parkingStallExtractor.ParkingStalls.Count > 0)
            {
                var parkingStallsTemp = parkingStallExtractor.ParkingStalls.OfType<Polyline>().ToList();
                var transformer = ThSprinklerTransformer.GetTransformer(parkingStallsTemp[0].Vertices());
                parkingStallsTemp.ForEach(o =>
                {
                    transformer.Transform(o);
                });
                ParkingStalls = parkingStallsTemp
                    .Select(o => o.Buffer(-1).OfType<Polyline>().OrderByDescending(poly => poly.Area).First())
                    .ToCollection();
                transformer.Reset(ParkingStalls);
            }
        }

        private List<List<Polyline>> AssortParkingStall(DBObjectCollection parkingStalls)
        {
            // 将车位根据车道方向进行分类
            var angleList = new List<double>();
            var centerPts = new List<Point3d>();
            var parkingStallSort = new List<List<Polyline>>();
            var stallList = parkingStalls.OfType<Polyline>().ToList();
            for (int i = 0; i < stallList.Count; i++)
            {
                var lines = new DBObjectCollection();
                stallList[i].Explode(lines);
                var list = lines.OfType<Line>().OrderByDescending(line => line.Length).ToList();
                var centerPt = new Point3d((list[0].StartPoint.X + list[0].EndPoint.X + list[1].StartPoint.X + list[1].EndPoint.X) / 4,
                                           (list[0].StartPoint.Y + list[0].EndPoint.Y + list[1].StartPoint.Y + list[1].EndPoint.Y) / 4, 0);
                if (stallList[i].Area > 4e7)
                {
                    int m = 0;
                    for (; m < angleList.Count; m++)
                    {
                        if (Math.Abs(angleList[m] - list[0].Angle) < 3.0 / 180.0 * Math.PI
                         || (Math.Abs(angleList[m] - list[0].Angle) > 177.0 / 180.0 * Math.PI
                         && Math.Abs(angleList[m] - list[0].Angle) < 183.0 / 180.0 * Math.PI)
                         || Math.Abs(angleList[m] - list[0].Angle) > 357.0 / 180.0 * Math.PI)
                        {
                            parkingStallSort[m].Add(stallList[i]);
                            break;
                        }
                    }
                    if (m == angleList.Count)
                    {
                        angleList.Add(list[0].Angle);
                        centerPts.Add(centerPt);
                        parkingStallSort.Add(new List<Polyline> { stallList[i] });
                    }
                }
                else
                {
                    int m = 0;
                    for (; m < angleList.Count; m++)
                    {
                        if (Math.Abs(angleList[m] - list[2].Angle) < 3.0 / 180.0 * Math.PI
                        || (Math.Abs(angleList[m] - list[2].Angle) > 177.0 / 180.0 * Math.PI
                         && Math.Abs(angleList[m] - list[2].Angle) < 183.0 / 180.0 * Math.PI)
                         || Math.Abs(angleList[m] - list[2].Angle) > 357.0 / 180.0 * Math.PI)
                        {
                            parkingStallSort[m].Add(stallList[i]);
                            break;
                        }
                    }
                    if (m == angleList.Count)
                    {
                        angleList.Add(list[2].Angle);
                        centerPts.Add(centerPt);
                        parkingStallSort.Add(new List<Polyline> { stallList[i] });
                    }
                }
            }
            return parkingStallSort;
        }

        private List<string> QueryBlkNames(string category)
        {
            if (BlockNameDict.ContainsKey(category))
            {
                return BlockNameDict[category].Distinct().ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        private List<Polyline> Normalize(List<Polyline> parkingStalls)
        {
            var stallsTidal = new List<Polyline>();
            parkingStalls.ForEach(o =>
            {
                var firstDist = o.GetPoint3dAt(1).DistanceTo(o.StartPoint);
                var secondDist = o.GetPoint3dAt(1).DistanceTo(o.GetPoint3dAt(2));
                if (firstDist > secondDist
                    && !(o.Area > 7.5e7 && o.Area < 8.5e7 && firstDist / secondDist > 1.45 && firstDist / secondDist < 1.6)
                    || (firstDist < secondDist 
                        && o.Area > 7.5e7 && o.Area < 8.5e7 && firstDist / secondDist > 1.45 && firstDist / secondDist < 1.6))
                {
                    // 取长边为车道方向
                    stallsTidal.Add(o.Clone() as Polyline);
                }
                else
                {
                    var pts = o.Vertices();
                    var newPts = new Point3dCollection
                    {
                        pts[0],
                        pts[3],
                        pts[2],
                        pts[1],
                    };
                    var pline = new Polyline
                    {
                        Closed = true
                    };
                    pline.CreatePolyline(newPts);
                    stallsTidal.Add(pline);
                }
            });
            return stallsTidal;
        }

        private Polyline Extend(Polyline srcPoly, double value)
        {
            var pts = srcPoly.Vertices();
            if (pts.Count == 5)
            {
                var targetPoly = new Polyline
                {
                    Closed = true
                };
                // 判断延伸方向
                var oneSide = pts[0].DistanceTo(pts[1]);
                var anthorSide = pts[2].DistanceTo(pts[1]);
                if (Math.Abs(oneSide - 5500.0) < Math.Abs(anthorSide - 5500.0))
                {
                    var unitVector = (pts[1] - pts[2]).GetNormal();
                    var newPts = new Point3dCollection
                    {
                        pts[0] + unitVector * value,
                        pts[1] + unitVector * value,
                        pts[2] - unitVector * value,
                        pts[3] - unitVector * value,
                    };
                    targetPoly.CreatePolyline(newPts);
                }
                else
                {
                    var unitVector = (pts[0] - pts[1]).GetNormal();
                    var newPts = new Point3dCollection
                    {
                        pts[0] + unitVector * value,
                        pts[1] - unitVector * value,
                        pts[2] - unitVector * value,
                        pts[3] + unitVector * value,
                    };
                    targetPoly.CreatePolyline(newPts);
                }

                return targetPoly;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
