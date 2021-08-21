﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class ThWSuplySystemDiagram // 竖管系统类
    {
        private int Loweststorey { get; set; }//竖管最低层
        private int Higheststorey { get; set; }//竖管最高层
        private List<int> HighStoreyList { get; set; }//竖管最高层列表
        private double PipeOffset_X { get; set; } //竖管相对于楼层的偏移量
        private double PipeX { get; set; }//竖管的X坐标
        private double PipeY { get; set; }//竖管的Y坐标
        private string PipeNumber { get; set; } //竖管编号
        public List<ThWSSDPipeUnit> PipeUnits { get; set; }//PipeUnit的数组

        public ThWSuplySystemDiagram(string pipeNumber, int loweststorey, int higheststorey, double pipeOffset_X, List<int> highStoreyList)
        {
            Loweststorey = loweststorey;
            Higheststorey = higheststorey;
            PipeOffset_X = pipeOffset_X;
            PipeNumber = pipeNumber;
            HighStoreyList = highStoreyList;
            PipeUnits = new List<ThWSSDPipeUnit>();
        }

        public double GetPipeX()
        {
            return PipeX;
        }

        public List<Line> CreatePipeLine(Point3d insertPt, double FloorHeight)
        {
            var lineList = new List<Line>();
            var pt1 = insertPt.OffsetXY(PipeOffset_X, -300); //new Point3d(indexStartX + PipeOffset_X, indexStartY - 300, 0);
            var pt2 = insertPt.OffsetXY(PipeOffset_X, Higheststorey * FloorHeight - 0.175 * FloorHeight);
            if (Higheststorey == 1)
            {
                var pt121 = new Point3d(pt1.X, pt1.Y + 140 + 300, 0);
                var pt122 = new Point3d(pt1.X, pt121.Y + 420, 0);

                lineList.Add(new Line(pt1, pt121));
                lineList.Add(new Line(pt122, pt2));
            }
            else if (Higheststorey > 5)
            {
                var pt121 = new Point3d(pt1.X, pt1.Y + 2 * FloorHeight + 140 + 300, 0);
                var pt122 = new Point3d(pt1.X, pt121.Y + 420, 0);
                var pt123 = insertPt.OffsetXY(PipeOffset_X, (Higheststorey - 2) * FloorHeight + 140);
                var pt124 = new Point3d(pt1.X, pt123.Y + 420, 0);

                lineList.Add(new Line(pt1, pt121));
                lineList.Add(new Line(pt122, pt123));
                lineList.Add(new Line(pt124, pt2));
            }
            else
            {
                lineList.Add(new Line(pt1, pt2));
            }

            foreach (var line1 in lineList)
            {
                line1.LayerId = DbHelper.GetLayerId("W-WSUP-COOL-PIPE");
            }
            PipeX = pt1.X;
            PipeY = pt2.Y;

            return lineList;
        }

        public void DrawPipeLine(int i, Point3d insertPt, double FloorHeight, int PipeNums)
        {
            using AcadDatabase acadDatabase = AcadDatabase.Active();  //要插入图纸的空间
            var PipeLine = CreatePipeLine(insertPt, FloorHeight);
            foreach (var line1 in PipeLine)
            {
                acadDatabase.CurrentSpace.Add(line1);
            }

            //绘制水管中断
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterPipeInterrupted,
            new Point3d(GetPipeX(), insertPt.Y - 300, 0), new Scale3d(1, 1, 1), Math.PI * 3 / 2);

            for (int j = PipeUnits.Count - 1; j >= 0; j--)
            {
                //管径图样插入 (DN50)
                if (j != PipeUnits.Count - 1 && j != 0)
                {
                    var DNj = PipeUnits[j].GetPipeDiameter();
                    var DNjp = PipeUnits[j + 1].GetPipeDiameter();
                    var DNjn = PipeUnits[j - 1].GetPipeDiameter();
                    if (DNj.Equals(DNjp) && DNj.Equals(DNjn))
                    {
                        continue;
                    }
                }
                if (PipeUnits[j].GetPipeDiameter() != "")
                {
                    var Position = new Point3d(GetPipeX(), insertPt.Y + FloorHeight * (j + 1) - 700 - FloorHeight / 3, 0);
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-DIMS", WaterSuplyBlockNames.PipeDiameter,
                    Position, new Scale3d(1, 1, 1), Math.PI / 2);
                    if (PipeNumber.Contains("JG"))
                    {
                        objID.SetDynBlockValue("可见性", "DN15");
                    }
                    else
                    {
                        objID.SetDynBlockValue("可见性", PipeUnits[j].GetPipeDiameter());
                    }
                }
            }

            //绘制立管起点
            var ptLs = new Point3d[3];
            ptLs[0] = new Point3d(GetPipeX(), insertPt.Y - 300, 0);
            ptLs[1] = new Point3d(GetPipeX(), insertPt.Y - 1000 - 500 * i, 0);
            ptLs[2] = new Point3d(GetPipeX() + 5500 + 600 * i, ptLs[1].Y, 0);
            var polyLine = new Polyline3d(0, new Point3dCollection(ptLs), false)
            {
                LayerId = DbHelper.GetLayerId("W-WSUP-NOTE")
            };
            acadDatabase.CurrentSpace.Add(polyLine);

            var textString = "";
            if (PipeNumber.Contains("JG"))
            {
                textString = "接自市政给水管DN15(X.XXMPa)";
            }
            else
            {
                textString = "接自加压" + Convert.ToString(i) + "区生活给水管" + PipeUnits[0].GetPipeDiameter() + "(X.XXMPa)";
            }
            
            var text = ThText.PipeText(ptLs[1].OffsetXY(600 * i + 50, 50), textString);
            acadDatabase.CurrentSpace.Add(text);

            //绘制立管编号 J1L1 J2L2 J3L3       2F统一标注
            var ptPipeNumLs = new Point3d[3];
            ptPipeNumLs[0] = new Point3d(GetPipeX() - 1300 - (PipeNums - i - 1) * 600, insertPt.Y + FloorHeight + 200 + (PipeNums - i - 1) * 450, 0);
            ptPipeNumLs[1] = new Point3d(ptPipeNumLs[0].X + 1100, ptPipeNumLs[0].Y, 0);
            ptPipeNumLs[2] = new Point3d(GetPipeX(), ptPipeNumLs[0].Y - 200 - (PipeNums - i - 1) * 600, 0);
            var PipePolyLine = new Polyline3d(0, new Point3dCollection(ptPipeNumLs), false)
            {
                LayerId = DbHelper.GetLayerId("W-NOTE")
            };
            acadDatabase.CurrentSpace.Add(PipePolyLine);

            var text1 = ThText.PipeText(ptPipeNumLs[0], PipeNumber);
            acadDatabase.CurrentSpace.Add(text1);

            if (i < 0)
            {
                int j = 1;
                //绘制立管编号 J1L1 J2L2 J3L3
                var ptPipeNumLsj = new Point3d[3];
                ptPipeNumLsj[0] = new Point3d(GetPipeX() - 1300 - (PipeNums - i - 1) * 600, insertPt.Y + FloorHeight * (HighStoreyList[j] - 1) + 200 + (PipeNums - i - 1) * 450, 0);
                ptPipeNumLsj[1] = new Point3d(ptPipeNumLsj[0].X + 1100, ptPipeNumLsj[0].Y, 0);
                ptPipeNumLsj[2] = new Point3d(GetPipeX(), ptPipeNumLsj[0].Y - 200 - (PipeNums - i - 1) * 600, 0);
                var PipePolyLinej = new Polyline3d(0, new Point3dCollection(ptPipeNumLsj), false);
                PipePolyLinej.LayerId = DbHelper.GetLayerId("W-NOTE");
                acadDatabase.CurrentSpace.Add(PipePolyLinej);

                var textj = ThText.PipeText(ptPipeNumLsj[0].OffsetY(30), PipeNumber);
                acadDatabase.CurrentSpace.Add(textj);
            }

            //绘制立管简编号 J1 J2 J3
            int num = 2;
            if (PipeNumber.Contains("JGL"))
            {
                num = 1;
            }
            for (int j = 0; j < num; j++)
            {
                if (PipeLine.Count >= 2)
                {
                    var textStr = "";
                    if (PipeNumber.Length > 2)
                    {
                        textStr = PipeNumber.Substring(0, 2);
                    }
                    else
                    {
                        textStr = PipeNumber;
                    }
                    var simpleNumber1 = ThText.PipeText(PipeLine[j].EndPoint.OffsetY(-150), textStr);
                    simpleNumber1.Rotate(PipeLine[j].EndPoint, Math.PI / 2);
                    acadDatabase.CurrentSpace.Add(simpleNumber1);
                }
            }
        }
    }
}
