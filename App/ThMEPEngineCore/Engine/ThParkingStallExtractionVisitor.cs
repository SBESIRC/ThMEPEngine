using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

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
                var obb = ToObb(objs);
                if (obb.Area > 1.0)
                {
                    obb.TransformBy(matrix);
                    elements.Add(new ThRawIfcDistributionElementData()
                    {
                        Geometry = obb,
                        Data = br.GetEffectiveName(),
                    });
                }
            }
        }

        private Polyline ToObb(DBObjectCollection objs)
        {
            var curves = new DBObjectCollection();
            objs.Cast<Entity>().Where(o => o is Curve).ToList().ForEach(e =>
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
