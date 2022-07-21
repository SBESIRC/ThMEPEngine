using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.StructPlane.Service
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
                        results.Add(CreateCornerLine(p));
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
                if(!pts.Contains(pt))
                {
                    pts.Add(pt);
                }
            }
            return pts;
        }

        private Tuple<Point3d,Point3d> GetMaxLengthCorner(Point3dCollection pts)
        {
            Point3d first = Point3d.Origin,second = Point3d.Origin;
            for (int i=0;i< pts.Count-1;i++)
            {
                for (int j = i+1; j < pts.Count; j++)
                {
                    if(pts[i].DistanceTo(pts[j])> first.DistanceTo(second))
                    {
                        first = pts[i];
                        second = pts[j];
                    }
                    else if(Math.Abs(pts[i].DistanceTo(pts[j]) - first.DistanceTo(second))<=1e-4)
                    {
                        var ang1 = first.GetVectorTo(second).GetAngleTo(Vector3d.XAxis);
                        var ang2 = pts[i].GetVectorTo(pts[j]).GetAngleTo(Vector3d.XAxis);
                        if(ang2< ang1)
                        {
                            first = pts[i];
                            second = pts[j];
                        }
                    }
                }
            }
            return Tuple.Create(first, second); 
        }
    }
}
