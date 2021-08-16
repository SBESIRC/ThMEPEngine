using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3CorniceExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(Handle(polyline, matrix));
            }
            else if(dbObj is Line line)
            {
                elements.AddRange(Handle(line, matrix));
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
        private List<ThRawIfcBuildingElementData> Handle(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(polyline) && CheckLayerValid(polyline))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = polyline.GetTransformedCopy(matrix),
                });
            }
            return results;
        }

        private List<ThRawIfcBuildingElementData> Handle(Line line, Matrix3d matrix)
        {
            List<Curve> curves = new List<Curve>();
            if (IsBuildElement(line) && CheckLayerValid(line))
            {
                var newLine = line.WashClone() as Line;
                newLine.TransformBy(matrix);

                var poly = new Polyline();
                poly.AddVertexAt(0, new Point2d(newLine.StartPoint.X, newLine.StartPoint.Y), 0, 0, 0);
                poly.AddVertexAt(1, new Point2d(newLine.EndPoint.X, newLine.EndPoint.Y), 0, 0, 0);
                curves.Add(poly);
            }
            return curves.Select(o => CreateBuildingElementData(o)).ToList();
        }

        private ThRawIfcBuildingElementData CreateBuildingElementData(Curve curve)
        {
            return new ThRawIfcBuildingElementData()
            {
                Geometry = curve,
            };
        }

        public override bool IsBuildElement(Entity entity)
        {
            if (entity.Hyperlinks.Count > 0)
            {
                var thPropertySet = ThPropertySet.CreateWithHyperlink(entity.Hyperlinks[0].Description);
                return thPropertySet.IsCornice;
            }
            return false;
        }
        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }
    }
}
