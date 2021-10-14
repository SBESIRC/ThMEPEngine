﻿using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Service;
using DotNetARX;
using System;

namespace ThMEPWSS.UndergroundSpraySystem.Block
{
    public class AlarmValveSys
    {
        private Point3d StPt { get; set; }
        private double PipeLength { get; set; }
        public Point3d EndPt { get; set; }
        public AlarmValveSys(Point3d stPt, int alarmValveIndex, double floorHeight)
        {
            StPt = stPt;
            PipeLength = floorHeight * (0.455 - 0.06 * alarmValveIndex);
            EndPt = StPt.OffsetY(1550 + PipeLength);
            ;
        }
        public void Insert(AcadDatabase acadDatabase)
        {
            BlocksImport.ImportElementsFromStdDwg();

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
            InsertBlock(acadDatabase, "湿式报警阀系统", StPt.OffsetY(550));
            InsertBlock(acadDatabase, "遥控信号阀", StPt.OffsetY(1250));
            InsertBlock(acadDatabase, "截止阀", StPt.OffsetXY(300, 550));
            InsertBlock(acadDatabase, "水力警铃", StPt.OffsetXY(-300, 1300));
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

        private void InsertBlock(AcadDatabase acadDatabase, string blockName, Point3d pt, string layer = "W-FRPT-SPRL-EQPM", double scaled = 1, double rotation = Math.PI/2)
        {
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference(layer, blockName, pt, new Scale3d(scaled, scaled, scaled), rotation);
        }
    }
}
