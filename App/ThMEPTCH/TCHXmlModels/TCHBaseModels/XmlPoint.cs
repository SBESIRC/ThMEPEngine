using Autodesk.AutoCAD.Geometry;
using System.Linq;

namespace ThMEPTCH.TCHXmlModels.TCHBaseModels
{
    public class XmlPoint : XmlString
    {
        public Point3d? GetCADPoint() 
        {
            if (string.IsNullOrEmpty(value))
                return null;
            var ptValues = value.Split(',').ToList();
            if (ptValues.Count == 3) 
            {
                var xValue = 0.0;
                var yValue = 0.0;
                var zValue = 0.0;
                double.TryParse(ptValues[0],out xValue);
                double.TryParse(ptValues[1], out yValue);
                double.TryParse(ptValues[2], out zValue);
                return new Point3d(xValue, yValue, zValue);
            }
            return null;
        }
    }
}
