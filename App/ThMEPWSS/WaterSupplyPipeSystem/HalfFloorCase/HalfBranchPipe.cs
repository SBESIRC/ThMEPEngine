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

namespace ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase
{
    public class HalfBranchPipe
    {
        public int FloorNumber { get; set; }  //楼层号
        public string DN { get; set; }  //管径号

        public List<Line> BranchPipes;//PipeUnit的数组
        public bool HasFlushFaucet { get; set; } //有冲洗龙头
        public bool NoValve { get; set; }//无减压阀
        public int[] Households { get; set; }//住户数
        public Point3d PressureReducingValveSite { get; set; } //减压阀位置
        public List<Point3d> CheckValveSite { get; set; } //截止阀位置
        public List<Point3d> PRValveSite { get; set; } //减压阀位置
        public List<Point3d> WaterMeterSite { get; set; } //水表位置
        public List<Point3d> WaterPipeInterrupted { get; set; } //水管中断位置
        public Point3d AutoExhaustValveSite { get; set; } //自动排气阀位置
        public Point3d VacuumBreakerSite { get; set; } //真空破坏器位置
        public Point3d WaterTapSite { get; set; } //水龙头位置
        public Point3d PRValveDetailSite { get; set; } //减压阀详图位置
        public List<double[]> BlockSize { get; set; } //模型尺寸
        public int LayingMethod { get; set; } //敷设方式
        public Point3d TextSite { get; set; }//文字位置
        public double FloorHeight { get; set; }//楼层高
        public double PipeOffsetX { get; set; }//立管的 X 偏移量
        public double BranchPipeX { get; set; }//支管偏移量
        public double IndexStartY { get; set; }//起始 Y 偏移量
        public int AreaIndex { get; set; }//分区索引
        public double AutoValveRatio { get; set; }//自动排气阀尺寸
        public double BlockRatio { get; set; }//其他块尺寸
        public int MaxHouse { get; set; } //最大住户数
        public double Dist { get; set; } //管间距
        public int Flag { get; set; } //距离1,2
        public int CaseType { get; set; } //半平台类型
        public List<Point3d> FloorPts { get; set; }//沿楼板线敷设标注点
        public bool PRValveStyle { get; set; }

        public HalfBranchPipe(int index, string dn, SysIn sysIn, SysProcess sysProcess,int caseType, bool prValveStyle)
        {
            var storey = sysProcess.StoreyList[index];
            double indexStartY = sysIn.InsertPt.Y;
            double pipeOffsetX = sysProcess.PipeOffsetX[index];
            List<double[]> blockSize = sysIn.BlockSize;
            int layingMethod = sysIn.LayingMethod;
            int areaIndex = sysIn.AreaIndex;
            int maxHouse = sysProcess.MaxHouseholds;
            CaseType = caseType;
            PRValveStyle = prValveStyle;

            DN = dn;//管径号
            FloorNumber = storey.GetFloorNumber();//楼层号
            HasFlushFaucet = storey.GetFlushFaucet();//有冲洗龙头
            NoValve = storey.GetPRValve();//无减压阀
            FloorHeight = storey.GetFloorHeight();//楼层高
            Households = storey.GetHouseholds();//住户数
            PipeOffsetX = pipeOffsetX;//立管的 X 偏移量
            BranchPipeX = sysProcess.BranchPipeX[index];
            IndexStartY = indexStartY;//起始 Y 偏移量
            BlockSize = blockSize;//模型尺寸
            LayingMethod = layingMethod;//敷设方式
            AreaIndex = areaIndex;
            MaxHouse = maxHouse;
            BlockRatio = 0.7;
            FloorPts = new List<Point3d>();

            BranchPipes = new List<Line>();//支管列表
            WaterPipeInterrupted = new List<Point3d>();//水管阻断位置列表
            CheckValveSite = new List<Point3d>();//截止阀位置列表
            PRValveSite = new List<Point3d>();
            WaterMeterSite = new List<Point3d>();//水表位置列表
        }

