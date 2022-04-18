using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase
{
    public static class Case5
    {
        public static void Init(HalfBranchPipe halfBranchPipe)
        {
            double curFloorHigherHeight = halfBranchPipe.IndexStartY + halfBranchPipe.FloorNumber * halfBranchPipe.FloorHeight;
            double curFloorLowerHeight = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight;
            double halfFloorHeight = 0.5 * halfBranchPipe.FloorHeight;
            var households = halfBranchPipe.Households[halfBranchPipe.AreaIndex];
            var branchPipeX = halfBranchPipe.BranchPipeX;

            var pt1Y = curFloorLowerHeight + 100;

            var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
            var pt2 = pt1.OffsetX(branchPipeX);
            var pt231 = pt2.OffsetY(-50);

            var pt232 = pt231.OffsetY(-0.7 * halfBranchPipe.BlockSize[0][0]);//减压阀

            var pt3 = pt232.OffsetY(-50);
            halfBranchPipe.TextSite = pt1.OffsetY(-1400);
            var pt371 = pt3.OffsetX(400);
            var pt372 = pt371.OffsetX(0.7 * halfBranchPipe.BlockSize[1][0]);
            var pt373 = pt372.OffsetX(75);
            var pt374 = pt373.OffsetX(0.7 * halfBranchPipe.BlockSize[2][0]);
            var pt7 = pt374.OffsetX(417);
            var pt71 = pt7.OffsetY(-446);
            var pt72 = pt71.OffsetX(160);
            var pt11 = new Point3d(pt72.X, curFloorLowerHeight + 360, 0);
            var pt15 = pt11.OffsetX(800);
            halfBranchPipe.BranchPipes = new List<Line>();//支管列表
            halfBranchPipe.PRValveDetailSite = new Point3d(pt1.X - 5000, curFloorLowerHeight + 200, 0);
            halfBranchPipe.WaterPipeInterrupted = new List<Point3d>();//水管阻断位置列表
            halfBranchPipe.CheckValveSite = new List<Point3d>();//截止阀位置列表
            halfBranchPipe.WaterMeterSite = new List<Point3d>();//水表位置列表
            halfBranchPipe.PressureReducingValveSite = pt231;//减压阀位置
            halfBranchPipe.AutoExhaustValveSite = pt1;
            halfBranchPipe.BranchPipes.Add(new Line(pt1, pt2));
            halfBranchPipe.BranchPipes.Add(new Line(pt1, pt1.OffsetY(-200)));
            halfBranchPipe.BranchPipes.Add(new Line(pt2, pt231));
            halfBranchPipe.BranchPipes.Add(new Line(pt232, pt3));
            if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
            {
                halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                halfBranchPipe.WaterPipeInterrupted.Add(pt15);//第1个水管截断位置

                halfBranchPipe.BranchPipes.Add(new Line(pt3, pt371));
                halfBranchPipe.BranchPipes.Add(new Line(pt372, pt373));
                halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                halfBranchPipe.BranchPipes.Add(new Line(pt7, pt71));
                halfBranchPipe.BranchPipes.Add(new Line(pt71, pt72));
                halfBranchPipe.BranchPipes.Add(new Line(pt72, pt11));
                halfBranchPipe.CheckValveSite.Add(new Point3d((pt371.X + pt372.X) / 2, pt3.Y, 0));//第一个截止阀位置                      
                halfBranchPipe.WaterMeterSite.Add(new Point3d((pt373.X + pt374.X) / 2, pt3.Y, 0));//第一个水表位置

                for (int i = 1; i < households; i++)
                {
                    var pt4 = pt3.OffsetY(-250);
                    var pt481 = new Point3d(pt371.X, pt4.Y, 0);
                    var pt482 = new Point3d(pt372.X, pt4.Y, 0);
                    var pt483 = new Point3d(pt373.X, pt4.Y, 0);
                    var pt484 = new Point3d(pt374.X, pt4.Y, 0);
                    halfBranchPipe.BranchPipes.Add(new Line(pt4, pt481));
                    halfBranchPipe.BranchPipes.Add(new Line(pt482, pt483));

                    halfBranchPipe.CheckValveSite.Add(new Point3d((pt481.X + pt482.X) / 2, pt4.Y, 0));//第i个截止阀位置
                    halfBranchPipe.WaterMeterSite.Add(new Point3d((pt483.X + pt484.X) / 2, pt4.Y, 0));//第i个水表位置

                    var pt8 = new Point3d(pt7.X - 200 * i, pt4.Y, 0);
                    var pt81 = pt8.OffsetY(-396);
                    var pt82 = pt81.OffsetX(560);
                    var pt12 = new Point3d(pt82.X, pt11.Y - i * 250, 0);
                    var pt16 = pt15.OffsetY(-250 * i);

                    halfBranchPipe.BranchPipes.Add(new Line(pt4, pt481));
                    halfBranchPipe.BranchPipes.Add(new Line(pt482, pt483));
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
                var pt19201 = new Point3d(pt371.X, pt19.Y, 0);
                var pt19202 = new Point3d(pt372.X, pt19.Y, 0);
                var pt19203 = new Point3d(pt373.X, pt19.Y, 0);
                var pt19204 = new Point3d(pt374.X, pt19.Y, 0);

                var pt201 = pt19204.OffsetX(117.5);
                var pt202 = pt201.OffsetX(200);
                var pt203 = pt202.OffsetX(360);
                var pt204 = pt203.OffsetX(200);

                var pt20 = pt204.OffsetX(120);
                var pt21 = pt20.OffsetY(716);
                var pt211 = pt21.OffsetXY(155, 91);
                var pt212 = pt21.OffsetX(155);
                var pt213 = pt21.OffsetXY(155, -728);

                halfBranchPipe.BranchPipes.Add(new Line(pt3, pt19));
                halfBranchPipe.BranchPipes.Add(new Line(pt19, pt19201));
                halfBranchPipe.BranchPipes.Add(new Line(pt19202, pt19203));
                halfBranchPipe.BranchPipes.Add(new Line(pt19204, pt201));
                halfBranchPipe.BranchPipes.Add(new Line(pt202, pt203));
                halfBranchPipe.BranchPipes.Add(new Line(pt204, pt20));
                halfBranchPipe.BranchPipes.Add(new Line(pt20, pt21));

                halfBranchPipe.BranchPipes.Add(new Line(pt21, pt212));
                halfBranchPipe.BranchPipes.Add(new Line(pt211, pt213));

                halfBranchPipe.CheckValveSite.Add(new Point3d((pt19201.X + pt19202.X) / 2, pt19201.Y, 0));//第五个截止阀位置
                halfBranchPipe.WaterMeterSite.Add(new Point3d((pt19203.X + pt19204.X) / 2, pt19203.Y, 0));//第五个水表位置
                halfBranchPipe.VacuumBreakerSite = pt211;//真空破坏器位置
                halfBranchPipe.WaterTapSite = pt213;//水龙头位置
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
                double halfFloorHeight = 0.5 * halfBranchPipe.FloorHeight;
                var households = halfBranchPipe.Households[halfBranchPipe.AreaIndex];
                var branchPipeX = halfBranchPipe.BranchPipeX;

                var pt1Y = curFloorHigherHeight + 100;

                var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
                var pt2 = pt1.OffsetX(150);
                var pt231 = pt2.OffsetY(-50);

                var pt232 = pt231.OffsetY(-0.7 * halfBranchPipe.BlockSize[0][0]);//减压阀

                var pt3 = pt232.OffsetY(-50);
                halfBranchPipe.TextSite = pt1.OffsetY(-1400);
                var pt371 = pt3.OffsetX(650);
                var pt372 = pt371.OffsetX(0.7 * halfBranchPipe.BlockSize[1][0]);
                var pt373 = pt372.OffsetX(75);
                var pt374 = pt373.OffsetX(0.7 * halfBranchPipe.BlockSize[2][0]);
                var pt7 = pt374.OffsetX(417);
                var pt71 = pt7.OffsetY(-287);
                var pt72 = pt71.OffsetX(760);
                var pt11 = new Point3d(pt72.X, curFloorLowerHeight + 360, 0);
                var pt15 = pt11.OffsetX(600);
                halfBranchPipe.BranchPipes = new List<Line>();//支管列表
                halfBranchPipe.PRValveDetailSite = new Point3d(pt1.X - 5000, curFloorLowerHeight + 200, 0);
                halfBranchPipe.WaterPipeInterrupted = new List<Point3d>();//水管阻断位置列表
                halfBranchPipe.CheckValveSite = new List<Point3d>();//截止阀位置列表
                halfBranchPipe.WaterMeterSite = new List<Point3d>();//水表位置列表
                halfBranchPipe.PressureReducingValveSite = pt231;//减压阀位置
                halfBranchPipe.AutoExhaustValveSite = pt1;
                halfBranchPipe.BranchPipes.Add(new Line(pt1, pt2));
                halfBranchPipe.BranchPipes.Add(new Line(pt1, pt1.OffsetY(-200)));
                halfBranchPipe.BranchPipes.Add(new Line(pt2, pt231));
                halfBranchPipe.BranchPipes.Add(new Line(pt232, pt3));
                if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
                {
                    halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                    halfBranchPipe.WaterPipeInterrupted.Add(pt15);//第1个水管截断位置

                    halfBranchPipe.BranchPipes.Add(new Line(pt3, pt371));
                    halfBranchPipe.BranchPipes.Add(new Line(pt372, pt373));
                    halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                    halfBranchPipe.BranchPipes.Add(new Line(pt7, pt71));
                    halfBranchPipe.BranchPipes.Add(new Line(pt71, pt72));
                    halfBranchPipe.BranchPipes.Add(new Line(pt72, pt11));
                    halfBranchPipe.CheckValveSite.Add(new Point3d((pt371.X + pt372.X) / 2, pt3.Y, 0));//第一个截止阀位置                      
                    halfBranchPipe.WaterMeterSite.Add(new Point3d((pt373.X + pt374.X) / 2, pt3.Y, 0));//第一个水表位置

                    for (int i = 1; i < households; i++)
                    {
                        var pt4 = pt3.OffsetY(-200*i);
                        var pt481 = new Point3d(pt371.X, pt4.Y, 0);
                        var pt482 = new Point3d(pt372.X, pt4.Y, 0);
                        var pt483 = new Point3d(pt373.X, pt4.Y, 0);
                        var pt484 = new Point3d(pt374.X, pt4.Y, 0);
                        halfBranchPipe.BranchPipes.Add(new Line(pt4, pt481));
                        halfBranchPipe.BranchPipes.Add(new Line(pt482, pt483));

                        halfBranchPipe.CheckValveSite.Add(new Point3d((pt481.X + pt482.X) / 2, pt4.Y, 0));//第i个截止阀位置
                        halfBranchPipe.WaterMeterSite.Add(new Point3d((pt483.X + pt484.X) / 2, pt4.Y, 0));//第i个水表位置

                        var pt8 = new Point3d(pt7.X - 200 * i, pt4.Y, 0);
                        var pt81 = pt71.OffsetXY(-200*i,-200*i);
                        var pt82 = pt72.OffsetXY(-200 * i, -200 * i);
                        var pt12 = new Point3d(pt82.X, pt11.Y - i * 250, 0);
                        var pt16 = pt15.OffsetY(-250 * i);

                        halfBranchPipe.BranchPipes.Add(new Line(pt4, pt481));
                        halfBranchPipe.BranchPipes.Add(new Line(pt482, pt483));
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
                    var pt19201 = new Point3d(pt371.X, pt19.Y, 0);
                    var pt19202 = new Point3d(pt372.X, pt19.Y, 0);
                    var pt19203 = new Point3d(pt373.X, pt19.Y, 0);
                    var pt19204 = new Point3d(pt374.X, pt19.Y, 0);

                    var pt201 = pt19204.OffsetX(117.5);
                    var pt202 = pt201.OffsetX(200);
                    var pt203 = pt202.OffsetX(360);
                    var pt204 = pt203.OffsetX(200);

                    var pt20 = pt204.OffsetX(120);
                    var pt21 = pt20.OffsetY(716);
                    var pt211 = pt21.OffsetXY(155, 91);
                    var pt212 = pt21.OffsetX(155);
                    var pt213 = pt21.OffsetXY(155, -728);

                    halfBranchPipe.BranchPipes.Add(new Line(pt3, pt19));
                    halfBranchPipe.BranchPipes.Add(new Line(pt19, pt19201));
                    halfBranchPipe.BranchPipes.Add(new Line(pt19202, pt19203));
                    halfBranchPipe.BranchPipes.Add(new Line(pt19204, pt201));
                    halfBranchPipe.BranchPipes.Add(new Line(pt202, pt203));
                    halfBranchPipe.BranchPipes.Add(new Line(pt204, pt20));
                    halfBranchPipe.BranchPipes.Add(new Line(pt20, pt21));

                    halfBranchPipe.BranchPipes.Add(new Line(pt21, pt212));
                    halfBranchPipe.BranchPipes.Add(new Line(pt211, pt213));

                    halfBranchPipe.CheckValveSite.Add(new Point3d((pt19201.X + pt19202.X) / 2, pt19201.Y, 0));//第五个截止阀位置
                    halfBranchPipe.WaterMeterSite.Add(new Point3d((pt19203.X + pt19204.X) / 2, pt19203.Y, 0));//第五个水表位置
                    halfBranchPipe.VacuumBreakerSite = pt211;//真空破坏器位置
                    halfBranchPipe.WaterTapSite = pt213;//水龙头位置
                }
            }
            else
            {
                double curFloorHigherHeight = halfBranchPipe.IndexStartY + halfBranchPipe.FloorNumber * halfBranchPipe.FloorHeight;
                double curFloorLowerHeight = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight;
                double halfFloorHeight = 0.5 * halfBranchPipe.FloorHeight;
                var households = halfBranchPipe.Households[halfBranchPipe.AreaIndex];
                var branchPipeX = halfBranchPipe.BranchPipeX;

                var pt1Y = curFloorLowerHeight + 510;

                var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, pt1Y, 0);
                var pt2 = pt1.OffsetX(branchPipeX);
                var pt231 = pt2.OffsetY(-50);

                var pt232 = pt231.OffsetY(-0.7 * halfBranchPipe.BlockSize[0][0]);//减压阀

                var pt3 = pt232.OffsetY(-50);
                halfBranchPipe.TextSite = pt1.OffsetY(-1400);
                var pt371 = pt3.OffsetX(400);
                var pt372 = pt371.OffsetX(0.7 * halfBranchPipe.BlockSize[1][0]);
                var pt373 = pt372.OffsetX(75);
                var pt374 = pt373.OffsetX(0.7 * halfBranchPipe.BlockSize[2][0]);
                var pt7 = pt374.OffsetX(817);
                var pt71 = pt7.OffsetY(-287);
                var pt72 = pt71.OffsetX(160);
                var pt11 = new Point3d(pt72.X, curFloorLowerHeight + 360, 0);
                var pt15 = pt11.OffsetX(800);
                halfBranchPipe.BranchPipes = new List<Line>();//支管列表
                halfBranchPipe.PRValveDetailSite = new Point3d(pt1.X - 5000, curFloorLowerHeight + 200, 0);
                halfBranchPipe.WaterPipeInterrupted = new List<Point3d>();//水管阻断位置列表
                halfBranchPipe.CheckValveSite = new List<Point3d>();//截止阀位置列表
                halfBranchPipe.WaterMeterSite = new List<Point3d>();//水表位置列表
                halfBranchPipe.PressureReducingValveSite = pt231;//减压阀位置
                halfBranchPipe.AutoExhaustValveSite = pt1;
                halfBranchPipe.BranchPipes.Add(new Line(pt1, pt2));
                halfBranchPipe.BranchPipes.Add(new Line(pt1, pt1.OffsetY(-500)));
                halfBranchPipe.BranchPipes.Add(new Line(pt2, pt231));
                halfBranchPipe.BranchPipes.Add(new Line(pt232, pt3));
                if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
                {
                    halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                    halfBranchPipe.WaterPipeInterrupted.Add(pt15);//第1个水管截断位置

                    halfBranchPipe.BranchPipes.Add(new Line(pt3, pt371));
                    halfBranchPipe.BranchPipes.Add(new Line(pt372, pt373));
                    halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                    halfBranchPipe.BranchPipes.Add(new Line(pt7, pt71));
                    halfBranchPipe.BranchPipes.Add(new Line(pt71, pt72));
                    halfBranchPipe.BranchPipes.Add(new Line(pt72, pt11));
                    halfBranchPipe.CheckValveSite.Add(new Point3d((pt371.X + pt372.X) / 2, pt3.Y, 0));//第一个截止阀位置                      
                    halfBranchPipe.WaterMeterSite.Add(new Point3d((pt373.X + pt374.X) / 2, pt3.Y, 0));//第一个水表位置

                    for (int i = 1; i < households; i++)
                    {
                        var pt4 = pt3.OffsetY(-200*i);
                        var pt481 = new Point3d(pt371.X, pt4.Y, 0);
                        var pt482 = new Point3d(pt372.X, pt4.Y, 0);
                        var pt483 = new Point3d(pt373.X, pt4.Y, 0);
                        var pt484 = new Point3d(pt374.X, pt4.Y, 0);
                        halfBranchPipe.BranchPipes.Add(new Line(pt4, pt481));
                        halfBranchPipe.BranchPipes.Add(new Line(pt482, pt483));

                        halfBranchPipe.CheckValveSite.Add(new Point3d((pt481.X + pt482.X) / 2, pt4.Y, 0));//第i个截止阀位置
                        halfBranchPipe.WaterMeterSite.Add(new Point3d((pt483.X + pt484.X) / 2, pt4.Y, 0));//第i个水表位置

                        var pt8 = new Point3d(pt7.X - 200 * i, pt4.Y, 0);
                        var pt81 = pt71.OffsetXY(-200 * i, -200 * i);
                        var pt82 = pt72.OffsetXY(200 * i, -200 * i);
                        var pt12 = new Point3d(pt82.X, pt11.Y - i * 250, 0);
                        var pt16 = pt15.OffsetY(-250 * i);

                        halfBranchPipe.BranchPipes.Add(new Line(pt4, pt481));
                        halfBranchPipe.BranchPipes.Add(new Line(pt482, pt483));
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
                    var pt19201 = new Point3d(pt371.X, pt19.Y, 0);
                    var pt19202 = new Point3d(pt372.X, pt19.Y, 0);
                    var pt19203 = new Point3d(pt373.X, pt19.Y, 0);
                    var pt19204 = new Point3d(pt374.X, pt19.Y, 0);

                    var pt201 = pt19204.OffsetX(117.5);
                    var pt202 = pt201.OffsetX(200);
                    var pt203 = pt202.OffsetX(360);
                    var pt204 = pt203.OffsetX(200);

                    var pt20 = pt204.OffsetX(120);
                    var pt21 = pt20.OffsetY(716);
                    var pt211 = pt21.OffsetXY(155, 91);
                    var pt212 = pt21.OffsetX(155);
                    var pt213 = pt21.OffsetXY(155, -728);

                    halfBranchPipe.BranchPipes.Add(new Line(pt3, pt19));
                    halfBranchPipe.BranchPipes.Add(new Line(pt19, pt19201));
                    halfBranchPipe.BranchPipes.Add(new Line(pt19202, pt19203));
                    halfBranchPipe.BranchPipes.Add(new Line(pt19204, pt201));
                    halfBranchPipe.BranchPipes.Add(new Line(pt202, pt203));
                    halfBranchPipe.BranchPipes.Add(new Line(pt204, pt20));
                    halfBranchPipe.BranchPipes.Add(new Line(pt20, pt21));

                    halfBranchPipe.BranchPipes.Add(new Line(pt21, pt212));
                    halfBranchPipe.BranchPipes.Add(new Line(pt211, pt213));

                    halfBranchPipe.CheckValveSite.Add(new Point3d((pt19201.X + pt19202.X) / 2, pt19201.Y, 0));//第五个截止阀位置
                    halfBranchPipe.WaterMeterSite.Add(new Point3d((pt19203.X + pt19204.X) / 2, pt19203.Y, 0));//第五个水表位置
                    halfBranchPipe.VacuumBreakerSite = pt211;//真空破坏器位置
                    halfBranchPipe.WaterTapSite = pt213;//水龙头位置
                }
            }

        }

    }
}
