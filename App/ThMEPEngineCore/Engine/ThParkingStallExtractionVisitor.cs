using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;

namespace ThMEPEngineCore.Engine
{
    public class ThParkingStallExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public ThParkingStallExtractionVisitor()
        {
            CheckQualifiedLayer = base.CheckLayerValid;
            CheckQualifiedBlockName = (Entity entity) => true;
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, matrix);
            }
        }
        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference br, Matrix3d matrix)
        {
            if (IsDistributionElement(br))
            {
                var objs = CAD.ThDrawTool.Explode(br);
                objs = ExplodeToBasic(objs);
                var obb = ToObb(objs);
                if (obb.Area > 1.0)
                {
                    var solid = obb.ToSolid();
                    solid.TransformBy(matrix);
                    elements.Add(new ThRawIfcDistributionElementData()
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
            basicCurves.Cast<Curve>().ForEach(e =>
            {
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
            //理想是炸到Line,Arc,Circle,Ellipse
            var results = new DBObjectCollection();
            objs.Cast<Entity>().Where(o => o is Curve).ForEach(c =>
                {
                    if(c is Line || c is Arc || c is Circle)
                    {
                        results.Add(c);
                    }
                    else if(c is Polyline poly)
                    {
                        var subObjs = new DBObjectCollection();
                        poly.Explode(subObjs);
                        subObjs.Cast<Curve>().ForEach(e => results.Add(e));
                    }
                    else if(c is Polyline2d poly2d)
                    {
                        var subObjs = new DBObjectCollection();
                        poly2d.Explode(subObjs);
                        subObjs.Cast<Curve>().ForEach(e => results.Add(e));
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                });
            return results;
        }

        public override bool IsDistributionElement(Entity entity)
        {
            return CheckQualifiedBlockName(entity);
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return CheckQualifiedLayer(curve);
        }
    }
}
