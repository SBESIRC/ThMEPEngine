using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPPolygonShapeRecognitionService
    {
        public static bool IsAisle(AcPolygon frame, List<AcPolygon> holes, double shrinkValue, double threshold)
        {
            var objs = new DBObjectCollection
            {
                frame
            };
            holes.ForEach(x => objs.Add(x));
            var geometry = objs.BuildAreaGeometry();
            return ThMEPEngineCoreGeUtils.IsAisleArea(geometry, shrinkValue, threshold);
        }

        public static bool IsAisleBufferShrinkFrame(AcPolygon frame,List<AcPolygon >holes, double shrinkValue,double threshold)
        {
            var objs = new DBObjectCollection
            {
                frame
            };
            holes.ForEach(x => objs.Add(x));
            var geometry = objs.BuildAreaGeometry();
            return ThMEPEngineCoreGeUtils.IsAisleBufferShrinkFrame(geometry, shrinkValue, threshold);
        }

     
    }
}
