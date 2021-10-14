using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.UndergroundSpraySystem.Block
{
    public class Text
    {
        public DBText DbText { get; set; }

        public Text(string text, Point3d pt, string layer = "W-FRPT-HYDT-DIMS", double angle = 0)
        {
            DbText = new DBText()
            {
                TextString = text,
                Position = pt.OffsetXY(50, 50),
                LayerId = DbHelper.GetLayerId(layer),
                WidthFactor = 0.7,
                Height = 350,
                Rotation = angle,
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
                ColorIndex = (int)ColorIndex.BYLAYER
            };
        }
    }
}
