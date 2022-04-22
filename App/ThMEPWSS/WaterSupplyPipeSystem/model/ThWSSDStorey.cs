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
        public Line CreateLine(Point3d insertPt, double floorLength)
        {
            var pt1 = insertPt.OffsetY((FloorNumber - 1) * FloorHeight);
            var pt2 = insertPt.OffsetXY(floorLength, (FloorNumber - 1) * FloorHeight);
            var line = new Line(pt1, pt2);
            line.LayerId = DbHelper.GetLayerId("W-NOTE");
            line.ColorIndex = (int)ColorIndex.BYLAYER;
            return line;
        }

        public List<Line> CreateHalfFloorLine(int floorNums, Point3d insertPt, double floorLength, List<int> highestStorey, Double[] PipeOffsetX)
        {
            var pt1 = insertPt.OffsetY((FloorNumber - 1) * FloorHeight);//楼板线左边点
            var pt2 = insertPt.OffsetXY(floorLength, (FloorNumber - 1) * FloorHeight);//楼板线右边点
            var pt15 = pt1.OffsetY(FloorHeight * 0.5);//半楼层左边点
            var pt25 = pt2.OffsetY(FloorHeight * 0.5);//半楼层右边点
            var ls = new List<Line>();

            ls.Add(new Line(pt1.OffsetX(15000), pt1.OffsetX(17400)));

            if (FloorNumber <= floorNums)
            {
                ls.Add(new Line(pt15, pt25));
            }
            return ls;
        }

        public void DrawStorey(int i, SysIn sysIn)
        {
            var insertPt = sysIn.InsertPt;
            var floorLength = sysIn.FloorLength;
            var floorNums = sysIn.FloorNumbers;
            var floorHeightDic = sysIn.FloorHeightDic;
            using AcadDatabase acadDatabase = AcadDatabase.Active();  //要插入图纸的空间
            {
                var line = CreateLine(insertPt, floorLength);
                acadDatabase.CurrentSpace.Add(line);

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
                textFirst = ThText.NoteText(insertPt.OffsetXY(16500, i * FloorHeight + 100), Convert.ToString(i + 1) + "F");
            }
            else
            {
                textFirst = ThText.NoteText(insertPt.OffsetXY(16500, i * FloorHeight + 100), "RF");
            }
            textFirst.ColorIndex = (int)ColorIndex.BYLAYER;
            acadDatabase.CurrentSpace.Add(textFirst);
            string height = "X.XX";
            string height2 = "X.XX";
            double heightInt = 0.0;
            if (floorHeightDic.ContainsKey(Convert.ToString(i + 1)))
            {
                height = floorHeightDic[Convert.ToString(i + 1)];
                if (i == 0)
                {
                    heightInt = 0;
                }
                else
                {
                    heightInt = Convert.ToDouble(height);
                }
            }
            if (floorHeightDic.ContainsKey(Convert.ToString(i + 2)))
            {
                height2 = floorHeightDic[Convert.ToString(i + 2)];
                var height2Int = Convert.ToDouble(height2);
                height2 = Convert.ToString((height2Int + heightInt) / 2.0);
            }

            var attNameValues = new Dictionary<string, string>() { { "标高", height } };
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                insertPt.OffsetXY(15000, i * FloorHeight), new Scale3d(1, 1, 1), 0, attNameValues);
            if (i < floorNums)
            {
                var attNameValues2 = new Dictionary<string, string>() { { "标高", height2 } };
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                    insertPt.OffsetY((i + 0.5) * FloorHeight), new Scale3d(1, 1, 1), 0, attNameValues2);
            }
        }
    }
}
