using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.ChargerDistribution.Service
{
    public class ThParkingStallSplitter
    {
        private readonly double SmallSize = 4300 * 2200;
        private readonly double MiddleSize = 5300 * 2400;
        private readonly double BigSize = 6000 * 3600;

        public void Split(List<Polyline> stalls)
        {
            var adds = new List<Polyline>();
            var removes = new List<Polyline>();
            stalls.ForEach(stall =>
            {
                var dictionary = Calculate(stall);
                var pair = dictionary.First();
                switch (pair.Key)
                {
                    case "Type1":
                    case "Type2":
                    case "Type3":
                        break;
                    case "Type4":
                        TwoPartsSplit(stall, 4300, adds, removes);
                        break;
                    case "Type5":
                        TwoPartsSplit(stall, 5300, adds, removes);
                        break;
                    case "Type6":
                        TwoPartsSplit(stall, 6000, adds, removes);
                        break;
                    case "Type7":
                        ThreePartsSplit(stall, 4300, adds, removes);
                        break;
                    case "Type8":
                        ThreePartsSplit(stall, 5300, adds, removes);
                        break;
                    case "Type9":
                        ThreePartsSplit(stall, 6000, adds, removes);
                        break;
                    default:
                        break;
                }
            });

            stalls.RemoveAll(o => removes.Contains(o));
            stalls.AddRange(adds);
        }

        private Dictionary<string, double> Calculate(Polyline pline)
        {
            var dictionary = new Dictionary<string, double>
            {
                { "Type1", Math.Abs(pline.Area - SmallSize) },
                { "Type2", Math.Abs(pline.Area - MiddleSize) },
                { "Type3", Math.Abs(pline.Area - BigSize) },
                { "Type4", Math.Abs(pline.Area - 2 * SmallSize) },
                { "Type5", Math.Abs(pline.Area - 2 * MiddleSize) },
                { "Type6", Math.Abs(pline.Area - 2 * BigSize) },
                { "Type7", Math.Abs(pline.Area - 3 * SmallSize) },
                { "Type8", Math.Abs(pline.Area - 3 * MiddleSize) },
                { "Type9", Math.Abs(pline.Area - 3 * BigSize) },
            };
            dictionary = dictionary.OrderBy(o => o.Value).ToDictionary(o => o.Key, o => o.Value);
            return dictionary;
        }

        private void TwoPartsSplit(Polyline stall, double length, List<Polyline> adds, List<Polyline> removes)
        {
            var vertices = stall.Vertices();
            var firstLength = vertices[0].DistanceTo(vertices[1]);
            var secondLength = vertices[1].DistanceTo(vertices[2]);
            // true表示0->1为长边
            var tag = firstLength > secondLength;
            var maxLength = tag ? firstLength : secondLength;
            var diff1 = Math.Abs(maxLength - 2 * length);
            var diff2 = Math.Abs(maxLength - length);
            // 两车位短边相接
            if (diff1 < diff2)
            {
                if (tag)
                {
                    var center1 = GetCenter(vertices[0], vertices[1]);
                    var center2 = GetCenter(vertices[2], vertices[3]);
                    var points1 = new Point3dCollection
                    {
                        vertices[0],
                        center1,
                        center2,
                        vertices[3],
                    };
                    var points2 = new Point3dCollection
                    {
                        center1,
                        vertices[1],
                        vertices[2],
                        center2,
                    };
                    removes.Add(stall);
                    adds.Add(points1.CreatePolyline(true));
                    adds.Add(points2.CreatePolyline(true));
                }
                else
                {
                    var center1 = GetCenter(vertices[1], vertices[2]);
                    var center2 = GetCenter(vertices[3], vertices[4]);
                    var points1 = new Point3dCollection
                    {
                        vertices[0],
                        vertices[1],
                        center1,
                        center2,
                    };
                    var points2 = new Point3dCollection
                    {
                        center2,
                        center1,
                        vertices[2],
                        vertices[3],
                    };
                    removes.Add(stall);
                    adds.Add(points1.CreatePolyline(true));
                    adds.Add(points2.CreatePolyline(true));
                }
            }
            // 两车位长边相接
            else
            {
                var diff3 = Math.Abs(firstLength - length);
                var diff4 = Math.Abs(secondLength - length);
                // 1->2为合并边
                if (diff3 < diff4)
                {
                    var center1 = GetCenter(vertices[1], vertices[2]);
                    var center2 = GetCenter(vertices[3], vertices[4]);
                    var points1 = new Point3dCollection
                    {
                        vertices[0],
                        vertices[1],
                        center1,
                        center2,
                    };
                    var points2 = new Point3dCollection
                    {
                        center2,
                        center1,
                        vertices[2],
                        vertices[3],
                    };
                    removes.Add(stall);
                    adds.Add(points1.CreatePolyline(true));
                    adds.Add(points2.CreatePolyline(true));
                }
                else
                {
                    var center1 = GetCenter(vertices[0], vertices[1]);
                    var center2 = GetCenter(vertices[2], vertices[3]);
                    var points1 = new Point3dCollection
                    {
                        vertices[0],
                        center1,
                        center2,
                        vertices[3],
                    };
                    var points2 = new Point3dCollection
                    {
                        center1,
                        vertices[1],
                        vertices[2],
                        center2,
                    };
                    removes.Add(stall);
                    adds.Add(points1.CreatePolyline(true));
                    adds.Add(points2.CreatePolyline(true));
                }
            }
        }

        private void ThreePartsSplit(Polyline stall, double length, List<Polyline> adds, List<Polyline> removes)
        {
            var vertices = stall.Vertices();
            var firstLength = vertices[0].DistanceTo(vertices[1]);
            var secondLength = vertices[1].DistanceTo(vertices[2]);
            // true表示0->1为长边
            var tag = firstLength > secondLength;
            var maxLength = tag ? firstLength : secondLength;
            var diff1 = Math.Abs(maxLength - 3 * length);
            // 暂不考虑三车位短边相接，三车位长边相接
            if (diff1 > 3 * 200)
            {
                var diff3 = Math.Abs(firstLength - length);
                var diff4 = Math.Abs(secondLength - length);
                // 1->2为合并边
                if (diff3 < diff4)
                {
                    var center1 = GetTrisection(vertices[1], vertices[2]);
                    var center2 = GetTrisection(vertices[3], vertices[4]);
                    var points1 = new Point3dCollection
                    {
                        vertices[0],
                        vertices[1],
                        center1[0],
                        center2[1],
                    };
                    var points2 = new Point3dCollection
                    {
                        center2[1],
                        center1[0],
                        center1[1],
                        center2[0],
                    };
                    var points3 = new Point3dCollection
                    {
                        center2[0],
                        center1[1],
                        vertices[2],
                        vertices[3],
                    };
                    removes.Add(stall);
                    adds.Add(points1.CreatePolyline(true));
                    adds.Add(points2.CreatePolyline(true));
                    adds.Add(points3.CreatePolyline(true));
                }
                else
                {
                    var center1 = GetTrisection(vertices[0], vertices[1]);
                    var center2 = GetTrisection(vertices[2], vertices[3]);
                    var points1 = new Point3dCollection
                    {
                        vertices[0],
                        center1[0],
                        center2[1],
                        vertices[3],
                    };
                    var points2 = new Point3dCollection
                    {
                        center1[0],
                        center1[1],
                        center2[0],
                        center2[1],
                    };
                    var points3 = new Point3dCollection
                    {
                        center1[1],
                        vertices[1],
                        vertices[2],
                        center2[0],
                    };
                    removes.Add(stall);
                    adds.Add(points1.CreatePolyline(true));
                    adds.Add(points2.CreatePolyline(true));
                    adds.Add(points3.CreatePolyline(true));
                }
            }
        }

        private Point3d GetCenter(Point3d first, Point3d second)
        {
            return new Point3d((first.X + second.X) / 2, (first.Y + second.Y) / 2, (first.Z + second.Z) / 2);
        }

        private List<Point3d> GetTrisection(Point3d first, Point3d second)
        {
            return new List<Point3d>
            {
                GetPoint(first, second, 1),
                GetPoint(first, second, 2),
            };
        }

        private Point3d GetPoint(Point3d first, Point3d second, int i)
        {
            return new Point3d(first.X * i / 3 + second.X * (3 - i) / 3, first.Y * i / 3 + second.Y * (3 - i) / 3, first.Z * i / 3 + second.Z * (3 - i) / 3);
        }
    }
}
