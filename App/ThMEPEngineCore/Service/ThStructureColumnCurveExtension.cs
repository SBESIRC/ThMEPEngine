using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThStructureColumnCurveExtension : ThStructureDbExtension,IDisposable
    {
        public List<Curve> ColumnCurves { get; set; }
        public ThStructureColumnCurveExtension(Database db):base(db)
        {
            LayerFilter = ThStructureColumnLayerManager.CurveXrefLayers(db);
            ColumnCurves = new List<Curve>();
        }
        public void Dispose()
        {
            foreach (var curve in ColumnCurves)
            {
                curve.Dispose();
            }
            ColumnCurves.Clear();
        }
        public override void BuildElementCurves()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                foreach(var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkRef)
                    {
                        if (blkRef.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        BlockTableRecord btr = acadDatabase.Element<BlockTableRecord>(blkRef.BlockTableRecord);
                        var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        ColumnCurves.AddRange(BuildElementCurves(blkRef, mcs2wcs));
                    }
                }
                FilterCirlces();
                FiltePolyline2ds();
                FiltePolyline3ds();
            }
        }
        private void FilterCirlces()
        {
            var circles = ColumnCurves.Where(o => o is Circle).ToList();
            List<Polyline> polylines = circles.Select(o => CreatePolyline(o as Circle)).ToList();
            ColumnCurves = ColumnCurves.Where(o => !(o is Circle)).ToList();
            ColumnCurves.AddRange(polylines);
            circles.ForEach(o => o.Dispose());
        }
        private void FiltePolyline2ds()
        {
            var poyline2ds = ColumnCurves.Where(o => o is Polyline2d).ToList();
            List<Polyline> polylines = poyline2ds.Select(o => CreatePolyline(o as Polyline2d)).ToList();
            ColumnCurves = ColumnCurves.Where(o => !(o is Polyline2d)).ToList();
            ColumnCurves.AddRange(polylines);
            poyline2ds.ForEach(o => o.Dispose());
        }
        private void FiltePolyline3ds()
        {
            var poyline3ds = ColumnCurves.Where(o => o is Polyline3d).ToList();
            List<Polyline> polylines = poyline3ds.Select(o => CreatePolyline(o as Polyline3d)).ToList();
            ColumnCurves = ColumnCurves.Where(o => !(o is Polyline3d)).ToList();
            ColumnCurves.AddRange(polylines);
            poyline3ds.ForEach(o => o.Dispose());
        }
        private Polyline CreatePolyline(Circle circle)
        {             
            Point3d pt1 = circle.Center + new Vector3d(-1.0 * circle.Radius, -1.0 * circle.Radius, 0.0);
            Point3d pt2 = circle.Center + new Vector3d(circle.Radius, -1.0 * circle.Radius, 0.0);
            Point3d pt3 = circle.Center + new Vector3d(circle.Radius, circle.Radius, 0.0);
            Point3d pt4 = circle.Center + new Vector3d(-1.0 * circle.Radius, circle.Radius, 0.0);
            Point3dCollection pts = new Point3dCollection()
            {
                pt1,pt2,pt3,pt4
            };
            return pts.CreatePolyline();
        }
        private Polyline CreatePolyline(Polyline2d polyline2d)
        {
            Point3dCollection pts = new Point3dCollection();
            foreach (Point3d pt in polyline2d.GetPoints())
            {
                pts.Add(pt);
            }
            return pts.CreatePolyline();
        }
        private Polyline CreatePolyline(Polyline3d polyline3d)
        {
            Point3dCollection pts = new Point3dCollection();
            foreach (Point3d pt in polyline3d.GetPoints())
            {
                pts.Add(pt);
            }
            return pts.CreatePolyline();
        }
        private IEnumerable<Curve> BuildElementCurves(BlockReference blockReference, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                List<Curve> curves = new List<Curve>();
                if (IsBuildElementBlockReference(blockReference))
                {
                    var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                    if (IsBuildElementBlock(blockTableRecord))
                    {
                        foreach (var objId in blockTableRecord)
                        {
                            var dbObj = acadDatabase.Element<Entity>(objId);
                            if (dbObj is BlockReference blockObj)
                            {
                                if (blockObj.BlockTableRecord.IsNull)
                                {
                                    continue;
                                }
                                if (blockObj.IsBuildElementBlockReference())
                                {
                                    var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                    curves.AddRange(BuildElementCurves(blockObj, mcs2wcs));
                                }
                            }
                            else if (dbObj is Curve curve)
                            {
                                if (CheckLayerValid(curve))
                                {
                                    curves.Add(curve.GetTransformedCopy(matrix) as Curve);
                                }
                            }
                        }

                        var xclip = blockReference.XClipInfo();
                        if (xclip.IsValid)
                        {
                            xclip.TransformBy(matrix);
                            return curves.Where(o => xclip.Contains(o));
                        }
                    }
                }
                return curves;
            }
        }
        public override void BuildElementTexts()
        {
            throw new NotImplementedException();
        }
    }
}
