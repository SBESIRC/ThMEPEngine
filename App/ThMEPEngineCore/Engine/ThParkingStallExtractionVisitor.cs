using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Service;
using Linq2Acad;

namespace ThMEPEngineCore.Engine
{
    public class ThParkingStallExtractionVisitor : ThParkingStallVisitorBase
    {
        public ThParkingStallExtractionVisitor()
        {
            CheckQualifiedLayer = CheckLayerIsValid;
            CheckQualifiedBlockName = CheckBlockNameIsValid;
        }
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, Matrix3d.Identity);
            }
        }
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, matrix);
            }
        }
        public override void DoXClip(List<ThRawIfcSpatialElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }
        public override bool IsSpatialElement(Entity entity)
        {
            return CheckQualifiedBlockName(entity);
        }
        public override bool CheckLayerValid(Entity curve)
        {
            return CheckQualifiedLayer(curve);
        }
        private void HandleBlockReference(List<ThRawIfcSpatialElementData> elements, BlockReference br, Matrix3d matrix)
        {
            if (IsSpatialElement(br))
            {
                var objs = ExplodeWithVisible(br);
                objs = ExplodeToBasic(objs);
                var obb = ToObb(objs.PolygonsEx());
                if (obb.Area > 1.0)
                {
                    var solid = obb.ToSolid();
                    solid.TransformBy(matrix);
                    elements.Add(new ThRawIfcSpatialElementData()
                    {
                        Geometry = solid.ToPolyline(),
                        Data = br.GetEffectiveName(),
                    });
                }
            }
        }
        private Polyline ToObb(DBObjectCollection basicCurves)
        {
            var curves = new DBObjectCollection();
            basicCurves.OfType<Entity>().ForEach(e =>
            {
                if (e is MPolygon mPolygon)
                {
                    e = mPolygon.Shell();
                }
                var newEnt = ThTesslateService.Tesslate(e, 100.0);
                if (newEnt != null)
                {
                    if (newEnt is Line line && line.Length > 0.0)
                    {
                        curves.Add(newEnt);
                    }
                    if (newEnt is Polyline polyline && polyline.Length > 0.0)
                    {
                        curves.Add(newEnt);
                    }
                }
            });
            if (curves.Count > 0)
            {
                var transformer = new ThMEPOriginTransformer(curves);
                transformer.Transform(curves);
                var obb = curves.GetMinimumRectangle();
                transformer.Reset(obb);
                return obb;
            }
            return new Polyline() { Closed = true };
        }
        private DBObjectCollection ExplodeToBasic(DBObjectCollection objs)
        {
            //理想是炸到Line,Arc,Circle,Ellipse,目前不支持对椭圆的处理，这里不抛出不支持的异常
            var results = new DBObjectCollection();
            objs.Cast<Entity>().Where(o => o is Curve).ForEach(c =>
                {
                    if (c is Line || c is Arc || c is Circle)
                    {
                        results.Add(c);
                    }
                    else if (c is Polyline poly)
                    {
                        var subObjs = new DBObjectCollection();
                        poly.Explode(subObjs);
                        subObjs.Cast<Curve>().ForEach(e => results.Add(e));
                    }
                    else if (c is Polyline2d poly2d)
                    {
                        var subObjs = new DBObjectCollection();
                        poly2d.Explode(subObjs);
                        subObjs.Cast<Curve>().ForEach(e => results.Add(e));
                    }
                });
            return results;
        }
        private DBObjectCollection ExplodeWithVisible(BlockReference blockReference)
        {
            var objs = new DBObjectCollection();
            var newObjs = new DBObjectCollection();
            blockReference.Explode(objs);
            objs.OfType<Entity>().ForEach(o =>
            {
                if (o is Curve curve)
                {
                    newObjs.Add(curve);
                }
                else if (o is BlockReference br)
                {
                    ExplodeWithVisible(br).OfType<Entity>().ForEach(e => newObjs.Add(e));
                }
            });
            return newObjs.OfType<Entity>()
                .Where(e => e.Visible)
                .Where(e => e.Bounds.HasValue)
                .ToCollection();
        }
    }
    public class ThLightingParkingStallVisitor : ThParkingStallVisitorBase 
    {
        public ThLightingParkingStallVisitor()
        {
            CheckQualifiedLayer = CheckLayerIsValid;
            CheckQualifiedBlockName = CheckBlockNameIsValid;
        }
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, Matrix3d.Identity);
            }
        }
        public override void DoExtract(List<ThRawIfcSpatialElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, matrix);
            }
        }
        public override void DoXClip(List<ThRawIfcSpatialElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }
        public override bool IsSpatialElement(Entity entity)
        {
            return CheckQualifiedBlockName(entity);
        }
        public override bool CheckLayerValid(Entity curve)
        {
            return CheckQualifiedLayer(curve);
        }
        private void HandleBlockReference(List<ThRawIfcSpatialElementData> elements, BlockReference br, Matrix3d matrix)
        {
            if (IsSpatialElement(br))
            {
                var objs = new DBObjectCollection();
                br.ExplodeWithVisible(objs);
                objs = ExplodeToBasic(objs);
                //防止有突刺，先求一个闭合区域，这里不考虑没有闭合的问题
                var obb = ToObb(objs.PolygonsEx());
                if (obb.Area > 1.0)
                {
                    var solid = obb.ToSolid();
                    solid.TransformBy(matrix);
                    elements.Add(new ThRawIfcSpatialElementData()
                    {
                        Geometry = solid.ToPolyline(),
                        Data = br.GetEffectiveName(),
                    });
                }
            }
        }
        private Polyline ToObb(DBObjectCollection basicCurves)
        {
            var curves = new DBObjectCollection();
            basicCurves.OfType<Entity>().ForEach(e =>
            {
                if (e is MPolygon mPolygon)
                {
                    e = mPolygon.Shell();
                }
                var newEnt = ThTesslateService.Tesslate(e, 100.0);
                if (newEnt != null)
                {
                    if (newEnt is Line line && line.Length > 0.0)
                    {
                        curves.Add(newEnt);
                    }
                    if (newEnt is Polyline polyline && polyline.Length > 0.0)
                    {
                        curves.Add(newEnt);
                    }
                }
            });
            if (curves.Count > 0)
            {
                var transformer = new ThMEPOriginTransformer(curves);
                transformer.Transform(curves);
                var obb = curves.GetMinimumRectangle();
                transformer.Reset(obb);
                return obb;
            }
            return new Polyline() { Closed = true };
        }
        private DBObjectCollection ExplodeToBasic(DBObjectCollection objs)
        {
            //理想是炸到Line,Arc,Circle,Ellipse,目前不支持对椭圆的处理，这里不抛出不支持的异常
            var results = new DBObjectCollection();
            objs.Cast<Entity>().Where(o => o is Curve).ForEach(c =>
            {
                if (c is Line || c is Arc || c is Circle)
                {
                    results.Add(c);
                }
                else if (c is Polyline poly)
                {
                    var subObjs = new DBObjectCollection();
                    poly.Explode(subObjs);
                    subObjs.Cast<Curve>().ForEach(e => results.Add(e));
                }
                else if (c is Polyline2d poly2d)
                {
                    var subObjs = new DBObjectCollection();
                    poly2d.Explode(subObjs);
                    subObjs.Cast<Curve>().ForEach(e => results.Add(e));
                }
            });
            return results;
        }
    }


    public abstract class ThParkingStallVisitorBase : ThSpatialElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public ThParkingStallVisitorBase()
        {
            CheckQualifiedLayer = CheckLayerIsValid;
            CheckQualifiedBlockName = CheckBlockNameIsValid;
        }
        protected bool CheckLayerIsValid(Entity e)
        {
            return LayerFilter.Where(o => string.Compare(e.Layer, o, true) == 0).Any();
        }
        protected bool CheckBlockNameIsValid(Entity e)
        {
            return e is BlockReference;
        }
    }
}