        public void DrawBranchPipe()
        {
            using var acadDatabase = AcadDatabase.Active();
            if (!(BranchPipes is null))
            {
                if (DN != "")
                {
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-DIMS", WaterSuplyBlockNames.PipeDiameter,
                    TextSite, new Scale3d(1, 1, 1), Math.PI / 2);
                    objID.SetDynBlockValue("可见性", DN);
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
            var layMethod = "穿梁敷设，接至户内给水管";
            if(LayingMethod == 1)
            {
                layMethod = "埋地敷设，接至户内给水管";
            }
            if (CaseType != 3 && CaseType != 4)
            {
                using var acadDatabase = AcadDatabase.Active();
                for (int j = 0; j < WaterPipeInterrupted.Count; j++)
                {
                    var pti1 = new Point3d(WaterPipeInterrupted[j].X - 53 - 150, WaterPipeInterrupted[j].Y - 53, 0);
                    var pti2 = new Point3d(WaterPipeInterrupted[j].X + 53 - 150, WaterPipeInterrupted[j].Y + 53, 0);
                    InsertLine(acadDatabase, pti1, pti2);
                }
                var pt1 = new Point3d(WaterPipeInterrupted[0].X - 150, WaterPipeInterrupted[WaterPipeInterrupted.Count - 1].Y, 0);
                var pt2 = new Point3d(pt1.X, IndexStartY + FloorHeight * FloorNumber + 1100, 0);
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

                var text2 = ThText.NoteText(pt2.OffsetXY(textX, -400), layMethod);
                acadDatabase.CurrentSpace.Add(text2);
            }
            else
            {
                using var acadDatabase = AcadDatabase.Active();
                for (int j = 0; j < WaterPipeInterrupted.Count; j++)
                {
                    var pti1 = new Point3d(WaterPipeInterrupted[j].X - 53, WaterPipeInterrupted[j].Y - 53 + 60, 0);
                    var pti2 = new Point3d(WaterPipeInterrupted[j].X + 53, WaterPipeInterrupted[j].Y + 53 + 60, 0);
                    InsertLine(acadDatabase, pti1, pti2);
                }
                var pt1 = WaterPipeInterrupted[0].OffsetY(60);
                var pt2 = WaterPipeInterrupted.Last().OffsetXY(200, 60);
                if (WaterPipeInterrupted[0].X > WaterPipeInterrupted.Last().X)
                {
                    pt1 = WaterPipeInterrupted.Last().OffsetY(60);
                    pt2 = WaterPipeInterrupted[0].OffsetXY(200, 60);
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
                var text2 = ThText.NoteText(pt4.OffsetXY(textX, -400), layMethod);
                acadDatabase.CurrentSpace.Add(text2);
            }
        }

        public void DrawStairCallout()
        {
            var stairs = new List<int>() { 1,2,3,4,9,10,11,12,13,14};
            if (stairs.Contains(CaseType))
            {
                using (var acadDatabase = AcadDatabase.Active())
                {
                    var pt1 = FloorPts.First();
                    var pt2 = FloorPts.Last();
                    InsertLine(acadDatabase,pt1.OffsetXY(53, -53), pt1.OffsetXY(-53, 53));
                    InsertLine(acadDatabase,pt2.OffsetXY(53, -53), pt2.OffsetXY(-53, 53));
                    var pt3 = new Point3d(pt2.X, IndexStartY + FloorNumber * FloorHeight + 800, 0);
                    InsertLine(acadDatabase,pt2, pt3);
                    InsertLine(acadDatabase,pt3, pt3.OffsetX(3850));

                    var text1 = ThText.NoteText(pt3.OffsetXY(50, 50), "DN25*2 墙体开槽/明敷(余同)");
                    acadDatabase.CurrentSpace.Add(text1);

                    var text2 = ThText.NoteText(pt3.OffsetXY(50, -350), "沿楼板斜线敷设");
                    acadDatabase.CurrentSpace.Add(text2);
                }
                    

            }
        }
        public static void InsertLine(AcadDatabase acadDatabase, Point3d pt1, Point3d pt2)
        {
            var line12 = new Line(pt1, pt2);
            line12.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
            line12.ColorIndex = (int)ColorIndex.BYLAYER;
            acadDatabase.CurrentSpace.Add(line12);
        }
        public static void InsertLine(Point3d pt1, Point3d pt2)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var line12 = new Line(pt1, pt2);
                line12.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
                line12.ColorIndex = (int)ColorIndex.BYLAYER;
                acadDatabase.CurrentSpace.Add(line12);
            }
       
        }
    }
}
