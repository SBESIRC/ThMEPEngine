using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThWallExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(HandleCurve(polyline, matrix));
            }
            else if(dbObj is Line line)
            {
                elements.AddRange(HandleCurve(line, matrix));
            }
            else if (dbObj is Arc arc)
            {
                elements.AddRange(HandleCurve(arc, matrix));
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
        private List<ThRawIfcBuildingElementData> HandleCurve(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(polyline) && CheckLayerValid(polyline))
            {
                var clone = polyline.WashClone();
                if (clone != null && clone is Polyline)
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
        private List<ThRawIfcBuildingElementData> HandleCurve(Line line, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(line) && CheckLayerValid(line))
            {
                var clone = line.WashClone();
                if (clone != null && clone is Line)
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
        private List<ThRawIfcBuildingElementData> HandleCurve(Arc arc, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(arc) && CheckLayerValid(arc))
            {
                var clone = arc.WashClone();
                if (clone != null && clone is Arc)
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
    }
}
