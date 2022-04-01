using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.UndergroundSpraySystem.Block
{
    public class WaterPump
    {
        public Point3d StPt { get; set; }
        private string PumpText { get; set; }
        private string PipeDN { get; set; }
        public WaterPump(Point3d stPt, string text, string DN)
        {
            StPt = stPt;
            PumpText = text;
            PipeDN = DN;
        }
        public void Insert(AcadDatabase acadDatabase)
        {
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "水泵接合器接口",
                    StPt.OffsetXY(-3200, 1200), new Scale3d(1, 1, 1), 0);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "水泵接合器接口",
                    StPt.OffsetXY(-2600, 1200), new Scale3d(1, 1, 1), 0);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "止回阀",
                    StPt.OffsetXY(-2300, 700), new Scale3d(1, 1, 1), 0);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "止回阀",
                    StPt.OffsetX(-2300), new Scale3d(1, 1, 1), 0);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "安全阀",
                    StPt.OffsetXY(-1700, 700), new Scale3d(1, 1, 1), 0);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "安全阀",
                    StPt.OffsetX(-1700), new Scale3d(1, 1, 1), 0);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "闸阀",
                    StPt.OffsetXY(-1400, 700), new Scale3d(1, 1, 1), 0);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "闸阀",
                    StPt.OffsetX(-1400), new Scale3d(1, 1, 1), 0);

            InsertLine(acadDatabase, StPt.OffsetXY(-3200, 1200), StPt.OffsetX(-3200));
            InsertLine(acadDatabase, StPt.OffsetX(-3200), StPt.OffsetX(-2300));
            InsertLine(acadDatabase, StPt.OffsetX(-2000), StPt.OffsetX(-1400));
            InsertLine(acadDatabase, StPt.OffsetX(-1100), StPt);
            InsertLine(acadDatabase, StPt.OffsetXY(-2600, 1200), StPt.OffsetXY(-2600, 700));
            InsertLine(acadDatabase, StPt.OffsetXY(-2600, 700), StPt.OffsetXY(-2300, 700));
            InsertLine(acadDatabase, StPt.OffsetXY(-2000, 700), StPt.OffsetXY(-1400, 700));
            InsertLine(acadDatabase, StPt.OffsetXY(-1100, 700), StPt.OffsetXY(-800, 700));
            InsertLine(acadDatabase, StPt.OffsetXY(-800, 700), StPt.OffsetX(-800));
            InsertLine(acadDatabase, StPt.OffsetXY(-3129, 1412), StPt.OffsetXY(-2829, 2012), "W-NOTE");
            InsertLine(acadDatabase, StPt.OffsetXY(-2529, 1412), StPt.OffsetXY(-2829, 2012), "W-NOTE");
            InsertLine(acadDatabase, StPt.OffsetXY(670, 2012), StPt.OffsetXY(-2829, 2012), "W-NOTE");

            InsertText(acadDatabase, StPt.OffsetXY(-2829, 2012), PumpText);
            if(!PipeDN.Equals(""))
            {
                InsertText(acadDatabase, StPt.OffsetXY(-2000, 1600), PipeDN + "*2");
            }

        }

        private void InsertLine(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2, string layer = "W-FRPT-SPRL-PIPE")
        {
            var line = new Line(pt1, pt2)
            {
                LayerId = DbHelper.GetLayerId(layer),
                ColorIndex = (int)ColorIndex.BYLAYER
            };
            acadDatabase.CurrentSpace.Add(line);
        }

        private void InsertText(AcadDatabase acadDatabase, Point3d insertPt, string text, string layer = "W-NOTE", double rotation = 0)
        {
            var dbText = new DBText
            {
                TextString = text,
                Position = insertPt,
                LayerId = DbHelper.GetLayerId(layer),
                Rotation = rotation,
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
                Height = 350,
                WidthFactor = 0.7,
                ColorIndex = (int)ColorIndex.BYLAYER
            };

            acadDatabase.CurrentSpace.Add(dbText);
        }
    }
}
