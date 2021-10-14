﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.Block
{
    public class FireDistrictLeft
    {
        public Point3d StPt { get; set; }
        public string FloorNum { get; set; }
        public string Area { get; set; }
        public Dictionary<int, string> FloorDic { get; set; }
        public TermPoint2 TermPt { get; set; }
        public FireDistrictLeft(Point3d stPt, TermPoint2 termPoint)
        {
            StPt = stPt;
            FloorNum = termPoint.PipeNumber.Replace("接至", "");
            TermPt = termPoint;
        }
        public void InsertBlock(AcadDatabase acadDatabase)
        {
            BlocksImport.ImportElementsFromStdDwg();

            InsertLine(acadDatabase, StPt, StPt.OffsetXReverse(300), "W-FRPT-SPRL-PIPE");

            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "遥控信号阀",
                    StPt.OffsetXReverse(300), new Scale3d(-1, 1, 1), 0);

            InsertLine(acadDatabase, StPt.OffsetXReverse(600), StPt.OffsetXReverse(650), "W-FRPT-SPRL-PIPE");

            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "水流指示器",
                    StPt.OffsetXReverse(770), new Scale3d(-1, 1, 1), 0);

            InsertLine(acadDatabase, StPt.OffsetXReverse(890), StPt.OffsetXReverse(1090), "W-FRPT-SPRL-PIPE");

            var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "减压孔板",
                    StPt.OffsetXReverse(1190), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性", "水平");

            InsertLine(acadDatabase, StPt.OffsetXReverse(1290), StPt.OffsetXReverse(2140), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverse(2140), StPt.OffsetXReverse(2740), "W-FRPT-SPRL-EQPM");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "喷头系统",
                    StPt.OffsetXReverse(1790), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性", "上喷闭式");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "喷头系统",
                    StPt.OffsetXReverse(3090), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性", "上喷闭式");

            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "水管中断",
                    StPt.OffsetXReverse(2140), new Scale3d(-1.2, 1.2, 1.2), Math.PI);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "水管中断",
                    StPt.OffsetXReverse(2740), new Scale3d(-1.2, 1.2, 1.2), Math.PI);

            InsertLine(acadDatabase, StPt.OffsetXReverse(2740), StPt.OffsetXReverse(3690), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverse(3690), StPt.OffsetXReverseY(3690, -2350), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3690, -2200), StPt.OffsetXReverseY(3940, -2200), "W-FRPT-SPRL-PIPE");
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "压力表",
                    StPt.OffsetXReverseY(3940, -2200), new Scale3d(-1.5, 1.5, 1.5), 0);

            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "截止阀",
                    StPt.OffsetXReverseY(3690, -2500), new Scale3d(1, 1, 1), Math.PI / 2);

            InsertLine(acadDatabase, StPt.OffsetXReverseY(3690, -2650), StPt.OffsetXReverseY(3690, -2800), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3540, -2800), StPt.OffsetXReverseY(3840, -2800), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3540, -2800), StPt.OffsetXReverseY(3690, -3050), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3840, -2800), StPt.OffsetXReverseY(3690, -3050), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3490, -3200), StPt.OffsetXReverseY(3890, -3200), "W-DRAI-EQPM");

            var arc = new Arc(StPt.OffsetXReverseY(3690, -3200), new Vector3d(0, 0, 1), 200, Math.PI, Math.PI * 2);
            arc.LayerId = DbHelper.GetLayerId("W-DRAI-EQPM");
            acadDatabase.CurrentSpace.Add(arc);

            InsertLine(acadDatabase, StPt.OffsetXReverseY(3690, -3400), StPt.OffsetXReverseY(3690, -5800), "W-FRPT-DRAI-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(690, -5800), StPt.OffsetXReverseY(3690, -5800), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(690, -410), StPt.OffsetXReverseY(690, -5800), "W-FRPT-DRAI-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(690, -410), StPt.OffsetXReverseY(1140, -410), "W-FRPT-DRAI-PIPE");

            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "截止阀",
                    StPt.OffsetXReverseY(1290, -410), new Scale3d(1, 1, 1), Math.PI);

            InsertLine(acadDatabase, StPt.OffsetXReverseY(1440, -410), StPt.OffsetXReverseY(1640, -410), "W-FRPT-DRAI-PIPE");

            InsertLine(acadDatabase, StPt.OffsetXReverse(1640), StPt.OffsetXReverseY(1640, -410), "W-FRPT-DRAI-PIPE");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-DIMS", "标高", StPt.OffsetXReverseY(3190, -2500),
                new Scale3d(1, 1, 1), 0, new Dictionary<string, string> { { "标高", "h+1.50" } });
            //objID.SetDynBlockValue("翻转状态2", x);

            InsertLine(acadDatabase, StPt.OffsetXReverseY(1346, -114), StPt.OffsetXReverseY(1753, -690), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(1753, -690), StPt.OffsetXReverseY(3640, -690), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(1440, -510), StPt.OffsetXReverseY(1917, -1250), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(1917, -1250), StPt.OffsetXReverseY(2660, -1250), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(2977, -2965), StPt.OffsetXReverseY(3340, -2670), "W-FRPT-NOTE");
            InsertLine(acadDatabase, StPt.OffsetXReverseY(3100, -3630), StPt.OffsetXReverseY(3520, -3107), "W-FRPT-NOTE");

            InsertText(acadDatabase, StPt.OffsetXReverseY(3640, -640), "减压孔板XXmm");
            InsertText(acadDatabase, StPt.OffsetXReverseY(2660, -1180), "泄水阀");
            InsertText(acadDatabase, StPt.OffsetXReverseY(2210, -3730), FloorNum);
            InsertText(acadDatabase, StPt.OffsetXReverseY(3330, -5700), "排至地下二层集水坑");

            InsertText(acadDatabase, StPt.OffsetXReverseY(3075, -3380), "截止阀", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXReverseY(3160, -4000), "K=80", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXReverseY(3310, -4450), "试水接头", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXReverseY(1050, 200), "DN150");
            InsertText(acadDatabase, StPt.OffsetXReverseY(250, -5250), "DN50", "W-FRPT-SPRL-DIMS", Math.PI / 2);
            InsertText(acadDatabase, StPt.OffsetXReverseY(3340, -5250), "DN80", "W-FRPT-SPRL-DIMS", Math.PI / 2);

            InsertSolid(acadDatabase, StPt.OffsetXReverseY(3497, -2546), StPt.OffsetXReverseY(3320, -2646), StPt.OffsetXReverseY(3362, -2698));
            InsertSolid(acadDatabase, StPt.OffsetXReverseY(3615, -2927), StPt.OffsetXReverseY(3523, -3107), StPt.OffsetXReverseY(3469, -3068));
        } 

        private void InsertLine(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2, string layer)
        {
            var line = new Line(pt1, pt2)
            {
                LayerId = DbHelper.GetLayerId(layer),
                ColorIndex = (int)ColorIndex.BYLAYER
        };
            acadDatabase.CurrentSpace.Add(line);
        }

        private void InsertSolid(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2, Point3d pt3, string layer = "W-FRPT-NOTE")
        {
            Solid solid = new Solid(pt1, pt2, pt3)
            {
                LayerId = DbHelper.GetLayerId(layer),
                ColorIndex = (int)ColorIndex.BYLAYER
            };
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

            acadDatabase.CurrentSpace.Add(dbText);
        }
    }
}

