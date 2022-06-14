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
using ThMEPWSS.WaterSupplyPipeSystem.tool;

namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class ThWSSDBranchPipe   //给用户供水的支管类
    {
        private int FloorNumber { get; set; }  //楼层号
        private string DN { get; set; }  //管径号

        public List<Line> BranchPipes;//PipeUnit的数组
        private bool HasFlushFaucet { get; set; } //有冲洗龙头
        private bool NoValve { get; set; }//无减压阀
        private int[] Households { get; set; }//住户数
        public Point3d PressureReducingValveSite { get; set; } //减压阀位置
        private List<Point3d> CheckValveSite { get; set; } //截止阀位置
        public List<Point3d> PRValveSite { get; set; }//减压阀位置
        private List<Point3d> WaterMeterSite { get; set; } //水表位置
        private List<Point3d> WaterPipeInterrupted { get; set; } //水管中断位置
        private Point3d AutoExhaustValveSite { get; set; } //自动排气阀位置
        private Point3d VacuumBreakerSite { get; set; } //真空破坏器位置
        private Point3d WaterTapSite { get; set; } //水龙头位置
        private Point3d PRValveDetailSite { get; set; } //减压阀详图位置
        private List<double[]> BlockSize { get; set; } //模型尺寸
        private int LayingMethod { get; set; } //敷设方式
        private Point3d TextSite { get; set; }//文字位置
        private double FloorHeight { get; set; }//楼层高
        private double PipeOffsetX { get; set; }//立管的 X 偏移量
        private double IndexStartY { get; set; }//起始 Y 偏移量
        private int AreaIndex { get; set; }//分区索引
        public double AutoValveRatio { get; set; }//自动排气阀尺寸
        public double BlockRatio { get; set; }//其他块尺寸
        private int MaxHouse { get; set; } //最大住户数
        private double Dist { get; set; } //管间距
        private int Flag { get; set; } //距离1,2
        private double MinDist { get; set; }//
        private bool PRValveStyle { get; set; }//

        //public ThWSSDBranchPipe(int index, string dn, SysIn sysIn, SysProcess sysProcess )
        //{
        //    ThWSSDStorey storey = sysProcess.StoreyList[index];
        //    double indexStartY = sysIn.InsertPt.Y;
        //    double pipeOffsetX = sysProcess.PipeOffsetX[index];
        //    List<double[]> blockSize = sysIn.BlockSize;
        //    int layingMethod = sysIn.LayingMethod;
        //    int areaIndex = sysIn.AreaIndex;
        //    int maxHouse = sysProcess.MaxHouseholds;

        //    DN = dn;//管径号
        //    FloorNumber = storey.GetFloorNumber();//楼层号
        //    HasFlushFaucet = storey.GetFlushFaucet();//有冲洗龙头
        //    NoValve = storey.GetPRValve();//无减压阀
        //    FloorHeight = storey.GetFloorHeight();//楼层高
        //    Households = storey.GetHouseholds();//住户数
        //    PipeOffsetX = pipeOffsetX;//立管的 X 偏移量
        //    IndexStartY = indexStartY;//起始 Y 偏移量
        //    BlockSize = blockSize;//模型尺寸
        //    LayingMethod = layingMethod;//敷设方式
        //    AreaIndex = areaIndex;
        //    MaxHouse = maxHouse;
        //    bool chaochu = false;

        //    var ratio = new double[] { 1.0, 0.7, 0.6 };
        //    var maxGap = new double[] { 350, 250, 200 };
        //    for (int i = 0; i < 3; i++)
        //    {
        //        var r = ratio[i];
        //        var mGap = maxGap[i];
        //        var gapYDown1 = 30 + 150 * r;
        //        var gapYDown2 = 180;
        //        double gapY = 300 * r;//水表间距 * 缩放因子 为 最小间距
        //        var dist1 = (FloorHeight - 731 * (r - 0.1) - 300 * r - 100 - gapYDown1 - 100 * layingMethod) / (MaxHouse - 1);//较大值
        //        var dist2 = (FloorHeight - 731 * (r - 0.1) - 300 * r - 200 - gapYDown2 - 100 * layingMethod) / (MaxHouse - 1);//较小值
        //        if (r == 1.0 || r == 0.7)
        //        {
        //            if (dist1 > gapY || dist2 > gapY)
        //            {
        //                AutoValveRatio = r - 0.1;
        //                BlockRatio = r;
        //                if (dist2 > mGap)
        //                {
        //                    Dist = mGap;
        //                    Flag = 2;
        //                }
        //                else
        //                {
        //                    Dist = Math.Min(mGap, dist1);
        //                    Flag = 1;
        //                }
        //                break;
        //            }
        //            else
        //            {
        //                chaochu = true;
        //                dist1 = (FloorHeight + 100 - 731 * (r - 0.1) - 300 * r - 100 - gapYDown1 - 100 * layingMethod) / (MaxHouse - 1);//较大值
        //                Flag = 1;
        //                if (dist1 > gapY)
        //                {
        //                    AutoValveRatio = r - 0.1;
        //                    BlockRatio = r;
        //                    Dist = dist1;
        //                    break;
        //                }
        //            }
        //        }
        //        if (r == 0.6)
        //        {
        //            if (dist1 < gapY)
        //            {
        //                chaochu = true;
        //                AutoValveRatio = 0.5;
        //                BlockRatio = 0.6;
        //                Dist = 180;
        //                Flag = 1;
        //                break;
        //            }
        //            AutoValveRatio = 0.5;
        //            BlockRatio = 0.6;
        //            if (dist1 > mGap)
        //            {
        //                Dist = mGap;
        //                Flag = 1;
        //            }
        //            else
        //            {
        //                Dist = dist1;
        //                Flag = 1;
        //            }
        //            break;
        //        }
        //    }
        //    if (layingMethod == 0)
        //    {
        //        if (Households[AreaIndex] != 0 || HasFlushFaucet)
        //        {
        //            InitChuanLiang(1.0 / 15, 0.1, chaochu);
        //        }
        //    }
        //    else
        //    {
        //        if (Households[AreaIndex] != 0 || HasFlushFaucet)
        //        {
        //            InitMaiDi(chaochu);
        //        }
        //    }
        //}

        public ThWSSDBranchPipe(int index, string dn, SysIn sysIn, SysProcess sysProcess, bool prValveStyle)
        {
            ThWSSDStorey storey = sysProcess.StoreyList[index];
            double indexStartY = sysIn.InsertPt.Y;
            double pipeOffsetX = sysProcess.PipeOffsetX[index];
            List<double[]> blockSize = sysIn.BlockSize;
            int layingMethod = sysIn.LayingMethod;
            int areaIndex = sysIn.AreaIndex;
            int maxHouse = sysProcess.MaxHouseholds;

            PRValveStyle = prValveStyle;
            DN = dn;//管径号
            FloorNumber = storey.GetFloorNumber();//楼层号
            HasFlushFaucet = storey.GetFlushFaucet();//有冲洗龙头
            NoValve = storey.GetPRValve();//无减压阀
            FloorHeight = storey.GetFloorHeight();//楼层高
            Households = storey.GetHouseholds();//住户数
            PipeOffsetX = pipeOffsetX;//立管的 X 偏移量
            IndexStartY = indexStartY;//起始 Y 偏移量
            BlockSize = blockSize;//模型尺寸
            LayingMethod = layingMethod;//敷设方式
            AreaIndex = areaIndex;
            MaxHouse = maxHouse;
            bool chaochu = false;

            AutoValveRatio = 0.6;
            BlockRatio = 0.7;
            if (MaxHouse > 5)
            {
                MinDist = 10;
            }
            else if(MaxHouse > 3)
            {
                MinDist = 20;
            }
            else
            {
                MinDist = 30;
            }
            Dist = BlockRatio * BlockSize[2][0] + MinDist;
            Flag = 1;

            if (layingMethod == 0)
            {
                if (Households[AreaIndex] != 0 || HasFlushFaucet)
                {
                    InitChuanLiang(1.0 / 15, 0.1);
                }
            }
            else
            {
                if (Households[AreaIndex] != 0 || HasFlushFaucet)
                {
                    InitMaiDi(chaochu);
                }
            }
        }


        public void InitChuanLiang(double gap2, double gapY2)
        {
            var pt1Y = IndexStartY + FloorNumber * FloorHeight - 512 - MinDist;

            var pt1 = new Point3d(PipeOffsetX, pt1Y, 0);
            var pt2 = pt1.OffsetX(400);
            var pt231 = pt2.OffsetY(-50);
            var pt232 = pt231.OffsetY(-210);
           
            double gapDown = MinDist + 105;

            var h = (Households[AreaIndex] - 1) * Dist + gapDown + FloorHeight * (FloorNumber - 1) + IndexStartY;
            if (HasFlushFaucet)
            {
                h += Dist;
            }
            var pt3 = new Point3d(pt2.X, h, 0);
            if (NoValve)
            {
                PressureReducingValveSite = new Point3d(pt2.X, (pt231.Y + pt232.Y) / 2, 0);//无减压阀的截止阀位置
            }
            else
            {
                PressureReducingValveSite = pt231;//减压阀位置
            }
            TextSite = new Point3d(pt3.X - BlockSize[0][1] / 2 + 50, IndexStartY + FloorHeight * FloorNumber - 700 - FloorHeight / 3, 0);//文字标注

            BranchPipes = new List<Line>();//支管列表
            BranchPipes.Add(new Line(pt1, pt2));
            
            if(!PRValveStyle)
            {
                BranchPipes.Add(new Line(pt2, pt231));
                BranchPipes.Add(new Line(pt232, pt3));
            }

            Point3d pt7;
            Point3d pt11;
            PRValveDetailSite = new Point3d(PipeOffsetX - 5000, IndexStartY + (FloorNumber - 1) * FloorHeight + 200, 0);
            WaterPipeInterrupted = new List<Point3d>();//水管阻断位置列表
            CheckValveSite = new List<Point3d>();//截止阀位置列表
            PRValveSite = new List<Point3d>();
            WaterMeterSite = new List<Point3d>();//水表位置列表

            AutoExhaustValveSite = pt1;
            
            if (Households[AreaIndex] != 0)
            {
                var pt374 = BranchPts.Get(pt3, BranchPipes, PRValveStyle, CheckValveSite, PRValveSite, WaterMeterSite);

                pt7 = pt374.OffsetX(300);
                pt11 = new Point3d(pt7.X, IndexStartY + (FloorNumber - gapY2) * FloorHeight, 0);
                var pt15 = pt11.OffsetX((Households[AreaIndex] - 1) * Dist + 300);

                BranchPipes.Add(new Line(pt374, pt7));
                BranchPipes.Add(new Line(pt7, pt11));
                BranchPipes.Add(new Line(pt11, pt15));
                WaterPipeInterrupted.Add(pt15);//第1个水管截断位置

                for (int i = 1; i < Households[AreaIndex]; i++)
                {
                    var pt4 = new Point3d(pt2.X, pt3.Y - i * Dist, 0);
                    var pt484 = BranchPts.Get(pt4, BranchPipes, PRValveStyle, CheckValveSite, PRValveSite, WaterMeterSite);
                    var pt8 = new Point3d(pt7.X + Dist * i, pt4.Y, 0);
                    var pt12 = new Point3d(pt8.X, pt11.Y - i * Dist, 0);
                    var pt16 = new Point3d(pt11.X + (Households[AreaIndex] - 1) * Dist + 300, pt12.Y, 0);
                    BranchPipes.Add(new Line(pt12, pt16));
                    WaterPipeInterrupted.Add(pt16);//第i个水管截断位置

                    BranchPipes.Add(new Line(pt484, pt8));
                    BranchPipes.Add(new Line(pt8, pt12));
                    if (i == Households[AreaIndex] - 1)
                    {
                        if(PRValveStyle)
                        {
                            BranchPipes.Add(new Line(pt2, pt4));
                        }
                        else
                        {
                            BranchPipes.Add(new Line(pt3, pt4));
                        }
                    }
                }
            }

            if (HasFlushFaucet) //有冲洗龙头
            {
                double pt19Y = IndexStartY + (FloorNumber - 1) * FloorHeight + gapDown;

                var pt19 = new Point3d(pt3.X, pt19Y, 0);
                var pt19204 = BranchPts.Get(pt19, BranchPipes, PRValveStyle, CheckValveSite, PRValveSite, WaterMeterSite);
                var pt20 = new Point3d(pt19204.X + Dist * (Households[AreaIndex] - 1) + 900, pt19.Y, 0);

                double waterGap = 150;
                if (BlockRatio == 0.6)
                {
                    waterGap = 100;
                }
                double pt21Y = pt20.Y + Convert.ToInt32((Households[AreaIndex] + 1) / 2) * Dist + waterGap;
                if (Households[AreaIndex] == 0)
                {
                    pt21Y = pt20.Y + FloorHeight * 0.4;
                }
                var pt21 = new Point3d(pt20.X, pt21Y, 0);

                BranchPipes.Add(new Line(pt3, pt19));
                
                BranchPipes.Add(new Line(pt19204, pt20));
                BranchPipes.Add(new Line(pt20, pt21));

                
                VacuumBreakerSite = pt21;//真空破坏器位置
                WaterTapSite = new Point3d(pt21.X, pt21.Y - waterGap, 0);//水龙头位置
            }
        }

        public void InitMaiDi(bool chaochu)
        {
            var pt1Y = IndexStartY + FloorNumber * FloorHeight - AutoValveRatio * 731 - MinDist;

            var pt1 = new Point3d(PipeOffsetX, pt1Y, 0);
            var pt2 = pt1.OffsetX(400);
            double offsetY = -100;

            if (Flag == 1)
            {
                offsetY = -50;
            }
            var pt231 = pt2.OffsetY(-50);
            Point3d pt232;
            if (NoValve)
            {
                pt232 = pt231.OffsetY(-BlockRatio * BlockSize[1][0]);
            }
            else
            {
                pt232 = pt231.OffsetY(-BlockRatio * BlockSize[0][0]);
            }
            var h = pt232.Y + offsetY;
            if (HasFlushFaucet)
            {
                h -= Dist;
            }
            var pt3 = new Point3d(pt2.X, h, 0);
            TextSite = new Point3d(pt3.X - BlockSize[0][1] / 2 + 50, IndexStartY + FloorHeight * FloorNumber - 700 - FloorHeight / 3, 0);//文字标注
            Point3d pt7;
            Point3d pt11;
            BranchPipes = new List<Line>();//支管列表
            PRValveDetailSite = new Point3d(pt1.X - 5000, IndexStartY + (FloorNumber - 1) * FloorHeight + 200, 0);
            WaterPipeInterrupted = new List<Point3d>();//水管阻断位置列表
            CheckValveSite = new List<Point3d>();//截止阀位置列表
            PRValveSite = new List<Point3d>();
            WaterMeterSite = new List<Point3d>();//水表位置列表

            if (NoValve)
            {
                PressureReducingValveSite = new Point3d(pt2.X, (pt231.Y + pt232.Y) / 2, 0);//无减压阀的截止阀位置
            }
            else
            {
                PressureReducingValveSite = pt231;//减压阀位置
            }
            AutoExhaustValveSite = pt1;
            BranchPipes.Add(new Line(pt1, pt1.OffsetY(-0.12 * FloorHeight)));
            BranchPipes.Add(new Line(pt1, pt2));
            if (!PRValveStyle)
            {
                BranchPipes.Add(new Line(pt2, pt231));
                BranchPipes.Add(new Line(pt232, pt3));
            }

            if (Households[AreaIndex] != 0)
            {
                var pt374 = BranchPts.Get(pt3, BranchPipes, PRValveStyle, CheckValveSite, PRValveSite, WaterMeterSite);

                pt7 = pt374.OffsetX(Dist * (Households[AreaIndex] - 1) + 300);
                pt11 = new Point3d(pt7.X, IndexStartY + FloorHeight * (FloorNumber - 1) + 100, 0);
                WaterPipeInterrupted.Add(pt11);//第1个水管截断位置
                BranchPipes.Add(new Line(pt232, pt3));
                BranchPipes.Add(new Line(pt374, pt7));
                BranchPipes.Add(new Line(pt7, pt11));

                for (int i = 1; i < Households[AreaIndex]; i++)
                {
                    var pt4 = new Point3d(pt2.X, pt3.Y - i * Dist, 0);
                    var pt484 = BranchPts.Get(pt4, BranchPipes, PRValveStyle, CheckValveSite, PRValveSite, WaterMeterSite);
                    Point3d pt8;
                    Point3d pt12;


                    pt8 = new Point3d(pt7.X - i * Dist, pt4.Y, 0);
                    pt12 = new Point3d(pt8.X, pt11.Y, 0);
                    WaterPipeInterrupted.Add(pt12);//第i个水管截断位置

                    BranchPipes.Add(new Line(pt484, pt8));

                    BranchPipes.Add(new Line(pt8, pt12));
                    if (i == Households[AreaIndex] - 1)
                    {
                        if (PRValveStyle)
                        {
                            BranchPipes.Add(new Line(pt2, pt4));
                        }
                        else
                        {
                            BranchPipes.Add(new Line(pt3, pt4));
                        }
                    }
                }
            }

            if (HasFlushFaucet) //有冲洗龙头
            {
                double pt19Y = pt3.Y + Dist;
                if (Households[AreaIndex] == 0)
                {
                    pt19Y = pt3.Y;
                }

                var pt19 = new Point3d(pt3.X, pt19Y, 0);
                var pt19204 = BranchPts.Get(pt19, BranchPipes, PRValveStyle, CheckValveSite, PRValveSite, WaterMeterSite);
                var pt20 = new Point3d(pt19204.X + Dist * Households[AreaIndex] + 300, pt19.Y, 0);
                var pt22 = pt20.OffsetY(100);

                double pt21Y = pt20.Y - Convert.ToInt32(Households[AreaIndex] / 2) * Dist;
                if (Households[AreaIndex] == 0)
                {
                    pt21Y = pt20.Y - FloorHeight * 0.4;
                }
                var pt21 = new Point3d(pt20.X, pt21Y, 0);

                BranchPipes.Add(new Line(pt232, pt19));
                BranchPipes.Add(new Line(pt19204, pt20));
                BranchPipes.Add(new Line(pt22, pt21));

                VacuumBreakerSite = pt22;//真空破坏器位置
                WaterTapSite = pt21;//水龙头位置
            }
        }

        public string GetDN()//获取管径编号
        {
            return DN;
        }
        public Point3d GetTextSite()//获取文字位置
        {
            return TextSite;
        }
        public Point3d GetPressureReducingValveSite()//获取减压阀位置
        {
            return PressureReducingValveSite;
        }

        public List<Point3d> GetCheckValveSite()//获取截止阀位置
        {
            return CheckValveSite;
        }

        public List<Point3d> GetWaterMeterSite()//获取水表位置
        {
            return WaterMeterSite;
        }

        public List<Point3d> GetWaterPipeInterrupted()//获取水管中断位置
        {
            return WaterPipeInterrupted;
        }

        public Point3d GetAutoExhaustValveSite()//获取自动排气阀位置
        {
            return AutoExhaustValveSite;
        }

        public Point3d GetVacuumBreakerSite()//获取真空破坏器位置
        {
            return VacuumBreakerSite;
        }

        public Point3d GetWaterTapSite()//获取水龙头位置
        {
            return WaterTapSite;
        }

        public Point3d GetPRValveDetailSite()//获取减压阀详图位置
        {
            return PRValveDetailSite;
        }

        public int GetHouseholds()
        {
            return Households[AreaIndex];
        }

        public void DrawBranchPipe()
        {
            using var acadDatabase = AcadDatabase.Active();
            if (!(BranchPipes is null))
            {
                if (GetDN() != "")
                {
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-DIMS", WaterSuplyBlockNames.PipeDiameter,
                    GetTextSite(), new Scale3d(1, 1, 1), Math.PI / 2);
                    objID.SetDynBlockValue("可见性", GetDN());
                }

                var BPipeLines = BranchPipes;
                for (int j = 0; j < BPipeLines.Count; j++)
                {
                    BPipeLines[j].LayerId = DbHelper.GetLayerId("W-WSUP-COOL-PIPE");
                    BPipeLines[j].ColorIndex = (int)ColorIndex.BYLAYER;
                    acadDatabase.CurrentSpace.Add(BPipeLines[j]);
                }
            }
        }

        public void DrawAutoExhaustValveNote()
        {
            using var acadDatabase = AcadDatabase.Active();
            var pt1 = new Point3d(AutoExhaustValveSite.X, AutoExhaustValveSite.Y + BlockSize[3][1] / 4, 0);
            var pt2 = new Point3d(pt1.X - 450, pt1.Y - 450, 0);
            var pt3 = new Point3d(pt2.X - 3400, pt2.Y, 0);
            var line1 = new Line(pt1, pt2);
            var line2 = new Line(pt2, pt3);
            line1.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
            line1.ColorIndex = (int)ColorIndex.BYLAYER;
            line2.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
            line2.ColorIndex = (int)ColorIndex.BYLAYER;
            acadDatabase.CurrentSpace.Add(line1);
            acadDatabase.CurrentSpace.Add(line2);

            var text1 = ThText.NoteText(pt3.OffsetXY(50, 50), "自动排气阀DN20，余同");
            acadDatabase.CurrentSpace.Add(text1);

            var text2 = ThText.NoteText(pt3.OffsetXY(50, -350), "排气阀贴板底敷设");
            acadDatabase.CurrentSpace.Add(text2);
        }

        public void DrawLayMethodNote()
        {

            if (LayingMethod == 0)
            {
                using var acadDatabase = AcadDatabase.Active();
                for (int j = 0; j < GetWaterPipeInterrupted().Count; j++)
                {
                    var pti1 = new Point3d(GetWaterPipeInterrupted()[j].X - 53 - 150, GetWaterPipeInterrupted()[j].Y - 53, 0);
                    var pti2 = new Point3d(GetWaterPipeInterrupted()[j].X + 53 - 150, GetWaterPipeInterrupted()[j].Y + 53, 0);
                    InsertLine(acadDatabase, pti1, pti2);
                }
                var pt1 = new Point3d(GetWaterPipeInterrupted()[0].X - 150, GetWaterPipeInterrupted()[GetWaterPipeInterrupted().Count - 1].Y, 0);
                var pt2 = new Point3d(pt1.X, IndexStartY + FloorHeight * FloorNumber + 500, 0);
                var pt3 = new Point3d(pt2.X + 4100, pt2.Y, 0);
                if (HasFlushFaucet)
                {
                    pt3.OffsetX(450);
                }

                InsertLine(acadDatabase, pt1, pt2);
                InsertLine(acadDatabase, pt2, pt3);
                var textX = 450;
                if (HasFlushFaucet)
                {
                    textX += 450;
                }
                var text1 = ThText.NoteText(pt2.OffsetXY(textX, 50), "DNXX×X+DNXX×X (余同)");
                acadDatabase.CurrentSpace.Add(text1);

                var text2 = ThText.NoteText(pt2.OffsetXY(textX, -400), "穿梁敷设，接至户内给水管");
                acadDatabase.CurrentSpace.Add(text2);
            }
            else
            {
                using var acadDatabase = AcadDatabase.Active();
                for (int j = 0; j < GetWaterPipeInterrupted().Count; j++)
                {
                    var pti1 = new Point3d(GetWaterPipeInterrupted()[j].X - 53, GetWaterPipeInterrupted()[j].Y - 53 + 60, 0);
                    var pti2 = new Point3d(GetWaterPipeInterrupted()[j].X + 53, GetWaterPipeInterrupted()[j].Y + 53 + 60, 0);
                    InsertLine(acadDatabase, pti1, pti2);
                }
                var pt1 = GetWaterPipeInterrupted()[0].OffsetY(60);
                var pt2 = GetWaterPipeInterrupted().Last().OffsetXY(200, 60);
                if (GetWaterPipeInterrupted()[0].X > GetWaterPipeInterrupted().Last().X)
                {
                    pt1 = GetWaterPipeInterrupted().Last().OffsetY(60);
                    pt2 = GetWaterPipeInterrupted()[0].OffsetXY(200, 60);
                }
                var pt4 = new Point3d(pt2.X, IndexStartY + FloorHeight * (FloorNumber - 1) + 500, 0);// pt2.OffsetY(400);
                var pt3 = pt4.OffsetX(4100);

                InsertLine(acadDatabase, pt1, pt2);
                InsertLine(acadDatabase, pt2, pt4);
                InsertLine(acadDatabase, pt4, pt3);

                var textX = 450;
                if (HasFlushFaucet)
                {
                    textX += 450;
                }
                var text1 = ThText.NoteText(pt4.OffsetXY(textX, 50), "DNXX×X+DNXX×X (余同)");
                acadDatabase.CurrentSpace.Add(text1);
                var text2 = ThText.NoteText(pt4.OffsetXY(textX, -400), "埋地敷设，接至户内给水管");
                acadDatabase.CurrentSpace.Add(text2);
            }
        }

        public static void InsertLine(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2)
        {
            var line12 = new Line(pt1, pt2);
            line12.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
            line12.ColorIndex = (int)ColorIndex.BYLAYER;
            acadDatabase.CurrentSpace.Add(line12);
        }
    }
}
