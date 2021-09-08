using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class ThWSSDStorey  //楼层类  Th Water Suply System Diagram Storey
    {
        private int FloorNumber { get; set; }//楼层号
        private double FloorHeight { get; set; }//楼层线间距
        private bool HasFlushFaucet { get; set; }//有冲洗龙头
        private bool NoPRValve { get; set; }//无减压阀
        private int[] Households { get; set; }//每层的住户数

        public ThWSSDStorey(int floorNumber, double floorHeight, bool hasFlushFaucet, bool noPRValve, int[] households)
        {
            FloorNumber = floorNumber;
            FloorHeight = floorHeight;
            HasFlushFaucet = hasFlushFaucet;
            NoPRValve = noPRValve;
            Households = households;
        }
        public int GetFloorNumber()
        {
            return FloorNumber;
        }
        public double GetFloorHeight()
        {
            return FloorHeight;
        }
        public bool GetFlushFaucet()
        {
            return HasFlushFaucet;
        }
        public bool GetPRValve()
        {
            return NoPRValve;
        }
        public int[] GetHouseholds()
        {
            return Households;
        }

        //绘制楼层线
        public Line CreateLine(Point3d insertPt, double floorLength)
        {
            var pt1 = insertPt.OffsetY((FloorNumber - 1) * FloorHeight);
            var pt2 = insertPt.OffsetXY(floorLength, (FloorNumber - 1) * FloorHeight);
            var line1 = new Line(pt1, pt2);

            return line1;
        }

        public void DrawStorey(int i, int floorNums, Point3d insertPt, double floorLength)
        {
            using AcadDatabase acadDatabase = AcadDatabase.Active();  //要插入图纸的空间
            var line1 = CreateLine(insertPt, floorLength);
            line1.LayerId = DbHelper.GetLayerId("W-NOTE");
            line1.ColorIndex = (int)ColorIndex.BYLAYER;
            acadDatabase.CurrentSpace.Add(line1);
            var textFirst = new DBText();
            if(i < floorNums)
            {
                textFirst = ThText.NoteText(insertPt.OffsetXY(1500, i * FloorHeight + 100), Convert.ToString(i + 1) + "F");
            }
            else
            {
                textFirst = ThText.NoteText(insertPt.OffsetXY(1500, i * FloorHeight + 100), "RF");
            }
            textFirst.ColorIndex = (int)ColorIndex.BYLAYER;
            acadDatabase.CurrentSpace.Add(textFirst);
            var attNameValues = new Dictionary<string, string>() { { "标高", "X.XX" } };
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                insertPt.OffsetY(i * FloorHeight), new Scale3d(1, 1, 1), 0, attNameValues);
        }
    }
}
