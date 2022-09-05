using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using System.Collections.Generic;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThBuildStairSlabLineService
    {
        public DBObjectCollection Build(DBObjectCollection texts,DBObjectCollection slabs)
        {
            var results = new DBObjectCollection();
            if(texts.Count==0 || slabs.Count==0)
            {
                return results;
            }
            var spatialIndex = new ThCADCoreNTSSpatialIndex(texts);
            slabs.OfType<Polyline>().Where(p=>p.Area>1.0).ForEach(p =>
            {
                if(IsRectangle(p))
                {
                    var marks = spatialIndex.SelectWindowPolygon(p);
                    if (marks.Count == 1)
                    {
                        var corner = CreateCornerLine(p);
                        if(corner.Length>1.0)
                        {
                            results.Add(corner);
                        }
                    }
                }
            });
            //slabs.OfType<MPolygon>().ForEach(m =>
            //{
            //    var marks = spatialIndex.SelectWindowPolygon(m);
            //    if (marks.Count == 1)
            //    {
            //        results.Add(CreateCornerLine(m));
            //    }
            //});
            return results;
        }

        private bool IsRectangle(Polyline frame)
        {
            return frame.IsRectangle();
        }

        private Line CreateCornerLine(Polyline polygon)
        {
            var pts = GetPolylinePts(polygon);
            var pair = GetMaxLengthCorner(pts);
            return new Line(pair.Item1, pair.Item2);
        }

        private Line CreateCornerLine(MPolygon polygon)
        {
            var shell = polygon.Shell();            
            var corner =  CreateCornerLine(shell);
            shell.Dispose();
            return corner;
        }

        private Point3dCollection GetPolylinePts(Polyline polyline)
        {
            var pts = new Point3dCollection();
            for(int i=0;i< polyline.NumberOfVertices;i++)
            {
                var pt = polyline.GetPoint3dAt(i);
                if(!pts.IsContains(pt,1.0))
                {
                    pts.Add(pt);
                }
            }
            return pts;
        }

        private Tuple<Point3d,Point3d> GetMaxLengthCorner(Point3dCollection pts)
        {
            var tourismQuadrant = new List<Tuple<Point3d, Point3d>>(); // 一、三象限
            var concerningQuadrant = new List<Tuple<Point3d, Point3d>>(); // 二、四象限
            for (int i=0;i< pts.Count;i++)
            {
                for (int j = i+2; j < pts.Count-1; j++)
                {
                    var dir = pts[i].GetVectorTo(pts[j]);
                    if (dir.IsParallelToXAix(1.0) || dir.IsParallelToYAix(1.0))
                    {
                        continue;
                    }
                    else if((dir.X>0.0 && dir.Y>0.0) || (dir.X < 0.0 && dir.Y< 0.0))
                    {
                        if (!IsExist(pts[i], pts[j], tourismQuadrant))
                        {
                            tourismQuadrant.Add(Tuple.Create(pts[i], pts[j]));
                        }
                    }
                    else if ((dir.X < 0.0 && dir.Y > 0.0) || (dir.X > 0.0 && dir.Y < 0.0))
                    {
                        if (!IsExist(pts[i], pts[j], concerningQuadrant))
                        {
                            concerningQuadrant.Add(Tuple.Create(pts[i], pts[j]));
                        }
                    }
                }
            }
            // 优先从第一象限中筛选
            if(tourismQuadrant.Count>0)
            {
                return tourismQuadrant.OrderByDescending(o => o.Item1.DistanceTo(o.Item2)).First();
            }
            else if(concerningQuadrant.Count>0)
            {
                return concerningQuadrant.OrderByDescending(o => o.Item1.DistanceTo(o.Item2)).First();
            }
            else
            {
                return Tuple.Create(Point3d.Origin, Point3d.Origin);
            }
        }
        private bool IsExist(Point3d sp,Point3d ep,List<Tuple<Point3d, Point3d>> ptPairs)
        {
            return ptPairs.Where(o => ThGeometryTool.IsEqual(sp, ep, o.Item1, o.Item2)).Any();
        }
    }
}
