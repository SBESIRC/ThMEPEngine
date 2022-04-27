using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.WaterSupplyPipeSystem.Data;
using ThMEPWSS.WaterSupplyPipeSystem.model;
using ThMEPWSS.WaterSupplyPipeSystem.tool;

namespace ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase
{
    public static class Case4
    {
        public static void Init(HalfBranchPipe halfBranchPipe,double pt1Offset = 100, double pt7Offset = 417)
        {
            double curFloorLowerHeight = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight;
            double halfFloorHeight = 0.5 * halfBranchPipe.FloorHeight;
            
            double totalDist = halfBranchPipe.BranchPipeX + 400;
            var pt1Y = curFloorLowerHeight + pt1Offset;

            var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
            var pt3 = BranchPts.AddPt2Pt3(halfBranchPipe, pt1);
            var pt374 = BranchPts.Get(pt3, halfBranchPipe);
           
            if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
            {
                var pt7 = pt374.OffsetX(417);

                var pt71 = pt7.OffsetY(-446);
                var pt72 = pt71.OffsetX(160);
                var pt11 = new Point3d(pt72.X, curFloorLowerHeight + halfFloorHeight - 150, 0);
                var pt15 = pt11.OffsetX((halfBranchPipe.Households[halfBranchPipe.AreaIndex] - 1) * 200 + 285);
                halfBranchPipe.FloorPts.Add(pt15.OffsetXY(444, 444));//第一个楼梯敷设点

                var pt1501 = pt15.OffsetXY(945, 945);
                var pt1502 = pt1501.OffsetX(1065);
                var pt1503 = pt1502.OffsetY(-1513);
                halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                halfBranchPipe.BranchPipes.Add(new Line(pt15, pt1501));
                halfBranchPipe.BranchPipes.Add(new Line(pt1501, pt1502));
                halfBranchPipe.BranchPipes.Add(new Line(pt1502, pt1503));
                halfBranchPipe.WaterPipeInterrupted.Add(pt1503);//第1个水管截断位置

               
                halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                halfBranchPipe.BranchPipes.Add(new Line(pt7, pt71));
                halfBranchPipe.BranchPipes.Add(new Line(pt71, pt72));
                halfBranchPipe.BranchPipes.Add(new Line(pt72, pt11));
     

                for (int i = 1; i < halfBranchPipe.Households[halfBranchPipe.AreaIndex]; i++)
                {
                    var pt4 = pt3.OffsetY(-250);
                    var pt484 = BranchPts.Get(pt4, halfBranchPipe);
                    
                    var pt8 = new Point3d(pt7.X - 200 * i, pt4.Y, 0);
                    var pt81 = pt8.OffsetY(-396);
                    var pt82 = pt81.OffsetX(560);
                    var pt12 = new Point3d(pt82.X, pt11.Y - i * 250, 0);
                    var pt16 = pt15.OffsetY(-250 * i);
                    halfBranchPipe.FloorPts.Add(pt16.OffsetXY(444, 444));//第二个楼梯敷设点

                    var pt1601 = pt1501.OffsetY(-250);
                    var pt1602 = pt1502.OffsetXY(-250 * i, -250);
                    var pt1603 = pt1503.OffsetX(-250 * i);

                  
                    halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                    halfBranchPipe.BranchPipes.Add(new Line(pt8, pt81));
                    halfBranchPipe.BranchPipes.Add(new Line(pt81, pt82));
                    halfBranchPipe.BranchPipes.Add(new Line(pt82, pt12));
                    halfBranchPipe.BranchPipes.Add(new Line(pt12, pt16));
                    halfBranchPipe.BranchPipes.Add(new Line(pt16, pt1601));
                    halfBranchPipe.BranchPipes.Add(new Line(pt1601, pt1602));
                    halfBranchPipe.BranchPipes.Add(new Line(pt1602, pt1603));

                    halfBranchPipe.WaterPipeInterrupted.Add(pt1603);//第i个水管截断位置


                    if (i == halfBranchPipe.Households[halfBranchPipe.AreaIndex] - 1)
                    {
                        halfBranchPipe.BranchPipes.Add(new Line(pt3, pt4));
                    }
                }
            }

            if (halfBranchPipe.HasFlushFaucet) //有冲洗龙头
            {
                double pt19Y = pt3.Y - 250 * halfBranchPipe.Households[halfBranchPipe.AreaIndex];

                var pt19 = new Point3d(pt3.X, pt19Y, 0);
                var pt19204 = BranchPts.Get(pt19, halfBranchPipe);

                var pt201 = pt19204.OffsetX(117.5);
                var pt202 = pt201.OffsetX(200);
                var pt203 = pt202.OffsetX(360);
                var pt204 = pt203.OffsetX(200);

                var pt20 = pt204.OffsetX(120);
                halfBranchPipe.BranchPipes.Add(new Line(pt3, pt19));
                
                halfBranchPipe.BranchPipes.Add(new Line(pt19204, pt20));

                Case1.CreateFlushFaucet(pt20, halfBranchPipe);
            }
        }


        /// <summary>
        /// 一层特殊处理
        /// </summary>
        /// <param name="halfBranchPipe"></param>
        /// <param name="firstFloorMeterLocation"></param>
        /// <param name="firstFloor"></param>
        public static void Init1Floor(HalfBranchPipe halfBranchPipe, string firstFloorMeterLocation, bool firstFloor)
        {
            if (firstFloorMeterLocation.Equals("0"))//半平台
            {
                InitHalfPlatform(halfBranchPipe, firstFloor);
            }
            else//大堂
            {
                if (firstFloor)
                {
                    Case1.InitLobby(halfBranchPipe);
                }
                else
                {
                    Init(halfBranchPipe);
                }
            }
        }

