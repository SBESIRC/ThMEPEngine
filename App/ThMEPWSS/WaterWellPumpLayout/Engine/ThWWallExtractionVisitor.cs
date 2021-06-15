using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.WaterWellPumpLayout.Engine
{
    public class ThWWallExtractionVisitor : ThBuildingElementExtractionVisitor
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
            else if(dbObj is Mline mline)
            {
                elements.AddRange(HandleMline(mline, matrix));
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

        private List<ThRawIfcBuildingElementData> HandleLine(Line line, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(line) && CheckLayerValid(line))
            {
                var entity = line.GetTransformedCopy(matrix);
                results.Add(CreateBuildingElementData(entity));
            }
            return results;
        }
        private List<ThRawIfcBuildingElementData> HandlePolyline(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(polyline) && CheckLayerValid(polyline))
            {
                var entity = polyline.GetTransformedCopy(matrix);
                results.Add(CreateBuildingElementData(entity));
            }
            return results;
        }

        private List<ThRawIfcBuildingElementData> HandleMline(Mline mline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(mline) && CheckLayerValid(mline))
            {
                var objs = new DBObjectCollection();
                var newMline = mline.GetTransformedCopy(matrix) as Mline;
                newMline.Explode(objs);
                objs.Cast<Line>().ForEach(o=> results.Add(CreateBuildingElementData(o)));
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

