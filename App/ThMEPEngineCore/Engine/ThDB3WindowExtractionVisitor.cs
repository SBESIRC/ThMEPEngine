using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3WindowExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(HandleCurve(polyline, matrix));
            }
            else if (dbObj is BlockReference br)
            {
                elements.AddRange(HandleBlockreference(br, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

        public override bool IsBuildElement(Entity entity)
        {
            if (entity.Hyperlinks.Count > 0)
            {
                var thPropertySet = ThPropertySet.CreateWithHyperlink(entity.Hyperlinks[0].Description);
                return thPropertySet.IsWindow;
            }
            return false;
        }

        public override bool IsBuildElementBlockReference(BlockReference blockReference)
        {
            return blockReference.BlockTableRecord.IsValid && 
                CheckLayerValid(blockReference) && 
                IsBuildElement(blockReference);
        }

        private List<ThRawIfcBuildingElementData> HandleCurve(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(polyline) && CheckLayerValid(polyline))
            {
                var clone = polyline.WashClone();
                if(clone!= null)
                {
                    clone.TransformBy(matrix);
                    results.Add(new ThRawIfcBuildingElementData()
                    {
                        Geometry = clone,
                    });
                }
            }
            return results;
        }

        private List<ThRawIfcBuildingElementData> HandleBlockreference(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            var objs = ThDrawTool.Explode(br);
            objs = VisibleEntities(objs);
            objs.OfType<Curve>().ForEach(c =>
            {
                var clone = c.WashClone();
                if (clone != null)
                {
                    clone.TransformBy(matrix);
                    results.Add(new ThRawIfcBuildingElementData()
                    {
                        Geometry = clone,
                    });
                }
            });
            return results;
        }

        private DBObjectCollection VisibleEntities(DBObjectCollection objs)
        {
            return objs.OfType<Entity>()
                .Where(e => e.Visible)
                .ToCollection();
        }
    }
}
