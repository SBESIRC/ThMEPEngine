using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.UndergroundSpraySystem.Block
{
    public class AlarmValveSys
    {
        private Point3d StPt { get; set; }
        private double PipeLength { get; set; }
        public Point3d EndPt { get; set; }
        private Matrix3d U2WMat { get; set; }

        public AlarmValveSys(Point3d stPt, int alarmValveIndex, double floorHeight)
        {
            StPt = stPt;
            PipeLength = floorHeight - 2600 - 2550 - 600 * alarmValveIndex;
            EndPt = StPt.OffsetY(1550 + PipeLength);
            U2WMat = Active.Editor.UCS2WCS();
        }

        public void Insert(AcadDatabase acadDatabase)
        {
            InsertLine(acadDatabase, StPt, StPt.OffsetY(150));
            InsertLine(acadDatabase, StPt.OffsetY(450), StPt.OffsetY(550));
            InsertLine(acadDatabase, StPt.OffsetY(850), StPt.OffsetY(1250));
            InsertLine(acadDatabase, StPt.OffsetY(1550), StPt.OffsetY(1550 + PipeLength));

            InsertLine(acadDatabase, StPt.OffsetXY(-150, 700), StPt.OffsetXY(-150, 1300), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXY(-300, 1300), StPt.OffsetXY(-150, 1300), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetY(1050), StPt.OffsetXY(300, 1050), "W-FRPT-DRAI-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXY(300, 700), StPt.OffsetXY(300, 1050), "W-FRPT-DRAI-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXY(300, 250), StPt.OffsetXY(300, 400), "W-FRPT-DRAI-PIPE");

            InsertBlock(acadDatabase, "遥控信号阀", StPt.OffsetY(150));
            InsertBlock(acadDatabase, "湿式报警阀系统", StPt.OffsetXY(150, 700), "W-FRPT-SPRL-EQPM", 1, Math.PI);
            InsertBlock(acadDatabase, "遥控信号阀", StPt.OffsetY(1250));
            InsertBlock(acadDatabase, "截止阀", StPt.OffsetXY(300, 550));
            InsertBlock(acadDatabase, "水力警铃", StPt.OffsetXY(-300, 1300));

            InsertText(acadDatabase, StPt.OffsetXY(750, 200), "DN50", Math.PI / 2);
        }

        private void InsertLine(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2, string layer = "W-FRPT-SPRL-PIPE")
        {
            var line = new Line(pt1, pt2)
            {
                LayerId = DbHelper.GetLayerId(layer),
                ColorIndex = (int)ColorIndex.BYLAYER
            };
            line.TransformBy(U2WMat);
            acadDatabase.CurrentSpace.Add(line);
        }

        private void InsertBlock(AcadDatabase acadDatabase, string blockName, Point3d pt, string layer = "W-FRPT-SPRL-EQPM", double scaled = 1, double rotation = Math.PI/2)
        {
            var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(layer, blockName, pt, 
                new Scale3d(scaled, scaled, scaled), rotation);
            var blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);
        }

        private void InsertText(AcadDatabase acadDatabase, Point3d insertPt, string text, double rotation = 0, string layer = "W-FRPT-HYDT-DIMS")
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
            dbText.TransformBy(U2WMat);
            acadDatabase.CurrentSpace.Add(dbText);
        }
    }
}