        /// <summary>
        /// 一层水表在半平台
        /// </summary>
        /// <param name="halfBranchPipe"></param>
        /// <param name="firstFloor"></param>
        public static void InitHalfPlatform(HalfBranchPipe halfBranchPipe, bool firstFloor)
        {
            double curFloorUpperHeight = halfBranchPipe.IndexStartY + halfBranchPipe.FloorNumber * halfBranchPipe.FloorHeight;
            double curFloorLowerHeight = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight;
           
            var households = halfBranchPipe.Households[halfBranchPipe.AreaIndex];
           
            if (firstFloor)//按照case12
            {
                double totalDist = 800;
                var pt1Y = curFloorUpperHeight + 100;
                double pt1Pt2Dist = 165;
                var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
                var pt3 = BranchPts.AddPt2Pt3(halfBranchPipe, pt1, pt1Pt2Dist);
                var pt374 = BranchPts.Get(pt3, halfBranchPipe, totalDist - pt1Pt2Dist);
                Point3d pt7;
                Point3d pt11;
                
                if (households != 0)
                {
                    pt7 = pt374.OffsetX(378);
                    var pt71 = pt7.OffsetY(-422);
                    var pt72 = pt71.OffsetX(1520);
                    pt11 = new Point3d(pt72.X, halfBranchPipe.IndexStartY + halfBranchPipe.FloorNumber * halfBranchPipe.FloorHeight + 790, 0);
                    var pt15 = pt11.OffsetX((households - 1) * 200 + 300);


                    var pt1501 = pt15.OffsetXY(930, -930);
                    var pt1502 = pt1501.OffsetX(430);
                    var pt1503 = new Point3d(pt1502.X, curFloorLowerHeight + 100 + (households - 1) * 200, 0);
                    var pt1504 = pt1503.OffsetX(200);
                    halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                    halfBranchPipe.BranchPipes.Add(new Line(pt15, pt1501));
                    halfBranchPipe.BranchPipes.Add(new Line(pt1501, pt1502));
                    halfBranchPipe.BranchPipes.Add(new Line(pt1502, pt1503));
                    halfBranchPipe.BranchPipes.Add(new Line(pt1503, pt1504));
                    halfBranchPipe.WaterPipeInterrupted.Add(pt1504);//第1个水管截断位置

                  
                    halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                    halfBranchPipe.BranchPipes.Add(new Line(pt7, pt71));
                    halfBranchPipe.BranchPipes.Add(new Line(pt71, pt72));
                    halfBranchPipe.BranchPipes.Add(new Line(pt72, pt11));
                   
                    for (int i = 1; i < households; i++)
                    {
                        var pt4 = pt3.OffsetY(-250 * i);
                        var pt484 = BranchPts.Get(pt4, halfBranchPipe, totalDist - pt1Pt2Dist);
                       

                        var pt8 = new Point3d(pt7.X - 200 * i, pt4.Y, 0);
                        var pt81 = pt8.OffsetY(-372);
                        var pt82 = pt81.OffsetX(1940);
                        var pt12 = pt11.OffsetXY(200 * i, -250 * i);
                        var pt16 = pt15.OffsetY(-250 * i);
                        var pt1601 = pt1501.OffsetXY(-50 * i, -200 * i);
                        var pt1602 = pt1502.OffsetXY(-200 * i, -200 * i);
                        var pt1603 = pt1503.OffsetXY(-200 * i, -200 * i);
                        var pt1604 = pt1504.OffsetY(-200 * i);

                       
                        halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                        halfBranchPipe.BranchPipes.Add(new Line(pt8, pt81));
                        halfBranchPipe.BranchPipes.Add(new Line(pt81, pt82));
                        halfBranchPipe.BranchPipes.Add(new Line(pt82, pt12));
                        halfBranchPipe.BranchPipes.Add(new Line(pt12, pt16));
                        halfBranchPipe.BranchPipes.Add(new Line(pt16, pt1601));
                        halfBranchPipe.BranchPipes.Add(new Line(pt1601, pt1602));
                        halfBranchPipe.BranchPipes.Add(new Line(pt1602, pt1603));
                        halfBranchPipe.BranchPipes.Add(new Line(pt1603, pt1604));

                        halfBranchPipe.WaterPipeInterrupted.Add(pt1604);//第i个水管截断位置


                        if (i == households - 1)
                        {
                            halfBranchPipe.BranchPipes.Add(new Line(pt3, pt4));
                        }
                    }
                }

                if (halfBranchPipe.HasFlushFaucet) //有冲洗龙头
                {
                    double pt19Y = pt3.Y - 250 * households;

                    var pt19 = new Point3d(pt3.X, pt19Y, 0);
                    var pt19204 = BranchPts.Get(pt19, halfBranchPipe, totalDist - pt1Pt2Dist);

                    var pt201 = pt19204.OffsetX(117.5);
                    var pt202 = pt201.OffsetX(200);
                    var pt203 = pt202.OffsetX(360);
                    var pt204 = pt203.OffsetX(200);

                    var pt20 = pt204.OffsetX(120);
                    halfBranchPipe.BranchPipes.Add(new Line(pt3, pt19));
                    
                    halfBranchPipe.BranchPipes.Add(new Line(pt19204, pt20));

                  
                    Case1.CreateFlushFaucet(pt20, halfBranchPipe);
                }
            }
            else//第二层
            {
                Init(halfBranchPipe,600,777);
            }

        }

        
    }
}
