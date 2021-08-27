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
    public class ThWSSDBranchPipe   //给用户供水的支管类
    {
        private int FloorNumber { get; set; }  //楼层号
        private string DN { get; set; }  //管径号

        public List<Line> BranchPipes;//PipeUnit的数组
        private bool HasFlushFaucet { get; set; } //有冲洗龙头
        private bool NoValve { get; set; }//无减压阀
        private int[] Households { get; set; }//住户数
        private Point3d PressureReducingValveSite { get; set; } //减压阀位置
        private List<Point3d> CheckValveSite { get; set; } //截止阀位置
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
        public ThWSSDBranchPipe(string dn, ThWSSDStorey storey, double indexStartY, double pipeOffsetX, List<double[]> blockSize, int layingMethod, int areaIndex)
        {
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

            if (Households[AreaIndex] == 0 && HasFlushFaucet)
            {
                var pt1 = new Point3d(PipeOffsetX, IndexStartY + (FloorNumber - 0.175) * FloorHeight, 0);
                var pt2 = new Point3d(pt1.X + 0.2 * FloorHeight, pt1.Y, 0);
                BranchPipes = new List<Line>();//支管列表
                BranchPipes.Add(new Line(pt1, pt2));

                AutoExhaustValveSite = pt1;//自动排气阀位置
                CheckValveSite = new List<Point3d>();//截止阀位置列表
                WaterMeterSite = new List<Point3d>();//水表位置列表

                var pt19 = new Point3d(pt2.X, pt2.Y - 0.645 * FloorHeight, 0);
                var pt19201 = new Point3d(pt19.X + 225, pt19.Y, 0);
                var pt19202 = new Point3d(pt19201.X + 0.5 * BlockSize[1][0], pt19.Y, 0);
                var pt19203 = new Point3d(pt19202.X + 75, pt19.Y, 0);
                var pt19204 = new Point3d(pt19203.X + 0.5 * BlockSize[2][0], pt19.Y, 0);
                var pt20 = new Point3d(pt19204.X + 300, pt19.Y, 0);
                var pt21 = new Point3d(pt20.X, pt20.Y + 0.45 * FloorHeight, 0);

                BranchPipes.Add(new Line(pt2, pt19));
                BranchPipes.Add(new Line(pt19, pt19201));
                BranchPipes.Add(new Line(pt19202, pt19203));
                BranchPipes.Add(new Line(pt19204, pt20));
                BranchPipes.Add(new Line(pt20, pt21));

                CheckValveSite.Add(new Point3d((pt19201.X + pt19202.X) / 2, pt19201.Y, 0));//截止阀位置
                WaterMeterSite.Add(new Point3d((pt19203.X + pt19204.X) / 2, pt19203.Y, 0));//水表位置
                VacuumBreakerSite = pt21;//真空破坏器位置
                WaterTapSite = new Point3d(pt21.X, pt21.Y - 150, 0);//水龙头位置

            }
            else if (Households[AreaIndex] == 0)//没有住户不添加支管
            {
                ;
            }
            else
            {
                Init();
            }
        }

        public void Init()
        {
            //支管 point 初始化
            var pt1 = new Point3d(PipeOffsetX, IndexStartY + (FloorNumber - 0.175) * FloorHeight, 0);
            var pt2 = new Point3d(pt1.X + 400, pt1.Y, 0);
            var pt231 = new Point3d(pt2.X, pt2.Y - 0.05 * FloorHeight, 0);
            Point3d pt232, pt3;

            if (NoValve)
            {
                pt232 = new Point3d(pt2.X, pt231.Y - 0.5 * BlockSize[1][0], 0);
            }
            else
            {
                pt232 = new Point3d(pt2.X, pt231.Y - 0.7 * BlockSize[0][0], 0);
            }

            var h = FloorHeight *(((Households[AreaIndex] -1 ) * 0.14 + 0.1)) + FloorHeight * (FloorNumber - 1) + IndexStartY;
            if (HasFlushFaucet)
            {
                h += FloorHeight * 0.14;
            }
            //pt3 = new Point3d(pt2.X, pt232.Y - 0.05 * FloorHeight, 0);
            pt3 = new Point3d(pt2.X, h, 0);
            TextSite = new Point3d(pt3.X - BlockSize[0][1] / 2 + 50, IndexStartY + FloorHeight * FloorNumber - 700 - FloorHeight / 3, 0);//文字标注
            var pt371 = new Point3d(pt3.X + 225, pt3.Y, 0);
            var pt372 = new Point3d(pt371.X + 1 * BlockSize[1][0], pt3.Y, 0);
            var pt373 = new Point3d(pt372.X + 75, pt3.Y, 0);
            var pt374 = new Point3d(pt373.X + 0.8 * BlockSize[2][0], pt3.Y, 0);
            Point3d pt7;
            Point3d pt11;

            PRValveDetailSite = new Point3d(pt1.X + 7000, IndexStartY + (FloorNumber - 1) * FloorHeight + 200, 0);
            BranchPipes = new List<Line>();//支管列表
            WaterPipeInterrupted = new List<Point3d>();//水管阻断位置列表
            CheckValveSite = new List<Point3d>();//截止阀位置列表
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

            double gap = 0.064;
            if (Households[AreaIndex] > 2 && Households[AreaIndex] < 6)
            {
                gap = 0.08;
            }
            if (Households[AreaIndex] < 3)
            {
                gap = 0.1;
            }

            if (LayingMethod == 0)//穿梁敷设
            {
                pt7 = new Point3d(pt374.X + 300, pt3.Y, 0);
                pt11 = new Point3d(pt7.X, IndexStartY + (FloorNumber - 0.075) * FloorHeight, 0);
                var pt15 = new Point3d(pt11.X + 0.5 * FloorHeight, pt11.Y, 0);
                BranchPipes.Add(new Line(pt11, pt15));

                WaterPipeInterrupted.Add(pt15);//第1个水管截断位置
            }
            else
            {
                pt7 = new Point3d(pt374.X + 150 * Households[AreaIndex], pt3.Y, 0);

                pt11 = new Point3d(pt7.X, pt7.Y - gap * (Households[AreaIndex] + 1) * FloorHeight, 0);
                WaterPipeInterrupted.Add(pt11);//第1个水管截断位置
            }

            BranchPipes.Add(new Line(pt1, pt2));
            BranchPipes.Add(new Line(pt2, pt231));
            BranchPipes.Add(new Line(pt232, pt3));
            BranchPipes.Add(new Line(pt3, pt371));
            BranchPipes.Add(new Line(pt372, pt373));
            BranchPipes.Add(new Line(pt374, pt7));
            BranchPipes.Add(new Line(pt7, pt11));


            CheckValveSite.Add(new Point3d((pt371.X + pt372.X) / 2, pt3.Y, 0));//第一个截止阀位置                      
            WaterMeterSite.Add(new Point3d((pt373.X + pt374.X) / 2, pt3.Y, 0));//第一个水表位置

            for (int i = 1; i < Households[AreaIndex]; i++)
            {
                var pt4 = new Point3d(pt2.X, pt3.Y - i * 0.14 * FloorHeight, 0);
                var pt481 = new Point3d(pt371.X, pt4.Y, 0);
                var pt482 = new Point3d(pt372.X, pt4.Y, 0);
                var pt483 = new Point3d(pt373.X, pt4.Y, 0);
                var pt484 = new Point3d(pt374.X, pt4.Y, 0);
                Point3d pt8;
                Point3d pt12;
                BranchPipes.Add(new Line(pt4, pt481));
                BranchPipes.Add(new Line(pt482, pt483));

                CheckValveSite.Add(new Point3d((pt481.X + pt482.X) / 2, pt4.Y, 0));//第i个截止阀位置
                WaterMeterSite.Add(new Point3d((pt483.X + pt484.X) / 2, pt4.Y, 0));//第i个水表位置

                if (LayingMethod == 0)//穿梁敷设
                {
                    pt8 = new Point3d(pt7.X + 150 * i, pt4.Y, 0);
                    pt12 = new Point3d(pt8.X, pt11.Y - i * 0.075 * FloorHeight, 0);
                    var pt16 = new Point3d(pt11.X + 0.5 * FloorHeight, pt12.Y, 0);
                    BranchPipes.Add(new Line(pt12, pt16));
                    WaterPipeInterrupted.Add(pt16);//第i个水管截断位置
                }
                else
                {
                    pt8 = new Point3d(pt7.X - i * 150, pt4.Y, 0);
                    pt12 = new Point3d(pt8.X, pt11.Y, 0);
                    WaterPipeInterrupted.Add(pt12);//第i个水管截断位置
                }
                BranchPipes.Add(new Line(pt4, pt481));

                BranchPipes.Add(new Line(pt482, pt483));

                BranchPipes.Add(new Line(pt484, pt8));

                BranchPipes.Add(new Line(pt8, pt12));
                if (i == Households[AreaIndex] - 1)
                {
                    BranchPipes.Add(new Line(pt3, pt4));
                }
            }


            if (HasFlushFaucet) //有冲洗龙头
            {
                double pt19Y = 0;
                if (Households[AreaIndex] > 6)
                {
                    pt19Y = IndexStartY + (gap * 0.5 + FloorNumber - 1) * FloorHeight;
                }
                else
                {
                    pt19Y = IndexStartY + (gap + FloorNumber - 1) * FloorHeight;
                }
                var pt19 = new Point3d(pt3.X, pt19Y, 0);
                var pt19201 = new Point3d(pt371.X, pt19.Y, 0);
                var pt19202 = new Point3d(pt372.X, pt19.Y, 0);
                var pt19203 = new Point3d(pt373.X, pt19.Y, 0);
                var pt19204 = new Point3d(pt374.X, pt19.Y, 0);
                var pt20 = new Point3d();
                if (LayingMethod == 0)
                {
                    pt20 = new Point3d(pt7.X + 150 * (Households[AreaIndex] + 4), pt19.Y, 0);
                }
                else
                {
                    pt20 = new Point3d(pt7.X + 300, pt19.Y, 0);
                }
                var pt21 = new Point3d(pt20.X, pt20.Y + 0.45 * FloorHeight, 0);

                BranchPipes.Add(new Line(pt3, pt19));
                BranchPipes.Add(new Line(pt19, pt19201));
                BranchPipes.Add(new Line(pt19202, pt19203));
                BranchPipes.Add(new Line(pt19204, pt20));
                BranchPipes.Add(new Line(pt20, pt21));

                CheckValveSite.Add(new Point3d((pt19201.X + pt19202.X) / 2, pt19201.Y, 0));//第五个截止阀位置
                WaterMeterSite.Add(new Point3d((pt19203.X + pt19204.X) / 2, pt19203.Y, 0));//第五个水表位置
                VacuumBreakerSite = pt21;//真空破坏器位置
                WaterTapSite = new Point3d(pt21.X, pt21.Y - 150, 0);//水龙头位置
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
            line2.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
            acadDatabase.CurrentSpace.Add(line1);
            acadDatabase.CurrentSpace.Add(line2);

            var text1 = ThText.NoteText(pt3.OffsetXY(50, 50), "自动排气阀DN20，余同");
            acadDatabase.CurrentSpace.Add(text1);

            
            var text2 = ThText.NoteText(pt3.OffsetXY(50, -350), "排气阀贴板底敷设");
            acadDatabase.CurrentSpace.Add(text2);
        }

        public void DrawLayMethodNote()
        {
            using var acadDatabase = AcadDatabase.Active();
            for (int j = 0; j < GetWaterPipeInterrupted().Count; j++)
            {
                var pti1 = new Point3d(GetWaterPipeInterrupted()[j].X - 106 - 150, GetWaterPipeInterrupted()[j].Y - 106, 0);
                var pti2 = new Point3d(GetWaterPipeInterrupted()[j].X + 106 - 150, GetWaterPipeInterrupted()[j].Y + 106, 0);
                var line1 = new Line(pti1, pti2);
                line1.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
                acadDatabase.CurrentSpace.Add(line1);
            }
            var pt1 = new Point3d(GetWaterPipeInterrupted()[0].X - 150, GetWaterPipeInterrupted()[GetWaterPipeInterrupted().Count - 1].Y, 0);
            var pt2 = new Point3d(pt1.X, GetWaterPipeInterrupted()[0].Y + 650, 0);
            var pt3 = new Point3d(pt2.X + 3700, pt2.Y, 0);
            var line12 = new Line(pt1, pt2);
            var line23 = new Line(pt2, pt3);
            line12.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
            line23.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
            acadDatabase.CurrentSpace.Add(line12);
            acadDatabase.CurrentSpace.Add(line23);

            
            var text1 = ThText.NoteText(pt2.OffsetXY(50, 50), "DNXX×X+DNXX×X（余同）");

            acadDatabase.CurrentSpace.Add(text1);

            if (LayingMethod == 0)
            {
                var text2 = ThText.NoteText(pt2.OffsetXY(50, -350), "XXXX敷设，接至户内给水管");
                acadDatabase.CurrentSpace.Add(text2);
            }
        }
    }
}
