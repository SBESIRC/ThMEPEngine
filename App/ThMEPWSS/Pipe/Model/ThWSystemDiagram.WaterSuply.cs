using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.Pipe.Service;
using ThCADCore.NTS;
using NFox.Cad;
using ThMEPEngineCore.Model.Common;
using System.Windows.Forms;
using ThMEPEngineCore.Model;
using ThMEPWSS.Diagram.ViewModel;
using Dreambuild.AutoCAD;

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
        public const string Elevation = "标高";
        public const string PipeDiameter = "给水管径100";
        public const string PRValveDetail = "减压阀详图";
    }

    public class WaterSuplyUtils
    {
        //读取供水系统模块文件的路径
        public static string WaterSuplyBlockFilePath
        {
            get
            {
                var path = Path.Combine(ThCADCommon.SupportPath(), "地上给水排水平面图模板_20210517.dwg");
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
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.Elevation));//标高
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PipeDiameter));//给水管经100
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PRValveDetail));//减压阀详图
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
        private int Households { get; set; }//每层的住户数

        public ThWSSDStorey(int floorNumber, double floorHeight, bool hasFlushFaucet, bool noPRValve, int households)
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

        public int GetHouseholds()
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
            using (AcadDatabase acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
            {
                var line1 = CreateLine(indexStartX, indexStartY, floorLength);
                acadDatabase.CurrentSpace.Add(line1);
                //文字绘制
                DBText textFirst = new DBText();
                textFirst.Position = new Point3d(indexStartX + 500, indexStartY + i * FloorHeight, 0);
                textFirst.Height = 200;
                if (i < floorNums)
                {
                    textFirst.TextString = Convert.ToString(i + 1) + "F";
                    textFirst.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                }
                else
                {
                    textFirst.TextString = "RF";
                    textFirst.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                }
                acadDatabase.CurrentSpace.Add(textFirst);
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                new Point3d(indexStartX, indexStartY + i * FloorHeight, 0), new Scale3d(1, 1, 1), 0);
            } 
        }
    }


    public class ThWSuplySystemDiagram // 竖管系统类
    {
        private int Loweststorey { get; set; }//竖管最低层
        private int Higheststorey { get; set; }//竖管最高层
        private List<int> HighStoreyList { get; set; }//竖管最高层列表
        private double PipeOffset_X { get; set; } //竖管相对于楼层的偏移量
        private double PipeX { get; set; }//竖管的X坐标
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

        public string GetPipeNumber()
        {
            return PipeNumber;
        }

        public List<Line> CreatePipeLine(double indexStartX, double indexStartY, double FloorHeight, double pipeGap)
        {
            var lineList = new List<Line>();
            var pt1 = new Point3d(indexStartX + PipeOffset_X, indexStartY - 300, 0);
            if (Higheststorey > 5)
            {
                var pt2 = new Point3d(pt1.X, indexStartY + Higheststorey * FloorHeight - 0.175 * FloorHeight, 0);
                var pt121 = new Point3d(pt1.X, pt1.Y + 2 * FloorHeight + 140, 0);
                var pt122 = new Point3d(pt1.X, pt121.Y + 420, 0);
                var pt123 = new Point3d(pt1.X, indexStartY + (Higheststorey - 2) * FloorHeight + 140, 0);
                var pt124 = new Point3d(pt1.X, pt123.Y + 420, 0);
                
                lineList.Add(new Line(pt1, pt121));
                lineList.Add(new Line(pt122, pt123));
                lineList.Add(new Line(pt124, pt2));

                foreach (var line1 in lineList)
                {
                    line1.LayerId = DbHelper.GetLayerId("W-WSUP-COOL-PIPE");
                }
            }
            else
            {
                var pt2 = new Point3d(pt1.X, indexStartY + Higheststorey * FloorHeight - 0.175 * FloorHeight, 0);
                var line1 = new Line(pt1, pt2);
                line1.LayerId = DbHelper.GetLayerId("W-WSUP-COOL-PIPE");
                lineList.Add(new Line(pt1, pt2));

            }
            PipeX = pt1.X;

            return lineList;
        }

        public void DrawPipeLine(int i, double indexStartX, double indexStartY, double FloorHeight, double pipeGap, int PipeNums)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
            {
                var PipeLine = CreatePipeLine(indexStartX, indexStartY, FloorHeight, pipeGap * i);
                foreach(var line1 in PipeLine)
                {
                    acadDatabase.CurrentSpace.Add(line1);
                }

                //绘制水管中断
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterPipeInterrupted,
                new Point3d(GetPipeX(), indexStartY - 300, 0), new Scale3d(1, 1, 1), Math.PI * 3 / 2);


                if(!PipeNumber.Contains("JGL"))
                {
                    for (int j = 0; j < PipeUnits.Count; j++)
                    {
                        //管径图样插入 (DN50)
                        if (j != 0 && j != PipeUnits.Count - 1)
                        {
                            if (PipeUnits[j].GetPipeDiameter().Equals(PipeUnits[j - 1].GetPipeDiameter()) && PipeUnits[j].GetPipeDiameter().Equals(PipeUnits[j + 1].GetPipeDiameter()))
                            {
                                continue;
                            }
                        }

                        var Position = new Point3d(GetPipeX(), indexStartY + FloorHeight * j + 1000, 0);
                        var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-DIMS", WaterSuplyBlockNames.PipeDiameter,
                        Position, new Scale3d(1, 1, 1), Math.PI / 2);
                        objID.SetDynBlockValue("可见性", PipeUnits[j].GetPipeDiameter());
                    }

                    //绘制立管起点
                    var ptLs = new Point3d[3];
                    ptLs[0] = new Point3d(GetPipeX(), indexStartY - 300, 0);
                    ptLs[1] = new Point3d(GetPipeX(), indexStartY - 1000 - 500 * i, 0);
                    ptLs[2] = new Point3d(GetPipeX() + 9000 + 600 * i, ptLs[1].Y, 0);
                    var polyLine = new Polyline3d(0, new Point3dCollection(ptLs), false);
                    polyLine.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
                    acadDatabase.CurrentSpace.Add(polyLine);

                    DBText text = new DBText();
                    text.Position = new Point3d(ptLs[1].X + 600 * i, ptLs[1].Y + 50, 0);
                    text.Height = 350;
                    text.TextString = "接自加压" + Convert.ToString(i + 1) + "区生活给水管" + PipeUnits[0].GetPipeDiameter() + "(X.XXMPa)";
                    text.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                    acadDatabase.CurrentSpace.Add(text);
                    text.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");

                    //绘制立管编号 J1L1 J2L2 J3L3       2F统一标注
                    var ptPipeNumLs = new Point3d[3];
                    ptPipeNumLs[0] = new Point3d(GetPipeX() - 1300 - (PipeNums - i - 1) * 600, indexStartY + FloorHeight + 200 + (PipeNums - i - 1) * 450, 0);
                    ptPipeNumLs[1] = new Point3d(ptPipeNumLs[0].X + 1100, ptPipeNumLs[0].Y, 0);
                    ptPipeNumLs[2] = new Point3d(GetPipeX(), ptPipeNumLs[0].Y - 200 - (PipeNums - i - 1) * 600, 0);
                    var PipePolyLine = new Polyline3d(0, new Point3dCollection(ptPipeNumLs), false);
                    PipePolyLine.LayerId = DbHelper.GetLayerId("W-NOTE");
                    acadDatabase.CurrentSpace.Add(PipePolyLine);

                    DBText text1 = new DBText();
                    text1.Position = ptPipeNumLs[0];
                    text1.Height = 350;
                    text1.WidthFactor = 0.7;
                    text1.TextString = PipeNumber;
                    text1.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
                    text1.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                    acadDatabase.CurrentSpace.Add(text1);

                    for(int j = 1; j <= i; j++)//越是低层的立管，标注越少
                    {
                        //绘制立管编号 J1L1 J2L2 J3L3
                        var ptPipeNumLsj = new Point3d[3];
                        ptPipeNumLsj[0] = new Point3d(GetPipeX() - 1300 - (PipeNums - i - 1) * 600, indexStartY + FloorHeight * (HighStoreyList[j] - 1) + 200 + (PipeNums - i - 1) * 450, 0);
                        ptPipeNumLsj[1] = new Point3d(ptPipeNumLsj[0].X + 1100, ptPipeNumLsj[0].Y, 0);
                        ptPipeNumLsj[2] = new Point3d(GetPipeX(), ptPipeNumLsj[0].Y - 200 - (PipeNums - i - 1) * 600, 0);
                        var PipePolyLinej = new Polyline3d(0, new Point3dCollection(ptPipeNumLsj), false);
                        PipePolyLine.LayerId = DbHelper.GetLayerId("W-NOTE");
                        acadDatabase.CurrentSpace.Add(PipePolyLinej);
                        
                        DBText textj = new DBText();
                        textj.Position = ptPipeNumLsj[0];
                        textj.Height = 350;
                        textj.WidthFactor = 0.7;
                        textj.TextString = PipeNumber;
                        textj.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
                        textj.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                        acadDatabase.CurrentSpace.Add(textj);
                    }
                    
                    //绘制立管简编号 J1 J2 J3
                    for (int j = 0; j < 2; j++)
                    {
                        DBText simpleNumber1 = new DBText();
                        simpleNumber1.Position = new Point3d(PipeLine[j].EndPoint.X, PipeLine[j].EndPoint.Y - 150, 0);
                        simpleNumber1.Height = 350;
                        simpleNumber1.WidthFactor = 0.7;
                        simpleNumber1.Rotate(PipeLine[j].EndPoint, Math.PI / 2);
                        simpleNumber1.TextString = PipeNumber.Substring(0, 2);
                        simpleNumber1.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
                        simpleNumber1.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                        acadDatabase.CurrentSpace.Add(simpleNumber1);
                    }

                    //绘制分区框线
                    var ptList = new Point3d[4];
                    ptList[0] = new Point3d(PipeX - 3000, Higheststorey * FloorHeight + indexStartY + 200, 0);
                    ptList[1] = new Point3d(PipeX + 3000, Higheststorey * FloorHeight + indexStartY + 200, 0);
                    ptList[2] = new Point3d(PipeX + 3000, (Loweststorey - 1) * FloorHeight + indexStartY + 400, 0);
                    ptList[3] = new Point3d(PipeX - 3000, (Loweststorey - 1) * FloorHeight + indexStartY + 400, 0);
                    var rect = new Polyline3d(0, new Point3dCollection(ptList),true);
                    rect.LayerId = DbHelper.GetLayerId("说明");
                    acadDatabase.CurrentSpace.Add(rect);

                    var textRect = new DBText();
                    textRect.Position = new Point3d(ptList[1].X - 500, ptList[1].Y - 500, 0);
                    textRect.Height = 150;
                    textRect.WidthFactor = 0.7;
                    textRect.TextString = "分区" + Convert.ToString(i);
                    textRect.LayerId = DbHelper.GetLayerId("说明");
                    textRect.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                    acadDatabase.CurrentSpace.Add(textRect);
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
        private int Households { get; set; }//住户数
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
        public ThWSSDBranchPipe(string dn, ThWSSDStorey storey, double indexStartY, double pipeOffsetX, List<double[]> blockSize, int layingMethod)
        {
            //DN, StoreyList[i], PipeOffsetX[i], BlockSize, LAYINGMETHOD
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
            
            if (Households == 0 && HasFlushFaucet)
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
            else if(Households == 0)//没有住户不添加支管
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
            ///////////////////////////////////////////////////////
            //支管 point 初始化
            var pt1 = new Point3d(PipeOffsetX, IndexStartY + (FloorNumber - 0.175) * FloorHeight, 0);
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
            TextSite = new Point3d(pt3.X - BlockSize[0][1]/2, pt3.Y, 0);//new Point3d(pt2.X, pt3.Y, 0);//文字标注
            var pt371 = new Point3d(pt3.X + 225, pt3.Y, 0);
            var pt372 = new Point3d(pt371.X + 0.5 * BlockSize[1][0], pt3.Y, 0);
            var pt373 = new Point3d(pt372.X + 75, pt3.Y, 0);
            var pt374 = new Point3d(pt373.X + 0.5 * BlockSize[2][0], pt3.Y, 0);
            var pt7 = new Point3d();
            var pt11 = new Point3d();

            PRValveDetailSite = new Point3d(pt1.X + 3000, IndexStartY + (FloorNumber - 1) * FloorHeight + 200, 0);
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
                pt7 = new Point3d(pt374.X + 150, pt3.Y, 0);
                pt11 = new Point3d(pt7.X, pt7.Y - (0.125 + 0.1 * (Households - 1)) * FloorHeight, 0);
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

            for (int i = 1; i < Households; i++)
            {
                var pt4 = new Point3d(pt2.X, pt3.Y - i * 0.1 * FloorHeight, 0);
                var pt481 = new Point3d(pt371.X, pt4.Y, 0);
                var pt482 = new Point3d(pt372.X, pt4.Y, 0);
                var pt483 = new Point3d(pt373.X, pt4.Y, 0);
                var pt484 = new Point3d(pt374.X, pt4.Y, 0);
                var pt8 = new Point3d();
                var pt12 = new Point3d();
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
                    pt8 = new Point3d(pt7.X - i * 150, pt3.Y, 0);
                    pt12 = new Point3d(pt8.X, pt11.Y, 0);
                    WaterPipeInterrupted.Add(pt12);//第i个水管截断位置

                }
                BranchPipes.Add(new Line(pt4, pt481));
                BranchPipes.Add(new Line(pt482, pt483));
                BranchPipes.Add(new Line(pt484, pt8));
                BranchPipes.Add(new Line(pt8, pt12));
                if(i == Households - 1 && !HasFlushFaucet)
                {
                    BranchPipes.Add(new Line(pt3, pt4));
                }
            }

            if (HasFlushFaucet) //有冲洗龙头
            {
                var pt19 = new Point3d(pt3.X, pt3.Y - Households * 0.1 * FloorHeight, 0);
                var pt19201 = new Point3d(pt371.X, pt19.Y, 0);
                var pt19202 = new Point3d(pt372.X, pt19.Y, 0);
                var pt19203 = new Point3d(pt373.X, pt19.Y, 0);
                var pt19204 = new Point3d(pt374.X, pt19.Y, 0);
                var pt20 = new Point3d(pt7.X + 150 * Households, pt19.Y, 0);
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

        public void DrawBranchPipe()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                if (!(BranchPipes is null))
                {
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-DIMS", WaterSuplyBlockNames.PipeDiameter,
                    GetTextSite(), new Scale3d(1, 1, 1), Math.PI / 2);
                    objID.SetDynBlockValue("可见性", GetDN());

                    var BPipeLines = BranchPipes;
                    for (int j = 0; j < BPipeLines.Count; j++)
                    {
                        acadDatabase.CurrentSpace.Add(BPipeLines[j]);
                    }
                }
            }  
        }

        public void DrawAutoExhaustValveNote()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var pt1 = new Point3d(AutoExhaustValveSite.X, AutoExhaustValveSite.Y + BlockSize[3][1] / 4, 0);// - BlockSize[3][1]/2
                var pt2 = new Point3d(pt1.X - 450, pt1.Y - 450, 0);
                var pt3 = new Point3d(pt2.X - 3400 ,pt2.Y, 0);
                acadDatabase.CurrentSpace.Add(new Line(pt1, pt2));
                acadDatabase.CurrentSpace.Add(new Line(pt2, pt3));

                var text1 = new DBText();
                text1.Height = 350;
                text1.WidthFactor = 0.7;
                text1.Position = pt3;
                text1.TextString = "自动排气阀DN20，余同";
                text1.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                acadDatabase.CurrentSpace.Add(text1);
                text1.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");

                var text2 = new DBText();
                text2.Height = 350;
                text2.WidthFactor = 0.7;
                text2.Position = new Point3d(pt3.X, pt3.Y-350,0);
                text2.TextString = "排气阀贴板底敷设";
                text2.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                acadDatabase.CurrentSpace.Add(text2);
                text2.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
            }
        }

        public void DrawLayMethodNote()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                for(int i = 0; i < GetWaterPipeInterrupted().Count; i++)
                {
                    var pti1 = new Point3d(GetWaterPipeInterrupted()[i].X - 106 - 150, GetWaterPipeInterrupted()[i].Y - 106, 0);
                    var pti2 = new Point3d(GetWaterPipeInterrupted()[i].X + 106 - 150, GetWaterPipeInterrupted()[i].Y + 106, 0);
                    var line1 = new Line(pti1, pti2);
                    line1.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
                    acadDatabase.CurrentSpace.Add(line1);

                }
                var pt1 = new Point3d(GetWaterPipeInterrupted()[0].X - 150, GetWaterPipeInterrupted()[GetWaterPipeInterrupted().Count - 1].Y, 0);// - BlockSize[3][1]/2
                var pt2 = new Point3d(pt1.X, pt1.Y + 500, 0);
                var pt3 = new Point3d(pt2.X + 3400, pt2.Y, 0);
                acadDatabase.CurrentSpace.Add(new Line(pt1, pt2));
                acadDatabase.CurrentSpace.Add(new Line(pt2, pt3));

                var text1 = new DBText();
                text1.Height = 350;
                text1.WidthFactor = 0.7;
                text1.Position = pt2;
                text1.TextString = "DNXX×X+DNXX×X（余同）";
                text1.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");

                acadDatabase.CurrentSpace.Add(text1);
                text1.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");               

                if (LayingMethod == 0)
                {
                    var text2 = new DBText();
                    text2.Height = 350;
                    text2.WidthFactor = 0.7;
                    text2.Position = new Point3d(pt2.X, pt2.Y - 350, 0);
                    text2.TextString = "XXXX敷设，接至户内给水管";
                    text2.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                    acadDatabase.CurrentSpace.Add(text2);
                    text2.LayerId = DbHelper.GetLayerId("W-WSUP-DIMS");
                }
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
        private int FloorNumber;//楼层号
        private int PartNumber;//分区号
        private int HouseholdNums;//住户数
        private int[] CleaningTools;//卫生洁具数组

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

        public int GetPartNumber()
        {
            return PartNumber;
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

            for(int i = 0; i < LineXList.Count + 1; i++)
            {
                var rect = new Point3d[5];
                if(i == 0)
                {
                    rect = CreatePolyLine(StartPt.X, LineXList[i], StartPt.Y, EndPt.Y);
                }
                else if(i == LineXList.Count)
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


        public static double InnerProduct(int[] Array1, double[] Array2)//求两个数组的內积
        {
            double result = 0;
            for (int i = 0; i < Array1.Length; i++)
            {
                result += Array1[i] * Array2[i];
            }
            return result;
        }

        public static int[,] CountKitchenNums(List<List<Point3dCollection>> floorAreaList, Point3dCollection selectArea, List<List<int>> floorList, int FloorNumbers)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                //统计厨房数
                var engineKitchen = new ThMEPEngineCore.Engine.ThRoomMarkRecognitionEngine();//创建厨房识别引擎
                engineKitchen.Recognize(acadDatabase.Database, selectArea);//厨房识别
                var ele = engineKitchen.Elements;
                var rooms = ele.Where(e => (e as ThIfcTextNote).Text.Equals("厨房"))
                    .Select(e => (e as ThIfcTextNote).Geometry);
                var kitchenIndex = new ThCADCoreNTSSpatialIndex(rooms.ToCollection());
                var households = new int[floorAreaList.Count, floorAreaList[0].Count];
                for (int i = 0; i < floorAreaList.Count; i++)//遍历每个楼层
                {
                    for (int j = 0; j < floorAreaList[0].Count; j++)
                    {
                        households[i, j] = kitchenIndex.SelectCrossingPolygon(floorAreaList[i][j]).Count;
                    }
                }

                var floorKitchenNumList = CreateZerosArray(FloorNumbers, floorAreaList[0].Count);//new int[FloorNumbers, floorAreaList[0].Count];
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
                return floorKitchenNumList;
            }
                
        }

        public static List<List<CleaningToolsSystem>> CountCleanToolNums(List<List<Point3dCollection>> floorAreaList, int[,] households, List<List<int>> floorList, Point3dCollection selectArea, List<int> notExistFloor)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
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
                            var CleanTool = new CleaningToolsSystem(f, j, households[f - 1, j], cleanTools);
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

        }


        //创建楼层列表
        public static List<ThWSSDStorey> CreateStoreysList(int FloorNumbers, double FloorHeight, List<int> FlushFaucet, List<int> NoPRValve, int[,] households)
        {
            var StoreyList = new List<ThWSSDStorey>();
            for (int i = 0; i < FloorNumbers; i++)
            {
                bool hasFlushFaucet = FlushFaucet.Contains(i+1);  //有冲洗龙头为true
                bool noValve = NoPRValve.Contains(i + 1);  //无减压阀为true                      

                //楼层初始化
                var storey = new ThWSSDStorey(i + 1, FloorHeight, hasFlushFaucet, noValve, households[i,0]);
                StoreyList.Add(storey);
            }
            StoreyList.Add(new ThWSSDStorey(FloorNumbers + 1, FloorHeight, false, false, 0));
            return StoreyList;
        }


        public static List<ThWSuplySystemDiagram> CreatePipeSystem(ref List<double[]> NGLIST, ref List<double[]> U0LIST, List<int> lowestStorey
            , List<int> highestStorey, double PipeOffset_X, List<List<CleaningToolsSystem>> floorCleanToolList, int areaIndex, double PipeGap
            , double[] WaterEquivalent, DrainageSetViewModel setViewModel, double T, int maxHouseholdNums, List<string> pipeNumber)
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
                                
                    U0List[j] = 100 * QL * m * Kh / (0.2 * NgList[j] * T * 3600);
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
                    U0aveList[j] /= NgTotalList[j];

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


        //获取block尺寸
        public static double[] GetBlockSize(BlockTable bt, string BlockValue)
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
                    LineXList.Add(spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("分割" + Convert.ToString(index) + " X")));
                    index += 1;
                }
            }
           
            //创建楼层分区类
            var floorZone = new FloorZone(spt, new Point3d(eptX, eptY, 0), LineXList);
            var rectList = floorZone.CreateRectList();//创建楼层分区的多段线

            return rectList;
        }


        //创建所有楼层的分区列表
        public static List<List<Point3dCollection>> CreateFloorAreaList(List<ThIfcSpatialElement> elements)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var FloorAreaList = new List<List<Point3dCollection>>();
                foreach (var obj in elements)//遍历楼层
                {
                    if (obj is ThStoreys)
                    {
                        var sobj = obj as ThStoreys;
                        var br = acadDatabase.Element<BlockReference>(sobj.ObjectId);
                        if (!br.IsDynamicBlock) continue;
                        if(sobj.StoreyType.ToString().Contains("StandardStorey"))
                        {
                            var rectList = CreateRectList(sobj);
                            FloorAreaList.Add(rectList);//分区的多段线添加
                        }
                        
                    }
                }
                return FloorAreaList;
            }
        }


        //提取每张图纸的楼层号
        public static List<List<int>> CreateFloorNumList(List<string> FloorNum)
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
                    households[i, j] = Convert.ToInt32(kitchenIndex.SelectCrossingPolygon(FloorAreaList[i][j]).Count > 0);
                    areaNums += households[i, j];
                }
                if (i == 0)
                {
                    AreaNums = areaNums;
                }
                else
                {
                    if (AreaNums != areaNums)
                    {
                        MessageBox.Show("有效分割线数目不对");
                        return -1;
                    }
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

        public static double AutomaticValue(double num, double minNum, double maxNum)
        {
            if (num < minNum)
            {
                return minNum;
            }
            if (num > maxNum)
            {
                return maxNum;
            }
            return num;
        }


    }
}

