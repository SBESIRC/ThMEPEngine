using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundSpraySystem.General;
using AcHelper;
using GeometryExtensions;

namespace ThMEPWSS.UndergroundSpraySystem.Block
{
    public class FireDistrictLeft
    {
        public Point3d StPt { get; set; }
        public string FloorNum { get; set; }
        public TermPoint2 TermPt { get; set; }
        private Matrix3d U2WMat { get; set; }

        public FireDistrictLeft(Point3d stPt, TermPoint2 termPoint)
        {
            StPt = stPt;
            FloorNum = termPoint.PipeNumber.Replace("接至", "").Split('喷')[0];
            TermPt = termPoint;
            U2WMat = Active.Editor.UCS2WCS();
        }

        public void InsertBlock(AcadDatabase acadDatabase)
        {
            InsertLine(acadDatabase, StPt, StPt.OffsetXReverse(300), "W-FRPT-SPRL-PIPE");

            var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "遥控信号阀",
                    StPt.OffsetXReverse(300), new Scale3d(-1, 1, 1), 0);
            var blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetXReverse(600), StPt.OffsetXReverse(650), "W-FRPT-SPRL-PIPE");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "水流指示器",
                    StPt.OffsetXReverse(770), new Scale3d(-1, 1, 1), 0);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetXReverse(890), StPt.OffsetXReverse(1090), "W-FRPT-SPRL-PIPE");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "减压孔板",
                    StPt.OffsetXReverse(1190), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性", "水平");
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetXReverse(1290), StPt.OffsetXReverse(2140), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverse(2140), StPt.OffsetXReverse(2740), "W-FRPT-SPRL-EQPM");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "喷头系统",
                    StPt.OffsetXReverse(1790), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性", "上喷闭式");
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "喷头系统",
                    StPt.OffsetXReverse(3090), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性", "上喷闭式");
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "水管中断",
                    StPt.OffsetXReverse(2140), new Scale3d(-1.2, 1.2, 1.2), Math.PI);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "水管中断",
                    StPt.OffsetXReverse(2740), new Scale3d(-1.2, 1.2, 1.2), Math.PI);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetXReverse(2740), StPt.OffsetXReverse(3690), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverse(3690), StPt.OffsetXReverseY(3690, -1350), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3690, -1200), StPt.OffsetXReverseY(3940, -1200), "W-FRPT-SPRL-PIPE");
            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "压力表",
                    StPt.OffsetXReverseY(3940, -1200), new Scale3d(-1.5, 1.5, 1.5), 0);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "截止阀",
                    StPt.OffsetXReverseY(3690, -1500), new Scale3d(1, 1, 1), Math.PI / 2);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetXReverseY(3690, -1650), StPt.OffsetXReverseY(3690, -1800), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3540, -1800), StPt.OffsetXReverseY(3840, -1800), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3540, -1800), StPt.OffsetXReverseY(3690, -2050), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3840, -1800), StPt.OffsetXReverseY(3690, -2050), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3490, -2200), StPt.OffsetXReverseY(3890, -2200), "W-DRAI-EQPM");

            var arc = new Arc(StPt.OffsetXReverseY(3690, -2200), new Vector3d(0, 0, 1), 200, Math.PI, Math.PI * 2);
            arc.LayerId = DbHelper.GetLayerId("W-DRAI-EQPM");
            arc.TransformBy(U2WMat);
            acadDatabase.CurrentSpace.Add(arc);

            InsertLine(acadDatabase, StPt.OffsetXReverseY(3690, -2400), StPt.OffsetXReverseY(3690, -4500), "W-FRPT-DRAI-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(690, -4500), StPt.OffsetXReverseY(3690, -4500), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(690, -410), StPt.OffsetXReverseY(690, -4500), "W-FRPT-DRAI-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(690, -410), StPt.OffsetXReverseY(1140, -410), "W-FRPT-DRAI-PIPE");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "截止阀",
                    StPt.OffsetXReverseY(1290, -410), new Scale3d(1, 1, 1), Math.PI);
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetXReverseY(1440, -410), StPt.OffsetXReverseY(1640, -410), "W-FRPT-DRAI-PIPE");

            InsertLine(acadDatabase, StPt.OffsetXReverse(1640), StPt.OffsetXReverseY(1640, -410), "W-FRPT-DRAI-PIPE");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-DIMS", "标高", StPt.OffsetXReverseY(3690, -1500),
                new Scale3d(1, 1, 1), 0, new Dictionary<string, string> { { "标高", "h+1.50" } });
            blk = acadDatabase.Element<BlockReference>(objID);
            blk.TransformBy(U2WMat);

            InsertLine(acadDatabase, StPt.OffsetXReverseY(1346, -114), StPt.OffsetXReverseY(1753, -690), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(1753, -690), StPt.OffsetXReverseY(3640, -690), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(2977, -1965), StPt.OffsetXReverseY(3340, -1670), "W-FRPT-NOTE");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3100, -2630), StPt.OffsetXReverseY(3520, -2107), "W-FRPT-NOTE");

            InsertText(acadDatabase, StPt.OffsetXReverseY(3640, -640), "减压孔板XXmm");
            InsertText(acadDatabase, StPt.OffsetXReverseY(2850 + 240, -3910), FloorNum);
            InsertText(acadDatabase, StPt.OffsetXReverseY(3200, -4400), "排至地下二层集水坑");

            InsertText(acadDatabase, StPt.OffsetXReverseY(3075, -2380), "截止阀", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXReverseY(3160, -3000), "K=80", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXReverseY(3310, -3450), "试水接头", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXReverseY(250, -3950), "DN50", "W-FRPT-SPRL-DIMS", Math.PI / 2);
            InsertText(acadDatabase, StPt.OffsetXReverseY(3340, -3950), "DN80", "W-FRPT-SPRL-DIMS", Math.PI / 2);

            InsertSolid(acadDatabase, StPt.OffsetXReverseY(3497, -1546), StPt.OffsetXReverseY(3320, -1646), StPt.OffsetXReverseY(3362, -1698));
            InsertSolid(acadDatabase, StPt.OffsetXReverseY(3615, -1927), StPt.OffsetXReverseY(3523, -2107), StPt.OffsetXReverseY(3469, -2068));
        } 

        private void InsertLine(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2, string layer)
        {
            var line = new Line(pt1, pt2)
            {
                LayerId = DbHelper.GetLayerId(layer),
                ColorIndex = (int)ColorIndex.BYLAYER
            };
            line.TransformBy(U2WMat);
            acadDatabase.CurrentSpace.Add(line);
        }

        private void InsertSolid(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2, Point3d pt3, string layer = "W-FRPT-NOTE")
        {
            Solid solid = new Solid(pt1, pt2, pt3)
            {
                LayerId = DbHelper.GetLayerId(layer),
                ColorIndex = (int)ColorIndex.BYLAYER
            };
            solid.TransformBy(U2WMat);
            acadDatabase.CurrentSpace.Add(solid);
        }

        private void InsertText(AcadDatabase acadDatabase, Point3d insertPt, string text, string layer = "W-FRPT-SPRL-DIMS", double rotation = 0)
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

