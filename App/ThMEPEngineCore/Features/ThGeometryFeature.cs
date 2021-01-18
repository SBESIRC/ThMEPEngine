using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

namespace ThMEPEngineCore.Features
{
    public class ThGeometryFeature
    {
        public static Feature Construct(ThGeometry geometry)
        {
            if (geometry != null)
            {
                using (var ov = new ThCADCoreNTSFixedPrecision())
                {
                    var objs = new DBObjectCollection();
                    geometry.Segments.ForEach(o=> objs.Add(o));
                    var geo = objs.ToNTSNodedLineStrings();
                    var attributesTable = new AttributesTable(geometry.Properties);
                    var feature = new Feature(geo, attributesTable);
                    return feature;
                }
            }
            return null;
        }
    }
}
