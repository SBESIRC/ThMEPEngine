using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using DU = ThMEPWSS.Assistant.DrawUtils;

namespace ThMEPWSS.Pipe.Model
{
    //预定义 block 名称
    public class WaterSuplyBlockNames
    {
        public const string CheckValve = "截止阀";
        public const string AutoExhaustValve = "自动排气阀系统1";
        public const string PressureReducingValve = "减压阀";
        public const string VacuumBreaker = "真空破坏器";
        public const string WaterMeter = "水表1";
        public const string WaterPipeInterrupted = "水管中断";
        public const string WaterTap = "水龙头1";
    }

    public class WaterSuplyUtils
    {
        //读取供水系统模块文件的路径
        public static string WaterSuplyBlockFilePath
        {
            get
            {
                var path = Path.Combine(ThCADCommon.SupportPath(), "地上给水排水平面图模板_20210125.dwg");
                return path;
            }
        }
        //加载需要使用的模块
        public static void ImportNecessaryBlocks()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
            using (AcadDatabase blockDb = AcadDatabase.Open(WaterSuplyBlockFilePath, DwgOpenMode.ReadOnly, false))//引用模块的位置
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.CheckValve));//截止阀
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.AutoExhaustValve));//自动排气阀系统1
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PressureReducingValve));//减压阀
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.VacuumBreaker));//真空破坏器
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.WaterMeter));//水表1
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.WaterPipeInterrupted));//水管中断
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.WaterTap));//水龙头
            }
        }
    }
    public enum LayingMethod //敷设方式
    {
        Piercing,  //穿梁
        Buried     //埋地
    }


    public class ThWSSDStorey  //楼层类  Th Water Suply System Diagram Storey
    {
        private int FloorNumber { get; set; }//楼层号
        private int FloorHeight { get; set; }//楼层线间距
        private bool HasFlushFaucet { get; set; }//有冲洗龙头
        private bool NoPRValve { get; set; }//无减压阀

        public ThWSSDStorey(int floorNumber, int floorHeight, bool hasFlushFaucet, bool noPRValve)
        {
            FloorNumber = floorNumber;
            FloorHeight = floorHeight;
            HasFlushFaucet = hasFlushFaucet;
            NoPRValve = noPRValve;
        }

        public bool getFlushFaucet()
        {
            return HasFlushFaucet;
        }
        public bool getPRValve()
        {
            return NoPRValve;
        }

        //绘制楼层线
        public Line CreateLine(double INDEX_START_X, double INDEX_START_Y, double FLOOR_LENGTH)  
        {
            var pt1 = new Point3d(INDEX_START_X, INDEX_START_Y + (FloorNumber -1) * FloorHeight, 0);
            var pt2 = new Point3d(INDEX_START_X + FLOOR_LENGTH, INDEX_START_Y + (FloorNumber - 1) * FloorHeight, 0);
            var line1 = new Line(pt1, pt2);

            return line1;
        }
    }


    public class ThWSuplySystemDiagram // 竖管系统类
    {
        private int Loweststorey { get; set; }//竖管最低层
        private int Higheststorey { get; set; }//竖管最高层
        private double PipeOffset_X { get; set; } //竖管相对于楼层的偏移量
        private List<double[]> BlockSize { get; set; }
        
        private double PipeX { get; set; }//竖管的X坐标
        public List<ThWSSDPipeUnit> PipeUnits { get; set; }//PipeUnit的数组


        public ThWSuplySystemDiagram(int loweststorey, int higheststorey, double pipeOffset_X, List<double[]> blockSize)
        {
            Loweststorey = loweststorey;
            Higheststorey = higheststorey;
            PipeOffset_X = pipeOffset_X;
            BlockSize = blockSize;
            PipeUnits = new List<ThWSSDPipeUnit>();
        }

        public double GetPipeX()
        {
            return PipeX;
        }

        public Line CreatePipeLine(double INDEX_START_X, double INDEX_START_Y, double FloorHeight, double PipeGap, List<double[]> blockSize)
        {
            var pt1 = new Point3d(INDEX_START_X + PipeOffset_X + PipeGap, INDEX_START_Y - 300, 0);
            var pt2 = new Point3d(INDEX_START_X + PipeOffset_X + PipeGap, INDEX_START_Y + Higheststorey * FloorHeight - 0.175 * FloorHeight, 0);
            var line1 = new Line(pt1, pt2);
            PipeX = pt1.X;
            return line1;
        }

    }


    public class ThWSSDPipeUnit  //竖管单元
    {
        private string PipeDiameter { get; set; }
        private int FloorNumber { get; set; }

        public ThWSSDPipeUnit(String pipeDiameter, int floorNumber)  //构造函数
        {
            PipeDiameter = pipeDiameter;
            FloorNumber = floorNumber;
        }
    }

    public class PipeCompute  //管径计算
    {
        private double Ng { get; set; }//用水总当量/分区内住户总数
        private int QL { get; set; }//最高日用水定额 QL
        private double Kh { get; set; }//最高日小时变化系数  Kh
        private double NumPerHouse { get; set; }//每户人数  m

        const double T = 24; //用水时数，24

        public PipeCompute(double ng, int qL, double kh, double numPerHouse)
        {
            Ng = ng;
            QL = qL;
            Kh = kh;
            NumPerHouse = numPerHouse;
        }

        public String PipeDiameterCompute()
        {   
            //计算各管段的流量
            double U0 = 100 * QL * NumPerHouse * Kh / (0.2 * Ng * T * 3600);
            Dictionary<double, double> U0ToAlphaC = new Dictionary<double, double>
            {{1.0, 0.00323 }, {1.5, 0.00697 }, {2.0, 0.01097 }, {2.5, 0.01512 }, {3.0, 0.01939 }, {3.5, 0.02374 },
            {4.0, 0.02816 }, {4.5, 0.03263 }, {5.0, 0.03715 }, {6.0, 0.04629 }, {7.0, 0.05555 }, {8.0, 0.06489 }};
            double key1 = 1.0;
            double alphaC = 0;//对应于 U0 的系数，线性插入
            foreach (double key in U0ToAlphaC.Keys)
            {
                if (U0 >= key)
                {
                    key1 = key;
                }
                if (U0 < key)
                {
                    alphaC = (U0ToAlphaC[key] - U0ToAlphaC[key1]) * (U0 - key1) / (key - key1) + U0ToAlphaC[key1];
                }
            }

            double U = (1 + alphaC * Math.Pow((Ng - 1), 0.49)) / (Math.Sqrt(Ng));

            double qg = 0.2 * U * Ng;  //管段的设计秒流量

            //管径列表
            Dictionary<string, double> pipeDList = new Dictionary<string, double>
            { {"DN20",0.0213 }, {"DN25",0.0273 },  {"DN32",0.0354 }, {"DN40",0.0413 },
              {"DN50",0.0527 }, {"DN65",0.0681 },  {"DN80",0.0809 }, {"DN100",0.1063 },
              {"DN125",0.131 }, {"DN150",0.1593 }, {"DN200",0.2071 } };

            foreach (string key in pipeDList.Keys)
            {
                double d = pipeDList[key];
                double FlowRate = qg * 4 / (Math.PI * Math.Pow(d, 2) * 1000);  // 不同管径下的流速
                switch (key)

                {
                    case "DN20":
                        if (FlowRate <= 0.8)
                        {
                            return key;
                        }
                        break;
                    case "DN25":
                    case "DN32":
                    case "DN40":
                        if (FlowRate <= 1)
                        {
                            return key;
                        }
                        break;
                    case "DN50":
                    case "DN65":
                        if (FlowRate > 1 && FlowRate <= 1.2)
                        {
                            return key;
                        }
                        break;
                    default:
                        if (FlowRate > 1.2 && FlowRate <= 1.5)
                        {
                            return key;
                        }
                        break;
                }
            }
            return "";
        }
    }


    public class ThWSSDBranchPipe : IThWDraw   //给用户供水的支管类
    {
        private int FloorNumber { get; set; }  //楼层号
        public List<Line> BranchPipes;//PipeUnit的数组
        private bool HasFlushFaucet { get; set; } //存在冲洗龙头
        private bool NoValve { get; set; }//无减压阀
        private Point3d PressureReducingValveSite { get; set; } //减压阀位置
        private List<Point3d> CheckValveSite { get; set; } //截止阀位置
        private List<Point3d> WaterMeterSite { get; set; } //水表位置
        private List<Point3d> WaterPipeInterrupted { get; set; } //水管中断位置
        private Point3d AutoExhaustValveSite { get; set; } //自动排气阀位置
        private Point3d VacuumBreakerSite { get; set; } //真空破坏器位置
        private Point3d WaterTapSite{ get; set; } //水龙头位置
        private List<double []> BlockSize { get; set; } //模型尺寸
        private int LAYINGMETHOD { get; set; } //敷设方式
        public double FloorHeight { get; set; }
        public double PipeOffsetX { get; set; }
        public double INDEX_START_Y { get; set; }
        public ThWSSDBranchPipe(int floorNumber, double floorHeight, double pipeOffsetX, double index_START_Y, bool hasFlushFaucet, bool noValve , List<double[]> blockSize, int lAYINGMETHOD)
        {
            FloorNumber = floorNumber;
            HasFlushFaucet = hasFlushFaucet;
            NoValve = noValve;
            FloorHeight = floorHeight;
            PipeOffsetX = pipeOffsetX;
            INDEX_START_Y = index_START_Y;
            BlockSize = blockSize;
            LAYINGMETHOD = lAYINGMETHOD;
            if(FloorNumber == 1)
            {
                var pt1 = new Point3d(PipeOffsetX, INDEX_START_Y + (FloorNumber - 0.175) * FloorHeight, 0);
                var pt2 = new Point3d(pt1.X + 0.2 * FloorHeight, pt1.Y, 0);
                BranchPipes = new List<Line>();//支管列表
                BranchPipes.Add(new Line(pt1, pt2));

                AutoExhaustValveSite = pt1;
                CheckValveSite = new List<Point3d>();//截止阀位置列表
                WaterMeterSite = new List<Point3d>();//水表位置列表

                if (HasFlushFaucet) //有冲洗龙头
                {
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

            }
            else if(FloorNumber == 2)
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
            var pt1 = new Point3d(PipeOffsetX, INDEX_START_Y + (FloorNumber - 0.175) * FloorHeight, 0);
            var pt2 = new Point3d(pt1.X + 400, pt1.Y, 0);
            var pt231 = new Point3d(pt2.X, pt2.Y - 0.1 * FloorHeight, 0);
            var pt232 = new Point3d();
            if (NoValve)
            {
                pt232 = new Point3d(pt2.X, pt2.Y - 0.1 * FloorHeight - 0.5 * BlockSize[1][0], 0);
            }
            else
            {
                pt232 = new Point3d(pt2.X, pt2.Y - 0.1 * FloorHeight - 0.7 * BlockSize[0][0], 0);
            }
            var pt3 = new Point3d(pt2.X, pt232.Y - 0.145 * FloorHeight, 0);
            var pt4 = new Point3d(pt2.X, pt3.Y - 0.1 * FloorHeight, 0);
            var pt5 = new Point3d(pt2.X, pt4.Y - 0.1 * FloorHeight, 0);
            var pt6 = new Point3d(pt2.X, pt5.Y - 0.1 * FloorHeight, 0);

            var pt371 = new Point3d(pt3.X + 225, pt3.Y, 0);
            var pt372 = new Point3d(pt371.X + 0.5 * BlockSize[1][0], pt3.Y, 0);
            var pt373 = new Point3d(pt372.X + 75, pt3.Y, 0);
            var pt374 = new Point3d(pt373.X + 0.5 * BlockSize[2][0], pt3.Y, 0);
            
            var pt481 = new Point3d(pt371.X, pt4.Y, 0);
            var pt482 = new Point3d(pt372.X, pt4.Y, 0);
            var pt483 = new Point3d(pt373.X, pt4.Y, 0);
            var pt484 = new Point3d(pt374.X, pt4.Y, 0);
            
            var pt591 = new Point3d(pt371.X, pt5.Y, 0);
            var pt592 = new Point3d(pt372.X, pt5.Y, 0);
            var pt593 = new Point3d(pt373.X, pt5.Y, 0);
            var pt594 = new Point3d(pt374.X, pt5.Y, 0);
            
            var pt6101 = new Point3d(pt371.X, pt6.Y, 0);
            var pt6102 = new Point3d(pt372.X, pt6.Y, 0);
            var pt6103 = new Point3d(pt373.X, pt6.Y, 0);
            var pt6104 = new Point3d(pt374.X, pt6.Y, 0);

            BranchPipes = new List<Line>();//支管列表
            BranchPipes.Add(new Line(pt1, pt2));
            BranchPipes.Add(new Line(pt2, pt231));
            BranchPipes.Add(new Line(pt232, pt3));
            BranchPipes.Add(new Line(pt3, pt6));
            BranchPipes.Add(new Line(pt3, pt371));
            BranchPipes.Add(new Line(pt372, pt373));
            BranchPipes.Add(new Line(pt4, pt481));
            BranchPipes.Add(new Line(pt482, pt483));
            BranchPipes.Add(new Line(pt5, pt591));
            BranchPipes.Add(new Line(pt592, pt593));
            BranchPipes.Add(new Line(pt6, pt6101));
            BranchPipes.Add(new Line(pt6102, pt6103));

            CheckValveSite = new List<Point3d>();//截止阀位置列表
            CheckValveSite.Add(new Point3d((pt371.X + pt372.X) / 2, pt3.Y, 0));//第一个截止阀位置
            CheckValveSite.Add(new Point3d((pt481.X + pt482.X) / 2, pt4.Y, 0));//第二个截止阀位置
            CheckValveSite.Add(new Point3d((pt591.X + pt592.X) / 2, pt5.Y, 0));//第三个截止阀位置
            CheckValveSite.Add(new Point3d((pt6101.X + pt6102.X) / 2, pt6.Y, 0));//第四个截止阀位置

            WaterMeterSite = new List<Point3d>();//水表位置列表
            WaterMeterSite.Add(new Point3d((pt373.X + pt374.X) / 2, pt3.Y, 0));//第一个水表位置
            WaterMeterSite.Add(new Point3d((pt483.X + pt484.X) / 2, pt4.Y, 0));//第二个水表位置
            WaterMeterSite.Add(new Point3d((pt593.X + pt594.X) / 2, pt5.Y, 0)); ;//第三个水表位置
            WaterMeterSite.Add(new Point3d((pt6103.X + pt6104.X) / 2, pt6.Y, 0));//第四个水表位置

            if (LAYINGMETHOD == 0)
            {
                var pt7 = new Point3d(pt374.X + 300, pt3.Y, 0);
                var pt8 = new Point3d(pt7.X + 150, pt4.Y, 0);
                var pt9 = new Point3d(pt8.X + 150, pt5.Y, 0);
                var pt10 = new Point3d(pt9.X + 150, pt6.Y, 0);
                var pt11 = new Point3d(pt7.X, INDEX_START_Y + (FloorNumber - 0.075) * FloorHeight, 0);
                var pt12 = new Point3d(pt8.X, pt11.Y - 0.075 * FloorHeight, 0);
                var pt13 = new Point3d(pt9.X, pt12.Y - 0.075 * FloorHeight, 0);
                var pt14 = new Point3d(pt10.X, pt13.Y - 0.075 * FloorHeight, 0);
                var pt15X = pt11.X + 0.5 * FloorHeight;
                var pt15 = new Point3d(pt15X, pt11.Y, 0);
                var pt16 = new Point3d(pt15.X, pt12.Y, 0);
                var pt17 = new Point3d(pt15.X, pt13.Y, 0);
                var pt18 = new Point3d(pt15.X, pt14.Y, 0);

                            
                BranchPipes.Add(new Line(pt374, pt7));
                BranchPipes.Add(new Line(pt484, pt8));
                BranchPipes.Add(new Line(pt594, pt9));
                BranchPipes.Add(new Line(pt6104, pt10));

                BranchPipes.Add(new Line(pt7, pt11));
                BranchPipes.Add(new Line(pt8, pt12));
                BranchPipes.Add(new Line(pt9, pt13));
                BranchPipes.Add(new Line(pt10, pt14));
                BranchPipes.Add(new Line(pt11, pt15));
                BranchPipes.Add(new Line(pt12, pt16));
                BranchPipes.Add(new Line(pt13, pt17));
                BranchPipes.Add(new Line(pt14, pt18));


                WaterPipeInterrupted = new List<Point3d>();//水管阻断位置列表
                WaterPipeInterrupted.Add(pt15);
                WaterPipeInterrupted.Add(pt16);
                WaterPipeInterrupted.Add(pt17);
                WaterPipeInterrupted.Add(pt18);

                if (NoValve)
                {
                    PressureReducingValveSite = new Point3d(pt2.X, (pt231.Y + pt232.Y) / 2, 0);//无减压阀的截止阀位置
                }
                else
                {
                    PressureReducingValveSite = pt231;//减压阀位置
                }


                AutoExhaustValveSite = pt1;
                if (HasFlushFaucet) //有冲洗龙头
                {
                    var pt19 = new Point3d(pt2.X, pt6.Y - 0.1 * FloorHeight, 0);
                    var pt19201 = new Point3d(pt371.X, pt19.Y, 0);
                    var pt19202 = new Point3d(pt372.X, pt19.Y, 0);
                    var pt19203 = new Point3d(pt373.X, pt19.Y, 0);
                    var pt19204 = new Point3d(pt374.X, pt19.Y, 0);
                    var pt20 = new Point3d(pt10.X + 150, pt19.Y, 0);
                    var pt21 = new Point3d(pt20.X, pt20.Y + 0.45 * FloorHeight, 0);

                    BranchPipes.Add(new Line(pt6, pt19));
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
            else
            {
                var pt7 = new Point3d(pt374.X + 150, pt3.Y, 0);
                var pt8 = new Point3d(pt7.X - 150, pt4.Y, 0);
                var pt9 = new Point3d(pt8.X - 150, pt5.Y, 0);
                var pt10 = new Point3d(pt9.X - 150, pt6.Y, 0);
                var pt11 = new Point3d(pt7.X, pt10.Y - 0.125 * FloorHeight, 0);
                var pt12 = new Point3d(pt8.X, pt11.Y, 0);
                var pt13 = new Point3d(pt9.X, pt11.Y, 0);
                var pt14 = new Point3d(pt10.X, pt11.Y, 0);

                BranchPipes.Add(new Line(pt374, pt7));
                BranchPipes.Add(new Line(pt484, pt8));
                BranchPipes.Add(new Line(pt594, pt9));
                BranchPipes.Add(new Line(pt6104, pt10));

                BranchPipes.Add(new Line(pt7, pt11));
                BranchPipes.Add(new Line(pt8, pt12));
                BranchPipes.Add(new Line(pt9, pt13));
                BranchPipes.Add(new Line(pt10, pt14));

                if (NoValve)
                {
                    PressureReducingValveSite = new Point3d(pt2.X, (pt231.Y + pt232.Y) / 2, 0);//无减压阀的截止阀位置
                }
                else
                {
                    PressureReducingValveSite = pt231;//减压阀位置
                }


                AutoExhaustValveSite = pt1;


                WaterPipeInterrupted = new List<Point3d>();//水管阻断位置列表
                WaterPipeInterrupted.Add(pt11);
                WaterPipeInterrupted.Add(pt12);
                WaterPipeInterrupted.Add(pt13);
                WaterPipeInterrupted.Add(pt14);




            }
            
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


        public void Draw(Point3d basePt, Matrix3d mat)
        {
            throw new NotImplementedException();
        }

        public void Draw(Point3d basePt)
        {
            throw new NotImplementedException();
        }

        public void DrawPipes()
        {


        }
    }


    public class CleaningToolsSystem//卫生洁具系统
    {
        private int FloorNumber;//楼层号
        private int PartNumber;//分区号
        private int[] CleaningTools;//卫生洁具数组

        public CleaningToolsSystem(int floorNumber, int partNumber, int[] cleaningTools)
        {
            FloorNumber = floorNumber;
            PartNumber = partNumber;
            CleaningTools = cleaningTools;
        }
    }


    public class FloorZone
    {
        private Point3d StartPt { get; set; }
        private Point3d EndPt { get; set; }
        private double Line1X { get; set; }
        private double Line2X { get; set; }
        private double Line3X { get; set; }
        public FloorZone(Point3d startPt, Point3d endPt, double line1X, double line2X, double line3X)
        {
            StartPt = startPt;
            EndPt = endPt;
            Line1X = line1X;
            Line2X = line2X;
            Line3X = line3X;
        }
        public Point3d[] CreatePolyLine(double X1, double X2, double Y1, double Y2)
        {
            var ptls = new Point3d[5];
            ptls[0] = new Point3d(X1, Y1, 0);
            ptls[1] = new Point3d(X2, Y1, 0);
            ptls[2] = new Point3d(X2, Y2, 0);
            ptls[3] = new Point3d(X1, Y2, 0);
            ptls[4] = new Point3d(X1, Y1, 0);
            return ptls;
        }

        public List<Point3dCollection> CreateRectList()
        {
            var rectls = new List<Point3dCollection>();

            var rect1 = CreatePolyLine(StartPt.X, Line1X, StartPt.Y, EndPt.Y);
            var rect2 = CreatePolyLine(Line1X, Line2X, StartPt.Y, EndPt.Y);
            var rect3 = CreatePolyLine(Line2X, Line3X, StartPt.Y, EndPt.Y);
            var rect4 = CreatePolyLine(Line3X, EndPt.X, StartPt.Y, EndPt.Y);

            rectls.Add(new Point3dCollection(rect1));
            rectls.Add(new Point3dCollection(rect2));
            rectls.Add(new Point3dCollection(rect3));
            rectls.Add(new Point3dCollection(rect4));

            return rectls;
        }
    }
}

