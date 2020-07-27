using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.BoundaryRepresentation;

namespace ThCADCore
{
    /// <summary>
    /// 轮廓
    /// </summary>
    /// http://drive-cad-with-code.blogspot.com/2016/11/getting-outline-of-overlapped-entities.html
    public class OutLiner
    {
        private Document _dwg;

        public OutLiner(Document dwg)
        {
            _dwg = dwg;
        }

        public void DrawOutline(IEnumerable<ObjectId> entIds)
        {
            using (var polyline = GetOutline(entIds))
            {
                using (var tran = _dwg.TransactionManager.StartTransaction())
                {
                    var space = (BlockTableRecord)tran.GetObject(
                        _dwg.Database.CurrentSpaceId, OpenMode.ForWrite);
                    space.AppendEntity(polyline as Entity);
                    tran.AddNewlyCreatedDBObject(polyline as Entity, true);
                    tran.Commit();
                }
            }
        }

        public Entity GetOutline(IEnumerable<ObjectId> entIds)
        {
            var regions = new List<Region>();

            using (var tran = _dwg.TransactionManager.StartTransaction())
            {
                foreach (var entId in entIds)
                {
                    var poly = tran.GetObject(entId, OpenMode.ForRead) as Polyline;
                    if (poly != null)
                    {
                        var rgs = GetRegionFromPolyline(poly);
                        regions.AddRange(rgs);
                    }

                }

                tran.Commit();
            }

            using (var region = MergeRegions(regions))
            {
                if (region != null)
                {
                    var brep = new Brep(region);
                    var points = new List<Point2d>();
                    var faceCount = brep.Faces.Count();
                    var face = brep.Faces.First();
                    foreach (var loop in face.Loops)
                    {
                        if (loop.LoopType == LoopType.LoopExterior)
                        {
                            foreach (var vertex in loop.Vertices)
                            {
                                points.Add(new Point2d(vertex.Point.X, vertex.Point.Y));
                            }
                            break;
                        }
                    }

                    return CreatePolyline(points);
                }
                else
                {
                    return null;
                }
            }
        }

        #region private methods

        private List<Region> GetRegionFromPolyline(Polyline poly)
        {
            var regions = new List<Region>();

            var sourceCol = new DBObjectCollection();
            var dbObj = poly.Clone() as Polyline;
            dbObj.Closed = true;
            sourceCol.Add(dbObj);

            var dbObjs = Region.CreateFromCurves(sourceCol);
            foreach (var obj in dbObjs)
            {
                if (obj is Region) regions.Add(obj as Region);
            }

            return regions;
        }

        private Region MergeRegions(List<Region> regions)
        {
            if (regions.Count == 0) return null;
            if (regions.Count == 1) return regions[0];

            var region = regions[0];
            for (int i = 1; i < regions.Count; i++)
            {
                var rg = regions[i];
                region.BooleanOperation(BooleanOperationType.BoolUnite, rg);
                rg.Dispose();
            }

            return region;
        }

        private Polyline CreatePolyline(List<Point2d> points)
        {
            var poly = new Polyline(points.Count());

            for (int i = 0; i < points.Count; i++)
            {
                poly.AddVertexAt(i, points[i], 0.0, 0.3, 0.3);
            }

            poly.SetDatabaseDefaults(_dwg.Database);
            poly.ColorIndex = 1;

            poly.Closed = true;

            return poly;
        }

        #endregion
    }
}
