using Autodesk.AutoCAD.DatabaseServices;
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
    public class FireDistrictRight
    {
        public Point3d StPt { get; set; }
        public string FloorNum { get; set; }
        public string Area { get; set; }
        public Dictionary<int, string> FloorDic { get; set; }
        public TermPoint2 TermPt { get; set; }
        public FireDistrictRight(Point3d stPt, TermPoint2 termPoint)
        {
            StPt = stPt;
            FloorNum = termPoint.PipeNumber.Replace("接至", "");
            TermPt = termPoint;
        }
        public void InsertBlock(AcadDatabase acadDatabase)
        {
            BlocksImport.ImportElementsFromStdDwg();

            InsertLine(acadDatabase, StPt, StPt.OffsetX(300), "W-FRPT-SPRL-PIPE");

            if(TermPt.HasSignalValve)
            {
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "遥控信号阀",
                    StPt.OffsetX(300), new Scale3d(1, 1, 1), 0);
            }
            else
            {
                InsertLine(acadDatabase, StPt.OffsetX(300), StPt.OffsetX(600), "W-FRPT-SPRL-PIPE");
            }
           
            InsertLine(acadDatabase, StPt.OffsetX(600), StPt.OffsetX(650), "W-FRPT-SPRL-PIPE");

            if (TermPt.HasFlow)
            {
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "水流指示器",
                    StPt.OffsetX(770), new Scale3d(1, 1, 1), 0);
            }
            else
            {
                InsertLine(acadDatabase, StPt.OffsetX(650), StPt.OffsetX(890), "W-FRPT-SPRL-PIPE");
            }

            InsertLine(acadDatabase, StPt.OffsetX(890), StPt.OffsetX(1090), "W-FRPT-SPRL-PIPE");

            var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "减压孔板",
                    StPt.OffsetX(1190), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性","水平");

            InsertLine(acadDatabase, StPt.OffsetX(1290), StPt.OffsetX(2140), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetX(2140), StPt.OffsetX(2740), "W-FRPT-SPRL-EQPM");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "喷头系统",
                    StPt.OffsetX(1790), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性", "上喷闭式");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "喷头系统",
                    StPt.OffsetX(3090), new Scale3d(1, 1, 1), 0);
            objID.SetDynBlockValue("可见性", "上喷闭式");

            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "水管中断",
                    StPt.OffsetX(2140), new Scale3d(-1.2, 1.2, 1.2), Math.PI);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "水管中断",
                    StPt.OffsetX(2740), new Scale3d(-1.2, 1.2, 1.2), Math.PI);

            InsertLine(acadDatabase, StPt.OffsetX(2740), StPt.OffsetX(3690), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetX(3690), StPt.OffsetXY(3690, -2350), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXY(3690, -2200), StPt.OffsetXY(3940, -2200), "W-FRPT-SPRL-PIPE");
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "压力表",
                    StPt.OffsetXY(3940, -2200), new Scale3d(1.5, 1.5, 1.5), 0);

            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "截止阀",
                    StPt.OffsetXY(3690, -2500), new Scale3d(1, 1, 1), Math.PI/2);

            InsertLine(acadDatabase, StPt.OffsetXY(3690, -2650), StPt.OffsetXY(3690, -2800), "W-FRPT-SPRL-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXY(3540, -2800), StPt.OffsetXY(3840, -2800), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXY(3540, -2800), StPt.OffsetXY(3690, -3050), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXY(3840, -2800), StPt.OffsetXY(3690, -3050), "W-FRPT-SPRL-EQPM");
            InsertLine(acadDatabase, StPt.OffsetXY(3490, -3200), StPt.OffsetXY(3890, -3200), "W-DRAI-EQPM");

            var arc = new Arc(StPt.OffsetXY(3690, -3200), new Vector3d(0, 0, 1), 200, Math.PI, Math.PI * 2);
            arc.LayerId = DbHelper.GetLayerId("W-DRAI-EQPM");
            acadDatabase.CurrentSpace.Add(arc);

            InsertLine(acadDatabase, StPt.OffsetXY(3690, -3400), StPt.OffsetXY(3690, -5800), "W-FRPT-DRAI-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXY(690, -5800), StPt.OffsetXY(3690, -5800), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXY(690, -410), StPt.OffsetXY(690, -5800), "W-FRPT-DRAI-PIPE");
            InsertLine(acadDatabase, StPt.OffsetXY(690, -410), StPt.OffsetXY(1140, -410), "W-FRPT-DRAI-PIPE");

            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-EQPM", "截止阀",
                    StPt.OffsetXY(1290, -410), new Scale3d(1, 1, 1), Math.PI);

            InsertLine(acadDatabase, StPt.OffsetXY(1440, -410), StPt.OffsetXY(1640, -410), "W-FRPT-DRAI-PIPE");

            InsertLine(acadDatabase, StPt.OffsetX(1640), StPt.OffsetXY(1640, -410), "W-FRPT-DRAI-PIPE");

            objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-SPRL-DIMS", "标高", StPt.OffsetXY(3190, -2500),
                new Scale3d(1, 1, 1), 0, new Dictionary<string, string> { { "标高", "h+1.50" } });
            //objID.SetDynBlockValue("翻转状态2", x);

            InsertLine(acadDatabase, StPt.OffsetXY(1346, -114), StPt.OffsetXY(1753, -690), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXY(1753, -690), StPt.OffsetXY(3640, -690), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXY(1440, -510), StPt.OffsetXY(1917, -1250), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXY(1917, -1250), StPt.OffsetXY(2660, -1250), "W-FRPT-SPRL-DIMS");
            InsertLine(acadDatabase, StPt.OffsetXY(2977, -2965), StPt.OffsetXY(3340, -2670), "W-FRPT-NOTE");
            InsertLine(acadDatabase, StPt.OffsetXY(3100, -3630), StPt.OffsetXY(3520, -3107), "W-FRPT-NOTE");

            InsertText(acadDatabase, StPt.OffsetXY(1780, -640), "减压孔板XXmm");
            InsertText(acadDatabase, StPt.OffsetXY(1940, -1180), "泄水阀");
            InsertText(acadDatabase, StPt.OffsetXY(980, -3730), FloorNum);
            InsertText(acadDatabase, StPt.OffsetXY(1130, -5700), "排至地下一层集水坑");

            InsertText(acadDatabase, StPt.OffsetXY(2350, -3380), "截止阀", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXY(2340, -4000), "K=80", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXY(2310, -4450), "试水接头", "W-FRPT-NOTE");
            InsertText(acadDatabase, StPt.OffsetXY(160, 200), "DN150");
            InsertText(acadDatabase, StPt.OffsetXY(740, -840), "DN50");
            InsertText(acadDatabase, StPt.OffsetXY(1040, -5250), "DN50", "W-FRPT-SPRL-DIMS", Math.PI / 2);
            InsertText(acadDatabase, StPt.OffsetXY(4060, -5250), "DN80", "W-FRPT-SPRL-DIMS", Math.PI / 2);
            InsertSolid(acadDatabase, StPt.OffsetXY(3497, -2546), StPt.OffsetXY(3320, -2646), StPt.OffsetXY(3362, -2698));
            InsertSolid(acadDatabase, StPt.OffsetXY(3615, -2927), StPt.OffsetXY(3523, -3107), StPt.OffsetXY(3469, -3068));
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
            var solid = new Solid(pt1, pt2, pt3)
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
