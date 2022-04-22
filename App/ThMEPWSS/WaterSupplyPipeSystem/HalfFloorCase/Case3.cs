﻿using Autodesk.AutoCAD.DatabaseServices;
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
    public static class Case3
    {
        public static void Init(HalfBranchPipe halfBranchPipe)
        {
            double curFloorLowerHeight = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight;
            var households = halfBranchPipe.Households[halfBranchPipe.AreaIndex];
         
            
            var pt1Y = curFloorLowerHeight + 100;

            var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
            var pt3 = BranchPts.AddPt2Pt3(halfBranchPipe, pt1);
            var pt374 = BranchPts.Get(pt3, halfBranchPipe);
           
            
            if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
            {
                var pt7 = pt374.OffsetX(275);
                var pt11 = new Point3d(pt7.X, curFloorLowerHeight + 790, 0);
                var pt15 = pt11.OffsetX((households - 1) * 200 + 300);

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
                halfBranchPipe.BranchPipes.Add(new Line(pt7, pt11));
               
                for (int i = 1; i < households; i++)
                {
                    var pt4 = pt3.OffsetY(-250);
                    var pt484 = BranchPts.Get(pt4, halfBranchPipe);

                    var pt8 = new Point3d(pt7.X + 200 * i, pt4.Y, 0);
                    var pt12 = new Point3d(pt8.X, pt11.Y - i * 250, 0);
                    var pt16 = pt15.OffsetY(-250 * i);
                    halfBranchPipe.FloorPts.Add(pt16.OffsetXY(444, 444));//第二个楼梯敷设点

                    var pt1601 = pt1501.OffsetY(-250);
                    var pt1602 = pt1502.OffsetXY(-250*i,-250);
                    var pt1603 = pt1503.OffsetX(-250*i);

                   
                    halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                    halfBranchPipe.BranchPipes.Add(new Line(pt8, pt12));
                    halfBranchPipe.BranchPipes.Add(new Line(pt12, pt16));
                    halfBranchPipe.BranchPipes.Add(new Line(pt16, pt1601));
                    halfBranchPipe.BranchPipes.Add(new Line(pt1601, pt1602));
                    halfBranchPipe.BranchPipes.Add(new Line(pt1602, pt1603));

                    halfBranchPipe.WaterPipeInterrupted.Add(pt1603);//第i个水管截断位置


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
                var pt19204 = BranchPts.Get(pt19, halfBranchPipe);

                var pt20 = new Point3d(pt374.X + 275 + households * 200, pt19.Y, 0);
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
            if (firstFloor)
            {
                var pt1Y = curFloorLowerHeight + 870;

                var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
                double pt1Pt2Dist = 150;
                double totalDist = 800;
                var pt3 = BranchPts.AddPt2Pt3(halfBranchPipe, pt1, pt1Pt2Dist);
                var pt374 = BranchPts.Get(pt3, halfBranchPipe, totalDist - pt1Pt2Dist);
                
                var pt7 = pt374.OffsetX(695);

                if (households != 0)
                {
                    var pt11 = new Point3d(pt7.X, curFloorUpperHeight - 115, 0);
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
                    halfBranchPipe.BranchPipes.Add(new Line(pt7, pt11));
      

                    for (int i = 1; i < households; i++)
                    {
                        var pt4 = pt3.OffsetY(-250);
                        var pt484 = BranchPts.Get(pt4, halfBranchPipe, totalDist - pt1Pt2Dist);
                       
                        var pt8 = new Point3d(pt7.X + 200 * i, pt4.Y, 0);
                        var pt12 = new Point3d(pt8.X, pt11.Y - i * 250, 0);
                        var pt16 = pt15.OffsetY(-250 * i);
                        var pt1601 = pt1501.OffsetXY(-50 * i, -200 * i);
                        var pt1602 = pt1502.OffsetXY(-200 * i, -200 * i);
                        var pt1603 = pt1503.OffsetXY(-200 * i, -200 * i);
                        var pt1604 = pt1504.OffsetY(-200 * i);

                        halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                        halfBranchPipe.BranchPipes.Add(new Line(pt8, pt12));
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

                    var pt20 = new Point3d(pt7.X + households * 200, pt19.Y, 0);
                    halfBranchPipe.BranchPipes.Add(new Line(pt3, pt19));
                    
                    halfBranchPipe.BranchPipes.Add(new Line(pt19204, pt20));

                    
                    Case1.CreateFlushFaucet(pt20, halfBranchPipe);
                }
            }
            else//第二层
            {
                Init(halfBranchPipe);
            }

        }

    }
}
