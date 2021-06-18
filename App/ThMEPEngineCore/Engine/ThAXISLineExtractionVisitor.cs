using System;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThAXISLineExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Line line)
            {
                elements.AddRange(HandleLine(line, matrix));
            }
            else if (dbObj is Polyline polyline)
            {
                elements.AddRange(HandlePolyline(polyline, matrix));
            }
            else if (dbObj is Mline mline)
            {
                throw new NotSupportedException();
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
            return true;
        }

        public override bool CheckLayerValid(Entity entity)
        {
            var layer = entity.Layer;
            return layer.Contains("AXIS") && 
                !layer.Contains("CRCL") && 
                !layer.Contains("NUMB") &&
                !layer.Contains("DIMS");
        }

        private List<ThRawIfcBuildingElementData> HandleLine(Line line, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(line) && CheckLayerValid(line))
            {
                results.Add(CreateBuildingElementData(line.GetTransformedCopy(matrix)));
            }
            return results;
        }

        private List<ThRawIfcBuildingElementData> HandlePolyline(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(polyline) && CheckLayerValid(polyline))
            {
                var entitySet = new DBObjectCollection();
                polyline.Explode(entitySet);
                entitySet.Cast<Entity>().ForEach(o => results.Add(CreateBuildingElementData(o.GetTransformedCopy(matrix))));
            }
            return results;
        }

        private ThRawIfcBuildingElementData CreateBuildingElementData(Entity entity)
        {
            return new ThRawIfcBuildingElementData()
            {
                Geometry = entity,
            };
        }
    }
}
