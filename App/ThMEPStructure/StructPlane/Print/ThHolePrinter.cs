using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
{
    internal class ThHolePrinter
    {
        private HatchPrintConfig HatchConfig { get; set; }
        private PrintConfig OutlineConfig { get; set; }
        public ThHolePrinter(HatchPrintConfig hatchConfig, PrintConfig outlineConfig)
        {
            HatchConfig = hatchConfig;
            OutlineConfig = outlineConfig;
        }
        public ObjectIdCollection Print(Database db, Polyline polygon)
        {
            var results = new ObjectIdCollection();
            if(polygon.Length<=1.0 || polygon.Area<=1.0 || 
                !ThMEPFrameService.IsClosed(polygon, 1.0))
            {
                return results;
            }            
            var outPolygonId = polygon.Print(db, OutlineConfig);
            results.Add(outPolygonId);
            var newOutPolygon = Handle(polygon);
            if (newOutPolygon.Area < 1.0)
            {
                return results;
            }
            var innerhole = BuildHatchHole(newOutPolygon);
            if (innerhole.Area > 0.0)
            {
                var innerPolygonId = innerhole.Print(db, OutlineConfig);
                var objIds = new ObjectIdCollection { innerPolygonId };
                var hatchId = objIds.Print(db, HatchConfig);
                results.Add(innerPolygonId);
                results.Add(hatchId);
            }
            return results;
        }
        private Polyline Handle(Polyline polygon)
        {
            var objs = new DBObjectCollection() { polygon };
            var simplifier = new ThPolygonalElementSimplifier()
            {
                TESSELLATEARCLENGTH = 10.0,
            };
            objs = simplifier.Tessellate(objs); //去掉弧
            objs = simplifier.MakeValid(objs);  //去除自交
            objs = simplifier.Simplify(objs);   //合并近似平行的线
            objs = simplifier.Normalize(objs);  //去除狭长线
            objs = simplifier.Filter(objs); // 过滤面积很小的Polyline
            if (objs.Count>0)
            {
                return objs.OfType<Polyline>().OrderByDescending(p => p.Area).First();
            }
            else
            {
                return new Polyline();
            }
        }
        private Polyline BuildHatchHole(Polyline polygon)
        {
            // 传入的polygon顶点不能有重复,只允许首尾有重复点,不支持弧
            var pairs = new List<Tuple<int, int, int>>();
            var count = polygon.NumberOfVertices;
            if (polygon.StartPoint.DistanceTo(polygon.EndPoint)<=1.0)
            {
                // 首尾点相同
                for (int i = 0; i < count-1; i++)
                {
                    int aIndex = i, bIndex = (i + 1) % count, cIndex = i;
                    cIndex = i == count - 2 ? (i + 3) % count : (i + 2) % count;
                    //if (pairs.Where(o => (o.Item1 == aIndex && o.Item3 == cIndex) ||
                    //(o.Item3 == aIndex && o.Item1 == cIndex)).Any() == false)
                    //{
                    //  暂时取消此过滤直接添加
                    //}
                    pairs.Add(Tuple.Create(aIndex, bIndex, cIndex));
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int aIndex = i, bIndex = (i + 1) % count, cIndex = (i + 2) % count;
                    //if (pairs.Where(o => (o.Item1 == aIndex && o.Item3 == cIndex) ||
                    //(o.Item3 == aIndex && o.Item1 == cIndex)).Any() == false)
                    //{ 
                    //   暂时取消此过滤直接添加
                    //}
                    pairs.Add(Tuple.Create(aIndex, bIndex, cIndex));
                }
            }
            // 过滤无效的边
            pairs = pairs.Where(p =>
            {
                var pt1 = polygon.GetPoint3dAt(p.Item1);
                var pt2 = polygon.GetPoint3dAt(p.Item2);
                var pt3 = polygon.GetPoint3dAt(p.Item3);
                if (pt1.DistanceTo(pt2) <= 1.0 || pt1.DistanceTo(pt2) <= 1.0)
                {
                    return false;
                }
                if (ThGeometryTool.IsCollinearEx(pt1, pt2, pt3))
                {
                    return false;
                }
                return IsContains(polygon, pt1.GetExtentPoint(pt1.GetVectorTo(pt3), 1.0),
                    pt3.GetExtentPoint(pt3.GetVectorTo(pt1), 1.0));
            }).ToList();

            if(pairs.Count>0)
            {
                var result = pairs
                .OrderByDescending(p => Math.Round(polygon.GetPoint3dAt(p.Item1).DistanceTo(polygon.GetPoint3dAt(p.Item3))))
                .ThenBy(p => polygon.GetPoint3dAt(p.Item2).X)
                .ThenByDescending(p => polygon.GetPoint3dAt(p.Item2).Y)
                .First();

                // 计算折点
                var pt1 = polygon.GetPoint3dAt(result.Item1);
                var pt2 = polygon.GetPoint3dAt(result.Item2);
                var pt3 = polygon.GetPoint3dAt(result.Item3);
                var length1 = pt1.DistanceTo(pt2);
                var length2 = pt2.DistanceTo(pt3);
                var distance = Math.Min(length1, length2)*0.15;
                var projectionPt = pt2.GetProjectPtOnLine(pt1, pt3);
                var cornerPt = pt2.GetExtentPoint(pt2.GetVectorTo(projectionPt), distance);

                var pts = new Point3dCollection() { 
                    polygon.GetPoint3dAt(result.Item1),
                    polygon.GetPoint3dAt(result.Item2),
                    polygon.GetPoint3dAt(result.Item3),cornerPt};
                return pts.CreatePolyline();
            }
            else
            {
                return new Polyline();
            }
        }
        private bool IsContains(Polyline polygon,Point3d lineSp,Point3d lineEp)
        {
            var line = new Line(lineSp, lineEp);
            bool isIn = polygon.EntityContains(line);
            line.Dispose();
            return isIn;
        }
        public static HatchPrintConfig GetHoleHatchConfig()
        {
            return new HatchPrintConfig
            {
                LayerName = ThPrintLayerManager.HoleHatchLayerName,
                PatternName = "SOLID",
                PatternScale = 1.0,
                PatternType = HatchPatternType.PreDefined,
            };
        }
        public static PrintConfig GetHoleConfig()
        {
            return new PrintConfig
            {
                LayerName = ThPrintLayerManager.HoleLayerName,
                LineType = "ByLayer",
                LineWeight = LineWeight.ByLayer,
            };
        }
    }
}
