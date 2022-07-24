using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class ThText
    {
        public static DBText NoteText(Point3d position, string textString)
        {
            var text = new DBText
            {
                TextString = textString,
                Position = new Point3d(position.X, position.Y, 0),
                LayerId = DbHelper.GetLayerId("W-NOTE"),
                WidthFactor = 0.7,
                Height = 350,
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
                ColorIndex = (int)ColorIndex.BYLAYER
        };
            return text;
        }

        public static DBText PipeText(Point3d position, string textString)
        {
            var text = new DBText
            {
                TextString = textString,
                Position = position,
                LayerId = DbHelper.GetLayerId("W-WSUP-NOTE"),
                WidthFactor = 0.7,
                Height = 350,
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
                ColorIndex = (int)ColorIndex.BYLAYER
            };
            return text;
        }

        public static DBText DbText(Point3d position, string textString, string layer)
        {
            var text = new DBText
            {
                TextString = textString,
                Position = position,
                LayerId = DbHelper.GetLayerId(layer),
                WidthFactor = 0.7,
                Height = 350,
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
                ColorIndex = (int)ColorIndex.BYLAYER
            };
            return text;
        }

    }
}
