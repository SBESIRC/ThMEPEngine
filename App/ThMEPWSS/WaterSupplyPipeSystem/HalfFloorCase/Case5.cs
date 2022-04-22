using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.WaterSupplyPipeSystem.tool;

namespace ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase
{
    public static class Case5
    {
        public static void Init(HalfBranchPipe halfBranchPipe)
        {
            double curFloorLowerHeight = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight;
            var households = halfBranchPipe.Households[halfBranchPipe.AreaIndex];

            var pt1Y = curFloorLowerHeight + 100;
            var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
            var pt3 = BranchPts.AddPt2Pt3(halfBranchPipe, pt1);
            var pt374 = BranchPts.Get(pt3, halfBranchPipe);

            var pt7 = pt374.OffsetX(417);
            var pt71 = pt7.OffsetY(-446);
            var pt72 = pt71.OffsetX(160);
            var pt11 = new Point3d(pt72.X, curFloorLowerHeight + 360, 0);
            var pt15 = pt11.OffsetX(800);
            
            if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
            {
                halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                halfBranchPipe.WaterPipeInterrupted.Add(pt15);//第1个水管截断位置

               
                halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                halfBranchPipe.BranchPipes.Add(new Line(pt7, pt71));
                halfBranchPipe.BranchPipes.Add(new Line(pt71, pt72));
                halfBranchPipe.BranchPipes.Add(new Line(pt72, pt11));
               
                for (int i = 1; i < households; i++)
                {
                    var pt4 = pt3.OffsetY(-250);
                    var pt484 = BranchPts.Get(pt4, halfBranchPipe);
                    

                    var pt8 = new Point3d(pt7.X - 200 * i, pt4.Y, 0);
                    var pt81 = pt8.OffsetY(-396);
                    var pt82 = pt81.OffsetX(560);
                    var pt12 = new Point3d(pt82.X, pt11.Y - i * 250, 0);
                    var pt16 = pt15.OffsetY(-250 * i);

                    
                    halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                    halfBranchPipe.BranchPipes.Add(new Line(pt8, pt81));
                    halfBranchPipe.BranchPipes.Add(new Line(pt81, pt82));
                    halfBranchPipe.BranchPipes.Add(new Line(pt82, pt12));
                    halfBranchPipe.BranchPipes.Add(new Line(pt12, pt16));
                    
                    halfBranchPipe.WaterPipeInterrupted.Add(pt16);//第i个水管截断位置


                    if (i == halfBranchPipe.Households[halfBranchPipe.AreaIndex] - 1)
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
            if (firstFloor)
            {
                double curFloorHigherHeight = halfBranchPipe.IndexStartY + halfBranchPipe.FloorNumber * halfBranchPipe.FloorHeight;
                double curFloorLowerHeight = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight;
                var households = halfBranchPipe.Households[halfBranchPipe.AreaIndex];
                double pt1Pt2Dist = 150;
                double totalDist = 800;
                var pt1Y = curFloorHigherHeight + 100;

                var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
                var pt3 = BranchPts.AddPt2Pt3(halfBranchPipe, pt1, pt1Pt2Dist);
                var pt374 = BranchPts.Get(pt3, halfBranchPipe, totalDist - pt1Pt2Dist);
                var pt7 = pt374.OffsetX(417);
                var pt71 = pt7.OffsetY(-287);
                var pt72 = pt71.OffsetX(760);
                var pt11 = new Point3d(pt72.X, curFloorLowerHeight + 360, 0);
                var pt15 = pt11.OffsetX(600);
                
                if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
                {
                    halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                    halfBranchPipe.WaterPipeInterrupted.Add(pt15);//第1个水管截断位置

                   
                    halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                    halfBranchPipe.BranchPipes.Add(new Line(pt7, pt71));
                    halfBranchPipe.BranchPipes.Add(new Line(pt71, pt72));
                    halfBranchPipe.BranchPipes.Add(new Line(pt72, pt11));
                    
                    for (int i = 1; i < households; i++)
                    {
                        var pt4 = pt3.OffsetY(-200*i);
                        var pt484 = BranchPts.Get(pt4, halfBranchPipe, totalDist - pt1Pt2Dist);

                      

                        var pt8 = new Point3d(pt7.X - 200 * i, pt4.Y, 0);
                        var pt81 = pt71.OffsetXY(-200*i,-200*i);
                        var pt82 = pt72.OffsetXY(-200 * i, -200 * i);
                        var pt12 = new Point3d(pt82.X, pt11.Y - i * 250, 0);
                        var pt16 = pt15.OffsetY(-250 * i);

                        halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                        halfBranchPipe.BranchPipes.Add(new Line(pt8, pt81));
                        halfBranchPipe.BranchPipes.Add(new Line(pt81, pt82));
                        halfBranchPipe.BranchPipes.Add(new Line(pt82, pt12));
                        halfBranchPipe.BranchPipes.Add(new Line(pt12, pt16));

                        halfBranchPipe.WaterPipeInterrupted.Add(pt16);//第i个水管截断位置


                        if (i == halfBranchPipe.Households[halfBranchPipe.AreaIndex] - 1)
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
            else
            {
                double curFloorLowerHeight = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight;
                var households = halfBranchPipe.Households[halfBranchPipe.AreaIndex];
                var branchPipeX = halfBranchPipe.BranchPipeX;

                var pt1Y = curFloorLowerHeight + 510;

                var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
                var pt3 = BranchPts.AddPt2Pt3(halfBranchPipe, pt1);
                var pt374 = BranchPts.Get(pt3, halfBranchPipe);

                var pt7 = pt374.OffsetX(817);
                var pt71 = pt7.OffsetY(-287);
                var pt72 = pt71.OffsetX(160);
                var pt11 = new Point3d(pt72.X, curFloorLowerHeight + 360, 0);
                var pt15 = pt11.OffsetX(800);
                
                if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
                {
                    halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                    halfBranchPipe.WaterPipeInterrupted.Add(pt15);//第1个水管截断位置

                   
                    halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                    halfBranchPipe.BranchPipes.Add(new Line(pt7, pt71));
                    halfBranchPipe.BranchPipes.Add(new Line(pt71, pt72));
                    halfBranchPipe.BranchPipes.Add(new Line(pt72, pt11));
                   
                    for (int i = 1; i < households; i++)
                    {
                        var pt4 = pt3.OffsetY(-200*i);
                        var pt484 = BranchPts.Get(pt4, halfBranchPipe);
                       

                        var pt8 = new Point3d(pt7.X - 200 * i, pt4.Y, 0);
                        var pt81 = pt71.OffsetXY(-200 * i, -200 * i);
                        var pt82 = pt72.OffsetXY(200 * i, -200 * i);
                        var pt12 = new Point3d(pt82.X, pt11.Y - i * 250, 0);
                        var pt16 = pt15.OffsetY(-250 * i);

                   
                        halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                        halfBranchPipe.BranchPipes.Add(new Line(pt8, pt81));
                        halfBranchPipe.BranchPipes.Add(new Line(pt81, pt82));
                        halfBranchPipe.BranchPipes.Add(new Line(pt82, pt12));
                        halfBranchPipe.BranchPipes.Add(new Line(pt12, pt16));

                        halfBranchPipe.WaterPipeInterrupted.Add(pt16);//第i个水管截断位置


                        if (i == halfBranchPipe.Households[halfBranchPipe.AreaIndex] - 1)
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

        }

    }
}
