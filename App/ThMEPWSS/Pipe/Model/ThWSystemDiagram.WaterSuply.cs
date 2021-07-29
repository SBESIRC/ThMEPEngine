using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.Pipe.Service;
using ThCADCore.NTS;
using NFox.Cad;
using ThMEPEngineCore.Model.Common;
using ThMEPEngineCore.Model;
using ThMEPWSS.Diagram.ViewModel;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using System.Text.RegularExpressions;
using System.Windows;

namespace ThMEPWSS.Pipe.Model
{
    public class WaterSuplyBlockNames
    {
        public const string CheckValve = "截止阀";
        public const string AutoExhaustValve = "自动排气阀系统1";
        public const string PressureReducingValve = "减压阀";
        public const string VacuumBreaker = "真空破坏器";
        public const string WaterMeter = "水表1";
        public const string WaterPipeInterrupted = "水管中断";
        public const string WaterTap = "水龙头1";
        public const string Elevation = "标高";
        public const string PipeDiameter = "给水管径100";
        public const string PRValveDetail = "减压阀详图";
        public const string FloorFraming = "楼层框定";
        public const string Casing = "套管系统";
    }


    public class WaterSuplyUtils
    {
        public static string WaterSuplyBlockFilePath
        {
            get
            {
                return ThCADCommon.WSSDwgPath();
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
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.Elevation));//标高
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PipeDiameter));//给水管经100
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PRValveDetail));//减压阀详图
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.Casing));//套管系统
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
        public Line CreateLine(double indexStartX, double indexStartY, double floorLength)  
        {
            var pt1 = new Point3d(indexStartX, indexStartY + (FloorNumber -1) * FloorHeight, 0);
            var pt2 = new Point3d(indexStartX + floorLength, indexStartY + (FloorNumber - 1) * FloorHeight, 0);
            var line1 = new Line(pt1, pt2);

            return line1;
        }

        public void DrawStorey(int i, int floorNums, double indexStartX, double indexStartY, double floorLength)
        {
            using AcadDatabase acadDatabase = AcadDatabase.Active();  //要插入图纸的空间
            var line1 = CreateLine(indexStartX, indexStartY, floorLength);
            line1.LayerId = DbHelper.GetLayerId("W-NOTE");
            acadDatabase.CurrentSpace.Add(line1);
            DBText textFirst = new DBText
            {
                Position = new Point3d(indexStartX + 1500, indexStartY + i * FloorHeight + 100, 0),
                Height = 350
            };

            if (i < floorNums)
            {
                textFirst.TextString = Convert.ToString(i + 1) + "F";
                textFirst.LayerId = DbHelper.GetLayerId("W-NOTE");
                textFirst.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                textFirst.WidthFactor = 0.7;
            }
            else
            {
                textFirst.TextString = "RF";
                textFirst.LayerId = DbHelper.GetLayerId("W-NOTE");
                textFirst.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                textFirst.WidthFactor = 0.7;
            }

            acadDatabase.CurrentSpace.Add(textFirst);
            var attNameValues = new Dictionary<string, string>() { { "标高", "X.XX" } };
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
            new Point3d(indexStartX, indexStartY + i * FloorHeight, 0), new Scale3d(1, 1, 1), 0, attNameValues);
        }
    }


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

        public List<Line> CreatePipeLine(double indexStartX, double indexStartY, double FloorHeight)
        {
            var lineList = new List<Line>();
            var pt1 = new Point3d(indexStartX + PipeOffset_X, indexStartY - 300, 0);
            var pt2 = new Point3d();
            if(Higheststorey == 1)
            {
                pt2 = new Point3d(pt1.X, indexStartY + Higheststorey * FloorHeight - 0.175 * FloorHeight, 0);
                var pt121 = new Point3d(pt1.X, pt1.Y + 140 + 300, 0);
                var pt122 = new Point3d(pt1.X, pt121.Y + 420, 0);
                
                lineList.Add(new Line(pt1, pt121));
                lineList.Add(new Line(pt122, pt2));
            }
            else if (Higheststorey > 5)
            {
                pt2 = new Point3d(pt1.X, indexStartY + Higheststorey * FloorHeight - 0.175 * FloorHeight, 0);
                var pt121 = new Point3d(pt1.X, pt1.Y + 2 * FloorHeight + 140 + 300, 0);
                var pt122 = new Point3d(pt1.X, pt121.Y + 420, 0);
                var pt123 = new Point3d(pt1.X, indexStartY + (Higheststorey - 2) * FloorHeight + 140, 0);
                var pt124 = new Point3d(pt1.X, pt123.Y + 420, 0);
                
                lineList.Add(new Line(pt1, pt121));
                lineList.Add(new Line(pt122, pt123));
                lineList.Add(new Line(pt124, pt2));
            }
            else
            {
                pt2 = new Point3d(pt1.X, indexStartY + Higheststorey * FloorHeight - 0.175 * FloorHeight, 0);
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

        public void DrawPipeLine(int i, double indexStartX, double indexStartY, double FloorHeight, int PipeNums)
        {
            using AcadDatabase acadDatabase = AcadDatabase.Active();  //要插入图纸的空间
            var PipeLine = CreatePipeLine(indexStartX, indexStartY, FloorHeight);
            foreach (var line1 in PipeLine)
            {
                acadDatabase.CurrentSpace.Add(line1);
            }

            //绘制水管中断
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterPipeInterrupted,
            new Point3d(GetPipeX(), indexStartY - 300, 0), new Scale3d(1, 1, 1), Math.PI * 3 / 2);

            //if (!PipeNumber.Contains("JGL"))
            {
                for (int j = PipeUnits.Count - 1; j >= 0; j--)
                {
                    //管径图样插入 (DN50)
                    if (j != PipeUnits.Count - 1)
                    {
                        if (PipeUnits[j].GetPipeDiameter().Equals(PipeUnits[j + 1].GetPipeDiameter()))
                        {
                            continue;
                        }
                    }
                    if (PipeUnits[j].GetPipeDiameter() != "")
                    {
                        var Position = new Point3d(GetPipeX(), indexStartY + FloorHeight * (j + 1) - 700 - FloorHeight/3, 0);
                        var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-DIMS", WaterSuplyBlockNames.PipeDiameter,
                        Position, new Scale3d(1, 1, 1), Math.PI / 2);
                        objID.SetDynBlockValue("可见性", PipeUnits[j].GetPipeDiameter());
                    }
                }

                //绘制立管起点
                var ptLs = new Point3d[3];
                ptLs[0] = new Point3d(GetPipeX(), indexStartY - 300, 0);
                ptLs[1] = new Point3d(GetPipeX(), indexStartY - 1000 - 500 * i, 0);
                ptLs[2] = new Point3d(GetPipeX() + 5500 + 600 * i, ptLs[1].Y, 0);
                var polyLine = new Polyline3d(0, new Point3dCollection(ptLs), false)
                {
                    LayerId = DbHelper.GetLayerId("W-WSUP-NOTE")
                };
                acadDatabase.CurrentSpace.Add(polyLine);

                var textString = "";
                if (PipeNumber.Contains("JGL"))
                {
                    textString = "接自市政给水管DN65(X.XXMPa)";
                }
                else
                {
                    textString = "接自加压" + Convert.ToString(i) + "区生活给水管" + PipeUnits[0].GetPipeDiameter() + "(X.XXMPa)";
                }
                    DBText text = new DBText
                {
                    Position = new Point3d(ptLs[1].X + 600 * i + 50, ptLs[1].Y + 50, 0),
                    Height = 350,
                    WidthFactor = 0.7,
                    
                    TextString = textString,
                    LayerId = DbHelper.GetLayerId("W-WSUP-NOTE"),
                    TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3")
                };
                acadDatabase.CurrentSpace.Add(text);

                //绘制立管编号 J1L1 J2L2 J3L3       2F统一标注
                var ptPipeNumLs = new Point3d[3];
                ptPipeNumLs[0] = new Point3d(GetPipeX() - 1300 - (PipeNums - i - 1) * 600, indexStartY + FloorHeight + 200 + (PipeNums - i - 1) * 450, 0);
                ptPipeNumLs[1] = new Point3d(ptPipeNumLs[0].X + 1100, ptPipeNumLs[0].Y, 0);
                ptPipeNumLs[2] = new Point3d(GetPipeX(), ptPipeNumLs[0].Y - 200 - (PipeNums - i - 1) * 600, 0);
                var PipePolyLine = new Polyline3d(0, new Point3dCollection(ptPipeNumLs), false)
                {
                    LayerId = DbHelper.GetLayerId("W-NOTE")
                };
                acadDatabase.CurrentSpace.Add(PipePolyLine);

                DBText text1 = new DBText
                {
                    Position = ptPipeNumLs[0],
                    Height = 350,
                    WidthFactor = 0.7,
                    TextString = PipeNumber,
                    LayerId = DbHelper.GetLayerId("W-WSUP-NOTE"),
                    TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3")
                };
                acadDatabase.CurrentSpace.Add(text1);

                //for (int j = 1; j <= i; j++)//越是低层的立管，标注越少
                //for (int j = 1; j <= 1; j++)//越是低层的立管，标注越少
                if(i<0)
                {
                    int j = 1;
                    //绘制立管编号 J1L1 J2L2 J3L3
                    var ptPipeNumLsj = new Point3d[3];
                    ptPipeNumLsj[0] = new Point3d(GetPipeX() - 1300 - (PipeNums - i - 1) * 600, indexStartY + FloorHeight * (HighStoreyList[j] - 1) + 200 + (PipeNums - i - 1) * 450, 0);
                    ptPipeNumLsj[1] = new Point3d(ptPipeNumLsj[0].X + 1100, ptPipeNumLsj[0].Y, 0);
                    ptPipeNumLsj[2] = new Point3d(GetPipeX(), ptPipeNumLsj[0].Y - 200 - (PipeNums - i - 1) * 600, 0);
                    var PipePolyLinej = new Polyline3d(0, new Point3dCollection(ptPipeNumLsj), false);
                    PipePolyLinej.LayerId = DbHelper.GetLayerId("W-NOTE");
                    acadDatabase.CurrentSpace.Add(PipePolyLinej);

                    DBText textj = new DBText
                    {
                        Position = new Point3d(ptPipeNumLsj[0].X, ptPipeNumLsj[0].Y + 30, 0),
                        Height = 350,
                        WidthFactor = 0.7,
                        TextString = PipeNumber,
                        LayerId = DbHelper.GetLayerId("W-WSUP-NOTE"),
                        TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3")
                    };
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
                        DBText simpleNumber1 = new DBText
                        {
                            Position = new Point3d(PipeLine[j].EndPoint.X, PipeLine[j].EndPoint.Y - 150, 0),
                            Height = 350,
                            WidthFactor = 0.7
                        };
                        simpleNumber1.Rotate(PipeLine[j].EndPoint, Math.PI / 2);
                        if (PipeNumber.Length > 2)
                        {
                            simpleNumber1.TextString = PipeNumber.Substring(0, 2);
                        }
                        else
                        {
                            simpleNumber1.TextString = PipeNumber;
                        }
                        simpleNumber1.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
                        simpleNumber1.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                        acadDatabase.CurrentSpace.Add(simpleNumber1);
                    }
                }
            }
        }
    }


    public class ThWSSDPipeUnit  //竖管单元
    {
        private string PipeDiameter { get; set; }
        private int FloorNumber { get; set; }

        public ThWSSDPipeUnit(string pipeDiameter, int floorNumber)  //构造函数
        {
            PipeDiameter = pipeDiameter;
            FloorNumber = floorNumber;
        }

        public string GetPipeDiameter()
        {
            return PipeDiameter;
        }
    }

    public class PipeCompute  //管径计算
    {
        private double U0 { get; set; }//用水总当量/分区内住户总数
        private double Ng { get; set; }//用水总当量/分区内住户总数

        public PipeCompute(double u0, double ng)
        {
            Ng = ng;//给水当量数
            U0 = u0;//平均出流概率
        }

        public string PipeDiameterCompute()
        {   
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
                        if (FlowRate <= 1.2)
                        {
                            return key;
                        }
                        break;
                    default:
                        if (FlowRate <= 1.5)
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
        private Point3d WaterTapSite{ get; set; } //水龙头位置
        private Point3d PRValveDetailSite { get; set; } //减压阀详图位置
        private List<double []> BlockSize { get; set; } //模型尺寸
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
            else if(Households[AreaIndex] == 0)//没有住户不添加支管
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
            Point3d pt232;

            if (NoValve)
            {
                pt232 = new Point3d(pt2.X, pt231.Y - 0.5 * BlockSize[1][0], 0);
            }
            else
            {
                pt232 = new Point3d(pt2.X, pt231.Y - 0.7 * BlockSize[0][0], 0);
            }
            var pt3 = new Point3d(pt2.X, pt232.Y - 0.05 * FloorHeight, 0);
            TextSite = new Point3d(pt3.X - BlockSize[0][1]/2 + 50, IndexStartY + FloorHeight * FloorNumber - 700 - FloorHeight/3, 0);//文字标注
            var pt371 = new Point3d(pt3.X + 225, pt3.Y, 0);
            var pt372 = new Point3d(pt371.X + 0.5 * BlockSize[1][0], pt3.Y, 0);
            var pt373 = new Point3d(pt372.X + 75, pt3.Y, 0);
            var pt374 = new Point3d(pt373.X + 0.5 * BlockSize[2][0], pt3.Y, 0);
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
                var pt4 = new Point3d(pt2.X, pt3.Y - i * 0.075 * FloorHeight, 0);
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
                if(i == Households[AreaIndex] -1)
                {
                    BranchPipes.Add(new Line(pt3, pt4));
                }
            }
            
            
            if (HasFlushFaucet) //有冲洗龙头
            {
                double pt19Y = 0;
                if(Households[AreaIndex] > 6)
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
                if(LayingMethod == 0)
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

            var text1 = new DBText
            {
                Height = 350,
                WidthFactor = 0.7,
                Position = new Point3d(pt3.X + 50, pt3.Y, 0),
                TextString = "自动排气阀DN20，余同",
                LayerId = DbHelper.GetLayerId("W-NOTE"),
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3")
            };
            acadDatabase.CurrentSpace.Add(text1);
            //
            //text1.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");

            var text2 = new DBText
            {
                Height = 350,
                WidthFactor = 0.7,
                Position = new Point3d(pt3.X + 50, pt3.Y - 350, 0),
                TextString = "排气阀贴板底敷设",
                LayerId = DbHelper.GetLayerId("W-NOTE"),
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3")
            };
            acadDatabase.CurrentSpace.Add(text2);
            //text2.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
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
            var pt2 = new Point3d(pt1.X, GetWaterPipeInterrupted()[0].Y + 500, 0);
            var pt3 = new Point3d(pt2.X + 3700, pt2.Y, 0);
            var line12 = new Line(pt1, pt2);
            var line23 = new Line(pt2, pt3);
            line12.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
            line23.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
            acadDatabase.CurrentSpace.Add(line12);
            acadDatabase.CurrentSpace.Add(line23);

            var text1 = new DBText
            {
                Height = 350,
                WidthFactor = 0.7,
                Position = new Point3d(pt2.X + 50, pt2.Y, 0),
                TextString = "DNXX×X+DNXX×X（余同）",
                LayerId = DbHelper.GetLayerId("W-NOTE"),
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3")
            };

            acadDatabase.CurrentSpace.Add(text1);
            //text1.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");

            if (LayingMethod == 0)
            {
                var text2 = new DBText
                {
                    Height = 350,
                    WidthFactor = 0.7,
                    Position = new Point3d(pt2.X + 50, pt2.Y - 350, 0),
                    TextString = "XXXX敷设，接至户内给水管",
                    LayerId = DbHelper.GetLayerId("W-NOTE"),
                    TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3")
                };
                acadDatabase.CurrentSpace.Add(text2);
                //text2.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
            }
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


    public class CleaningToolsSystem
    {
        private readonly int FloorNumber;//楼层号
        private readonly int PartNumber;//分区号
        private readonly int HouseholdNums;//住户数
        private readonly int[] CleaningTools;//卫生洁具数组

        public CleaningToolsSystem(int floorNumber, int partNumber, int householdNums, int[] cleaningTools)
        {
            FloorNumber = floorNumber;
            PartNumber = partNumber;
            CleaningTools = cleaningTools;
            HouseholdNums = householdNums;
        }

        public int[] GetCleaningTools()
        {
            return CleaningTools;
        }

        public int GetHouseholdNums()
        {
            return HouseholdNums;
        }

        public int GetFloorNumber()
        {
            return FloorNumber;
        }
    }


    public class FloorZone
    {
        private Point3d StartPt { get; set; }
        private Point3d EndPt { get; set; }
        private List<double> LineXList { get; set; }
        public FloorZone(Point3d startPt, Point3d endPt, List<double> lineXList)
        {
            StartPt = startPt;
            EndPt = endPt;
            LineXList = lineXList;
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
            if(LineXList.Count == 0)
            {
                Point3d[] rect;
                rect = CreatePolyLine(StartPt.X, EndPt.X, StartPt.Y, EndPt.Y);
                rectls.Add(new Point3dCollection(rect));
                return rectls;
            }
            for(int i = 0; i < LineXList.Count + 1; i++)
            {
                Point3d[] rect;
                if (i == 0)
                {
                    rect = CreatePolyLine(StartPt.X, LineXList[i], StartPt.Y, EndPt.Y);
                }
                else if (i == LineXList.Count)
                {
                    rect = CreatePolyLine(LineXList[i-1], EndPt.X, StartPt.Y, EndPt.Y);
                }
                else
                {
                    rect = CreatePolyLine(LineXList[i-1], LineXList[i], StartPt.Y, EndPt.Y);
                }
                rectls.Add(new Point3dCollection(rect));
            }
            return rectls;
        }
    }


    //常用的计算工具
    public static class ThWCompute
    {
        public static int[,] CreateZerosArray(int m, int n)//创建全0的二维数组
        {
            var result = new int[m, n];
            for (int i = 0; i < m; i++)//遍历楼层
            {
                for (int j = 0; j < n; j++)//遍历分区
                {
                    result[i, j] = 0;
                }
            }
            return result;
        }

        public static int[] CreateZerosArray(int m)//创建全0的二维数组
        {
            var result = new int[m];
            for (int i = 0; i < m; i++)//遍历楼层
            {              
                    result[i] = 0;               
            }
            return result;
        }

        public static double InnerProduct(int[] Array1, double[] Array2)//求两个数组的內积
        {
            double result = 0;
            for (int i = 0; i < Array1.Length; i++)
            {
                result += Array1[i] * Array2[i];
            }
            return result;
        }

        public static List<int[]> CountKitchenNums(List<List<Point3dCollection>> floorAreaList, Point3dCollection selectArea, List<List<int>> floorList, int FloorNumbers)
        {
            using var acadDatabase = AcadDatabase.Active();
            //统计厨房数
            //创建厨房识别引擎
            var engineKitchen = new ThDB3RoomMarkRecognitionEngine();
            engineKitchen.Recognize(acadDatabase.Database, selectArea);//厨房识别
            var ele = engineKitchen.Elements;
            var rooms = ele.Where(e => (e as ThIfcTextNote).Text.Equals("厨房")).Select(e => (e as ThIfcTextNote).Geometry);

            var kitchenIndex = new ThCADCoreNTSSpatialIndex(rooms.ToCollection());
            var households = new int[floorAreaList.Count, floorAreaList[0].Count];
            for (int i = 0; i < floorAreaList.Count; i++)//遍历每个楼层
            {
                for (int j = 0; j < floorAreaList[0].Count; j++)
                {
                    households[i, j] = kitchenIndex.SelectCrossingPolygon(floorAreaList[i][j]).Count;
                }
            }

            var floorKitchenNumList = CreateZerosArray(FloorNumbers, floorAreaList[0].Count);
            for (int i = 0; i < floorList.Count; i++)
            {
                foreach (var f in floorList[i])

                {
                    for (int j = 0; j < floorAreaList[0].Count; j++)
                    {
                        floorKitchenNumList[f - 1, j] = households[i, j];
                    }
                }
            }
            var fHouseNumList = new List<int[]>();
            for (int i = 0; i < floorKitchenNumList.Length / floorAreaList[0].Count; i++)
            {
                var house = new int[floorAreaList[0].Count];
                for (int j = 0; j < floorAreaList[0].Count; j++)
                {
                    house[j] = floorKitchenNumList[i, j];
                }
                fHouseNumList.Add(house);
            }
            return fHouseNumList;
        }

        public static List<List<CleaningToolsSystem>> CountCleanToolNums(List<List<Point3dCollection>> floorAreaList, List<int[]> households, List<List<int>> floorList, Point3dCollection selectArea, List<int> notExistFloor)
        {
            using var acadDatabase = AcadDatabase.Active();
            //统计卫生洁具数
            var engine = new Engine.ThWCleanToolsRecongnitionEngine();
            engine.Recognize(acadDatabase.Database, selectArea);
            var allCleanToolsInSelectedArea = engine.Datas.Select(d => d.Geometry).ToCollection();
            var allCleanToolsSpatialIndex = new ThCADCoreNTSSpatialIndex(allCleanToolsInSelectedArea);

            var CleanToolList = new List<List<CleaningToolsSystem>>();
            for (int i = 0; i < floorAreaList.Count; i++)//遍历每个楼层块
            {
                foreach (var f in floorList[i])//遍历每个楼层
                {
                    var CleanTools = new List<CleaningToolsSystem>();
                    for (int j = 0; j < floorAreaList[0].Count; j++)//遍历楼层的每个区域
                    {
                        var cleanToolsInSubArea = allCleanToolsSpatialIndex.SelectCrossingPolygon(floorAreaList[i][j]);
                        var allBlockNames = engine.Datas.Select(ct => ct.Data as string);
                        var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                        foreach (var ct in cleanToolsInSubArea)
                        {
                            var ctBr = ct as BlockReference;
                            cleanTools[ThCleanToolsManager.CleanToolIndex(ctBr.Name)] += 1;
                        }
                        var CleanTool = new CleaningToolsSystem(f, j, households[f - 1][j], cleanTools);
                        CleanTools.Add(CleanTool);
                    }
                    CleanToolList.Add(CleanTools);
                }
            }
            foreach (var nf in notExistFloor)
            {
                var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                var CleanTools = new List<CleaningToolsSystem>();
                for (int j = 0; j < floorAreaList[0].Count; j++)//遍历楼层的每个区域
                {
                    CleanTools.Add(new CleaningToolsSystem(nf, j, 0, cleanTools));
                }
                CleanToolList.Add(CleanTools);
            }
            CleanToolList = CleanToolList.OrderBy(l => l.First().GetFloorNumber()).ToList();
            return CleanToolList;
        }

        //创建楼层列表
        public static List<ThWSSDStorey> CreateStoreysList(int FloorNumbers, double FloorHeight, List<int> FlushFaucet, List<int> NoPRValve, List<int[]> households)
        {
            var StoreyList = new List<ThWSSDStorey>();
            for (int i = 0; i < FloorNumbers; i++)
            {
                bool hasFlushFaucet = FlushFaucet.Contains(i+1);  //有冲洗龙头为true
                bool noValve = NoPRValve.Contains(i + 1);  //无减压阀为true                      
                //楼层初始化
                var storey = new ThWSSDStorey(i + 1, FloorHeight, hasFlushFaucet, noValve, households[i]);
                StoreyList.Add(storey);
            }
            var zeroHouse = CreateZerosArray(households[0].Length);
            StoreyList.Add(new ThWSSDStorey(FloorNumbers + 1, FloorHeight, false, false, zeroHouse));

            return StoreyList;
        }

        public static List<ThWSuplySystemDiagram> CreatePipeSystem(ref List<double[]> NGLIST, ref List<double[]> U0LIST, List<int> lowestStorey,
            List<int> highestStorey, double PipeOffset_X, List<List<CleaningToolsSystem>> floorCleanToolList, int areaIndex, double PipeGap,
            double[] WaterEquivalent, DrainageSetViewModel setViewModel, double T, int maxHouseholdNums, List<string> pipeNumber)
        {
            var QL = setViewModel.MaxDayQuota;  //最高日用水定额 QL
            var Kh = setViewModel.MaxDayHourCoefficient;  //最高日小时变化系数  Kh
            var m = setViewModel.NumberOfHouseholds;   //每户人数  m

            var PipeSystem = new List<ThWSuplySystemDiagram>();// 创建竖管系统列表

            for (int i = 0; i < lowestStorey.Count; i++)  //对于每一根竖管 i 
            {
                //生成竖管对象并添加至竖管系统列表
                PipeSystem.Add(new ThWSuplySystemDiagram(pipeNumber[i], lowestStorey[i], highestStorey[i], PipeOffset_X + i * PipeGap, highestStorey));
                double[] NgList = new double[highestStorey[i]];//每层楼的当量总数
                double[] NgTotalList = new double[highestStorey[i]];//每层楼的当量总数
                double[] U0List = new double[highestStorey[i]];//每层楼的出流概率
                double[] U0aveList = new double[highestStorey[i]];//每层楼的平均出流概率，用于立管计算

                for (int j = highestStorey[i] - 1; j >= lowestStorey[i] - 1; j--)
                {
                    var toolNums = floorCleanToolList[j][areaIndex].GetCleaningTools();//当前层的卫生洁具数
                    var householdNum = floorCleanToolList[j][areaIndex].GetHouseholdNums();
                    if (householdNum == 0)
                    {
                        householdNum = maxHouseholdNums;
                    }

                    NgList[j] = InnerProduct(toolNums, WaterEquivalent) / householdNum;
                    if(Math.Abs(NgList[j]) < 1e-6)
                    {
                        U0List[j] = 0;
                    }
                    else
                    {
                        U0List[j] = 100 * QL * m * Kh / (0.2 * NgList[j] * T * 3600);
                    }                               
                }
                for (int j = lowestStorey[i] - 2; j >= 0; j--)
                {
                    NgList[j] = 0;
                    U0List[j] = 0;
                }
                for (int j = lowestStorey[i] - 1; j < highestStorey[i]; j++)// 对于竖管 i 的第 j 个竖管单元(即第 j 层)
                {
                    U0aveList[j] = 0;
                    NgTotalList[j] = 0;
                    for (int k = j; k < highestStorey[i]; k++)
                    {
                        U0aveList[j] += U0List[k] * NgList[k];
                        NgTotalList[j] += NgList[k];
                    }
                    if(Math.Abs(NgTotalList[j]) > 1e-6)
                    {
                        U0aveList[j] /= NgTotalList[j];
                    }
                }
                for (int j = 0; j < lowestStorey[i] - 1; j++)// 对于竖管 i 的第 j 个竖管单元(即第 j 层)
                {
                    U0aveList[j] = U0aveList[lowestStorey[i] - 1];
                    NgTotalList[j] = NgTotalList[lowestStorey[i] - 1];
                }
                for (int j = 0; j < highestStorey[i]; j++)
                {
                    var pipeCompute = new PipeCompute(U0aveList[j], NgTotalList[j]);
                    var DN = pipeCompute.PipeDiameterCompute();
                    PipeSystem[i].PipeUnits.Add(new ThWSSDPipeUnit(DN, j));
                }
                NGLIST.Add(NgTotalList);
                U0LIST.Add(U0aveList);
            }

            return PipeSystem;
        }

        public static double[] GetBlockSize(BlockTable bt, string BlockValue)//获取block尺寸
        {
            if (bt.Has(BlockValue))
            {
                var objId = bt[BlockValue];//获取objectID
                BlockReference br = new BlockReference(new Point3d(0, 0, 0), objId);//?
                var extent = br.GeometricExtents;
                var Length = extent.MaxPoint.X - extent.MinPoint.X;
                var Hight = extent.MaxPoint.Y - extent.MinPoint.Y;
                var Size = new double[] { Length, Hight };

                return Size;
            }
            return new double[] { 0, 0 };
        }

        //创建模块尺寸列表
        public static List<double[]> CreateBlockSizeList(BlockTable bt)
        {
            var BlockSize = new List<double[]>();//减压阀 截止阀 水表 自动排气阀 尺寸
            //获取并添加 block 尺寸
            BlockSize.Add(GetBlockSize(bt, WaterSuplyBlockNames.PressureReducingValve));
            BlockSize.Add(GetBlockSize(bt, WaterSuplyBlockNames.CheckValve));
            BlockSize.Add(GetBlockSize(bt, WaterSuplyBlockNames.WaterMeter));
            BlockSize.Add(GetBlockSize(bt, WaterSuplyBlockNames.AutoExhaustValve));
            return BlockSize;
        }

        public static Point3dCollection CreatePolyLine(Point3d pt1, Point3d pt2)
        {
            var ptls = new Point3d[5];
            ptls[0] = pt1;
            ptls[1] = new Point3d(pt2.X, pt1.Y, 0);
            ptls[2] = pt2;
            ptls[3] = new Point3d(pt1.X, pt2.Y, 0);
            ptls[4] = pt1;
            var SelectedArea = new Point3dCollection(ptls);
            return SelectedArea;
        }

        //创建单层的分区列表
        public static List<Point3dCollection> CreateRectList(ThStoreys sobj)
        {
            var spt = sobj.ObjectId.GetBlockPosition();//获取楼层分割线的起始点
            var eptX = spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("宽度"));
            var eptY = spt.Y - Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("高度"));
            var LineXList = new List<double>();
            var index = 1;
            for(int i = 0; i < sobj.ObjectId.GetDynProperties().Count; i++)
            {
                if(sobj.ObjectId.GetDynProperties()[i].PropertyName.Contains("分割") && 
                    sobj.ObjectId.GetDynProperties()[i].PropertyName.Contains(" X"))
                {
                    var SplitX = Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("分割" + Convert.ToString(index) + " X"));
                    if (SplitX < 0)
                    {
                        continue;
                    }
                    if (SplitX > eptX - spt.X)
                    {
                        continue;
                    }
                    LineXList.Add(spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("分割" + Convert.ToString(index) + " X")));
                    index += 1;
                }
            }
           
            //创建楼层分区类
            var floorZone = new FloorZone(spt, new Point3d(eptX, eptY, 0), LineXList);
            var rectList = floorZone.CreateRectList();//创建楼层分区的多段线

            return rectList;
        }

        
        public static List<List<Point3dCollection>> CreateFloorAreaList(List<ThIfcSpatialElement> elements)//创建所有楼层的分区列表
        {
            using var acadDatabase = AcadDatabase.Active();
            var FloorAreaList = new List<List<Point3dCollection>>();
            foreach (var obj in elements)//遍历楼层
            {
                if (obj is ThStoreys)
                {
                    var sobj = obj as ThStoreys;
                    var br = acadDatabase.Element<BlockReference>(sobj.ObjectId);
                    if (!br.IsDynamicBlock) continue;
                    if (sobj.StoreyType.ToString().Contains("StandardStorey"))
                    {
                        var rectList = CreateRectList(sobj);
                        FloorAreaList.Add(rectList);//分区的多段线添加
                    }
                }
            }

            return FloorAreaList;
        }

        public static List<List<int>> CreateFloorNumList(List<string> FloorNum) //提取每张图纸的楼层号
        {
            //楼层号识别，将 floor 作为 ThMEPEngineCore.Model.Common.ThStorey 类进行选择
            var FNumSplit = new List<string[]>();
            foreach (var f in FloorNum)
            {
                FNumSplit.Add(f.Split(','));
            }

            var FloorNumList = new List<List<int>>();

            foreach (var f in FNumSplit)
            {
                var fiNum = new List<int>();
                for (int i = 0; i < f.Length; i++)
                {
                    if (f[i].Contains('-'))
                    {
                        var start = Convert.ToInt32(f[i].Split('-')[0]);
                        var end = Convert.ToInt32(f[i].Split('-')[1]);
                        for (int j = start; j <= end; j++)
                        {
                            var hasNum = false;
                            foreach (var fi in FNumSplit)
                            {
                                if (fi.Contains(Convert.ToString(j)))
                                {
                                    hasNum = true;
                                    break;
                                }
                            }
                            if (!hasNum)
                            {
                                fiNum.Add(j);
                            }
                        }
                    }
                    else
                    {
                        fiNum.Add(Convert.ToInt32(f[i]));
                    }
                }
                FloorNumList.Add(fiNum);
            }

            return FloorNumList;
        }

        //统计分区数
        public static int CountAreaNums(List<List<Point3dCollection>> FloorAreaList, ThCADCoreNTSSpatialIndex kitchenIndex)
        {
            int AreaNums = 0;
            var households = new int[FloorAreaList.Count, FloorAreaList[0].Count];
            for (int i = 0; i < FloorAreaList.Count; i++)
            {
                var areaNums = 0;
                for (int j = 0; j < FloorAreaList[i].Count; j++)
                {
                    households[i, j] = Convert.ToInt32(kitchenIndex.SelectCrossingPolygon(FloorAreaList[i][j]).Count >0);
                    if(households[i, j]>0)
                    {
                        ;
                    }
                    areaNums += households[i, j];
                }
                if (AreaNums < areaNums)
                {
                    AreaNums = areaNums;
                }
            }
            return AreaNums;
        }

        public static List<string> CreateTypeList(string ListType)
        {
            var strType = new List<string>();
            for (int i = 0; i <= 9; i++)
            {
                strType.Add(Convert.ToString(i));
            }
            if(ListType.Equals("str"))
            {
                strType.Add("-");
                strType.Add(",");
            }
            if (ListType.Equals("float"))
            {
                strType.Add(".");
            }

            return strType;
        }

        public static bool IsCorrectNum(string S, string ListType)
        {
            var TypeList = CreateTypeList(ListType);
            foreach(var s in S)
            {
                if(!TypeList.Contains(s.ToString()))
                {
                    return false;
                }
            }
            return true;
        }

        public static List<int> ExtractData(string floorls, string dataName)
        {
            var FlushFaucet = new List<int>();//冲洗龙头层
            if (floorls != "")
            {
                foreach (var f in floorls.Split(','))
                {
                    if (f.Contains('-'))
                    {
                        var f1 = f.Split('-')[0];
                        var f2 = f.Split('-').Last();
                        if (f1 != "" && f2 != "")
                        {
                            if (Regex.IsMatch(f1, @"^[+-]?\d*$") && Regex.IsMatch(f2, @"^[+-]?\d*$"))
                            {
                                if (Convert.ToInt32(f1) < Convert.ToInt32(f2))
                                {
                                    for (int i = Convert.ToInt32(f1); i <= Convert.ToInt32(f2); i++)
                                    {
                                        if (!FlushFaucet.Contains(i))
                                        {
                                            FlushFaucet.Add(i);
                                        }
                                    }
                                }
                                else
                                {
                                    MessageBox.Show(dataName + "输入有误，\"-\"左边数字必须小于右边");
                                    return new List<int>();
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show(dataName + "输入有误，\"-\"左右只能是数字");
                            return new List<int>();
                        }
                    }
                    else
                    {
                        if (f != "" && !FlushFaucet.Contains(Convert.ToInt32(f)))
                        {
                            FlushFaucet.Add(Convert.ToInt32(f));
                        }
                    }
                }
            }
            return FlushFaucet;
        }
    }
}




