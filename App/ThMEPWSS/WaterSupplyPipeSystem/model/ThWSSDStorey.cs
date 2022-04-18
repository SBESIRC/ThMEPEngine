using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.WaterSupplyPipeSystem.Data;

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
        public List<Line> CreateLine(Point3d insertPt, double floorLength, List<int> highestStorey, Double[] PipeOffsetX)
        {
            var pt1 = insertPt.OffsetY((FloorNumber - 1) * FloorHeight);
            var pt2 = insertPt.OffsetXY(floorLength, (FloorNumber - 1) * FloorHeight);
            if (highestStorey.Contains(FloorNumber - 1))
            {
                var pt11 = new Point3d(PipeOffsetX[FloorNumber - 2] - 150, pt1.Y, 0);
                var pt22 = new Point3d(PipeOffsetX[FloorNumber - 2] + 150, pt1.Y, 0);
                var pt12 = pt11.OffsetY(100);
                var pt21 = pt22.OffsetY(100);
                var ls = new List<Line>();
                ls.Add(new Line(pt1, pt11));
                ls.Add(new Line(pt11, pt12));
                ls.Add(new Line(pt12, pt21));
                ls.Add(new Line(pt21, pt22));
                ls.Add(new Line(pt22, pt2));

                return ls;
            }
            else
            {
                var line1 = new Line(pt1, pt2);
                return new List<Line>() { line1 };
            }
        }

        public List<Line> CreateHalfFloorLine(int floorNums, Point3d insertPt, double floorLength, List<int> highestStorey, Double[] PipeOffsetX)
        {
            var pt1 = insertPt.OffsetY((FloorNumber - 1) * FloorHeight);//楼板线左边点
            var pt2 = insertPt.OffsetXY(floorLength, (FloorNumber - 1) * FloorHeight);//楼板线右边点
            var pt15 = pt1.OffsetY(FloorHeight * 0.5);//半楼层左边点
            var pt25 = pt2.OffsetY(FloorHeight * 0.5);//半楼层右边点
            var ls = new List<Line>();

            //if (highestStorey.Contains(FloorNumber - 1))
            //{
            //    var pt11 = new Point3d(PipeOffsetX[FloorNumber - 2] - 150, pt1.Y, 0);
            //    var pt22 = new Point3d(PipeOffsetX[FloorNumber - 2] + 150, pt1.Y, 0);
                
            //    var pt12 = pt11.OffsetY(100);
            //    var pt21 = pt22.OffsetY(100);
            //    ls.Add(new Line(pt1, pt11));
            //    ls.Add(new Line(pt11, pt12));
            //    ls.Add(new Line(pt12, pt21));
            //    ls.Add(new Line(pt21, pt22));
            //    ls.Add(new Line(pt22, pt2));
            //}
            //else
            //{
                ls.Add(new Line(pt1, pt2));
            //}
            if(FloorNumber <= floorNums)
            {
                ls.Add(new Line(pt15, pt25));
            }
            return ls;
        }


        public void DrawStorey(int i, SysIn sysIn, SysProcess sysProcess)
        {
            var insertPt = sysIn.InsertPt;
            var floorLength = sysIn.FloorLength;
            var highestStorey = sysIn.HighestStorey;
            var PipeOffsetX = sysProcess.PipeOffsetX;
            var floorNums = sysIn.FloorNumbers;
            var floorHeightDic = sysIn.FloorHeightDic;
            using AcadDatabase acadDatabase = AcadDatabase.Active();  //要插入图纸的空间
            var lines = CreateLine(insertPt, floorLength, highestStorey, PipeOffsetX);
            foreach (var line1 in lines)
            {
                line1.LayerId = DbHelper.GetLayerId("W-NOTE");
                line1.ColorIndex = (int)ColorIndex.BYLAYER;
                acadDatabase.CurrentSpace.Add(line1);
            }

            var textFirst = new DBText();
            if (i < floorNums)
            {
                textFirst = ThText.NoteText(insertPt.OffsetXY(1500, i * FloorHeight + 100), Convert.ToString(i + 1) + "F");
            }
            else
            {
                textFirst = ThText.NoteText(insertPt.OffsetXY(1500, i * FloorHeight + 100), "RF");
            }
            textFirst.ColorIndex = (int)ColorIndex.BYLAYER;
            acadDatabase.CurrentSpace.Add(textFirst);
            string height = "X.XX";
            if (floorHeightDic.ContainsKey(Convert.ToString(i + 1)))
            {
                height = floorHeightDic[Convert.ToString(i + 1)];
            }

            var attNameValues = new Dictionary<string, string>() { { "标高", height } };
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                insertPt.OffsetY(i * FloorHeight), new Scale3d(1, 1, 1), 0, attNameValues);
        }

        public void DrawHalfFloorStorey(int i, SysIn sysIn, SysProcess sysProcess)
        {
            var insertPt = sysIn.InsertPt;
            var floorLength = sysIn.FloorLength;
            var highestStorey = sysIn.HighestStorey;
            var PipeOffsetX = sysProcess.PipeOffsetX;
            var floorNums = sysIn.FloorNumbers;
            var floorHeightDic = sysIn.FloorHeightDic;
            using AcadDatabase acadDatabase = AcadDatabase.Active();  //要插入图纸的空间
            var lines = CreateHalfFloorLine(floorNums, insertPt, floorLength, highestStorey, PipeOffsetX);
            foreach (var line1 in lines)
            {
                line1.LayerId = DbHelper.GetLayerId("W-NOTE");
                line1.ColorIndex = (int)ColorIndex.BYLAYER;
                acadDatabase.CurrentSpace.Add(line1);
            }

            var textFirst = new DBText();
            if (i < floorNums)
            {
                textFirst = ThText.NoteText(insertPt.OffsetXY(1500, i * FloorHeight + 100), Convert.ToString(i + 1) + "F");
            }
            else
            {
                textFirst = ThText.NoteText(insertPt.OffsetXY(1500, i * FloorHeight + 100), "RF");
            }
            textFirst.ColorIndex = (int)ColorIndex.BYLAYER;
            acadDatabase.CurrentSpace.Add(textFirst);
            string height = "X.XX";
            if (floorHeightDic.ContainsKey(Convert.ToString(i + 1)))
            {
                height = floorHeightDic[Convert.ToString(i + 1)];
            }

            var attNameValues = new Dictionary<string, string>() { { "标高", height } };
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                insertPt.OffsetY(i * FloorHeight), new Scale3d(1, 1, 1), 0, attNameValues);
        }
        public void DrawStorey(int i, int floorNums, Point3d insertPt, double floorLength, List<int> highestStorey,
            Double[] PipeOffsetX, Dictionary<string , string> floorHeightDic)
        {
            using AcadDatabase acadDatabase = AcadDatabase.Active();  //要插入图纸的空间
            var lines = CreateLine(insertPt, floorLength, highestStorey, PipeOffsetX);
            foreach(var line1 in lines)
            {
                line1.LayerId = DbHelper.GetLayerId("W-NOTE");
                line1.ColorIndex = (int)ColorIndex.BYLAYER;
                acadDatabase.CurrentSpace.Add(line1);
            }
            
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
            string height = "X.XX";
            if(floorHeightDic.ContainsKey(Convert.ToString(i+1)))
            {
                height = floorHeightDic[Convert.ToString(i+1)];
            }
            
            var attNameValues = new Dictionary<string, string>() { { "标高", height } };
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                insertPt.OffsetY(i * FloorHeight), new Scale3d(1, 1, 1), 0, attNameValues);
        }
    }
}
