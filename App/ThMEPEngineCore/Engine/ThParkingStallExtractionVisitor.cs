using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
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

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br);
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

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements,BlockReference br, Matrix3d matrix)
        {
            var texts = new List<DBText>();
            if (IsDistributionElement(br))
            {
                //var rectangle = br.GeometricExtents.ToRectangle();
                //rectangle.TransformBy(matrix);
                var objs = CAD.ThDrawTool.Explode(br);
                var obb = ToObb(objs);
                if(obb.Area>1.0)
                {
                    elements.Add(new ThRawIfcDistributionElementData()
                    {
                        Data = br.GetEffectiveName(),
                        Geometry = obb,
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
                  if(newEnt!=null)
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
            if(curves.Count>0)
            {
                var transformer = new ThMEPOriginTransformer(curves);
                transformer.Transform(curves);
                var obb = curves.GetMinimumRectangle();
                transformer.Reset(obb);
                return obb;
            }            
            return new Polyline() { Closed=true};
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference br)
        {
            // 获取ModelSpace中BlockReference的OBB
            var texts = new List<DBText>();
            if (IsDistributionElement(br))
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Use(br.Database))
                {
                    var btr = acadDatabase.Blocks.Element(br.BlockTableRecord);
                    var rectangle = btr.GeometricExtents().ToRectangle();
                    rectangle.TransformBy(br.BlockTransform);
                    elements.Add(new ThRawIfcDistributionElementData()
                    {
                        Data = br.GetEffectiveName(),
                        Geometry = rectangle
                    });
                }
            }
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
