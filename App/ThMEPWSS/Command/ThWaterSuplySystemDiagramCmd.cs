using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Command
{
    public class ThWaterSuplySystemDiagramCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
        }

        public void Execute()
        {
            //楼层类的参数
            int FloorNumbers = 31; //楼层数
            int FloorHeight = 2900;  //楼层线间距 mm
            int[] FlushFaucet = { 1, 6, 11, 21, 26, 31 }; //冲洗龙头层
            int[] NoPRValve = { 12, 23, 31 }; //无减压阀层
            

            //楼板线绘制参数
            double INDEX_START_X = 43000;
            double INDEX_START_Y = -260000;
            double FLOOR_LENGTH = 20000;

            using (var acadDatabase = AcadDatabase.Active())
            {
                //楼板线生成和绘制
                var StoreyList = new List<ThWSSDStorey>();
                for (int i = 0; i < FloorNumbers; i++)
                {
                    bool hasFlushFaucet = false;  //有冲洗龙头为true
                    bool noValve = false;  //无减压阀为true
                    //判断当前层 i 是否存在冲洗龙头
                    foreach (int j in FlushFaucet)
                    {
                        if(i+1 == j)
                        {
                            hasFlushFaucet = true;
                            break;
                        }
                    }
                    //判断当前层 i 是否存在减压阀
                    foreach (int j in NoPRValve)
                    {
                        if (i+1 == j)
                        {
                            noValve = true;
                            break;
                        }
                    }

                    //楼层初始化
                    var storey = new ThWSSDStorey(i+1, FloorHeight, hasFlushFaucet, noValve);
                    StoreyList.Add(storey);
                }
                StoreyList.Add(new ThWSSDStorey(FloorNumbers + 1, FloorHeight, false, false));
                for (int i = 0; i < FloorNumbers + 1; i++)
                {
                    //楼层线绘制
                    var line1 = StoreyList[i].CreateLine(INDEX_START_X, INDEX_START_Y, FLOOR_LENGTH);
                    acadDatabase.CurrentSpace.Add(line1);
                }

                var bt = acadDatabase.Element<BlockTable>(acadDatabase.Database.BlockTableId);//创建BlockTable
                var BlockSize = new List<double[]>();//减压阀 截止阀 水表 自动排气阀
                //获取并添加减压阀尺寸
                if (bt.Has(WaterSuplyBlockNames.PressureReducingValve))
                {
                    var objId = bt[WaterSuplyBlockNames.PressureReducingValve];//获取objectID
                    BlockReference br = new BlockReference(new Point3d(0, 0, 0), objId);// ?
                    var extent = br.GeometricExtents;
                    var Length = extent.MaxPoint.X - extent.MinPoint.X;
                    var Hight = extent.MaxPoint.Y - extent.MinPoint.Y;
                    var PRValveSize = new double[] { Length, Hight };
                    BlockSize.Add(PRValveSize);
                }
                //获取并添加截止阀尺寸
                if (bt.Has(WaterSuplyBlockNames.CheckValve))
                {
                    var objId = bt[WaterSuplyBlockNames.CheckValve];
                    BlockReference br = new BlockReference(new Point3d(0, 0, 0), objId);
                    var extent = br.GeometricExtents;
                    var Length = extent.MaxPoint.X - extent.MinPoint.X;
                    var Hight = extent.MaxPoint.Y - extent.MinPoint.Y;
                    var Size = new double[] { Length, Hight };
                    BlockSize.Add(Size);
                }
                //获取并添加水表尺寸
                if (bt.Has(WaterSuplyBlockNames.WaterMeter))
                {
                    var objId = bt[WaterSuplyBlockNames.WaterMeter];
                    BlockReference br = new BlockReference(new Point3d(0, 0, 0), objId);
                    var extent = br.GeometricExtents;
                    var Length = extent.MaxPoint.X - extent.MinPoint.X;
                    var Hight = extent.MaxPoint.Y - extent.MinPoint.Y;
                    var Size = new double[] { Length, Hight };
                    BlockSize.Add(Size);
                }
                //获取并添加自动排气阀尺寸
                if (bt.Has(WaterSuplyBlockNames.AutoExhaustValve))
                {
                    var objId = bt[WaterSuplyBlockNames.AutoExhaustValve];
                    BlockReference br = new BlockReference(new Point3d(0, 0, 0), objId);
                    var extent = br.GeometricExtents;
                    var Length = extent.MaxPoint.X - extent.MinPoint.X;
                    var Hight = extent.MaxPoint.Y - extent.MinPoint.Y;
                    var Size = new double[] { Length, Hight };
                    BlockSize.Add(Size);
                }


                //立管对应的最低、最高层
                int[] loweststorey = { 1, 3, 13, 24 };
                int[] higheststorey = { 1, 12, 23, 31 };
                var LAYINGMETHOD = (int)LayingMethod.Piercing;  //敷设方式为穿梁
                //var LAYINGMETHOD = (int)LayingMethod.Buried;  //敷设方式为埋地

                //管径计算参数  UI输入值
                int QL = 250;  //最高日用水定额 QL
                double Kh = 2.5;  //最高日小时变化系数  Kh
                double m = 3.5;   //每户人数  m

                double PipeOffset_X = 10000; //第一根竖管相对于楼板起始 X 的偏移量
                double PipeGap = -600;  //竖管间的偏移量

                var PipeSystem = new List<ThWSuplySystemDiagram>();// 创建竖管系统列表
                for(int i = 0; i < loweststorey.Length; i++)  //对于每一根竖管 i 
                {
                    //生成竖管对象并添加至竖管系统列表
                    PipeSystem.Add(new ThWSuplySystemDiagram(loweststorey[i], higheststorey[i], PipeOffset_X + i * PipeGap, BlockSize));

                    for (int j = 1; j <= higheststorey[i]; j++)// 对于竖管 i 的第 j 个竖管单元(即第 j 层)
                    {
                        double ng = 1.2; //每户设置的卫生器具给水当量数
                        var pipeCompute = new PipeCompute(ng, QL, Kh, m);
                        String DN = pipeCompute.PipeDiameterCompute();
                        PipeSystem[i].PipeUnits.Add(new ThWSSDPipeUnit(DN, j));
                    }
                }
                //竖管对象绘制
                for (int i = 0; i < PipeSystem.Count; i++)
                {
                    var PipeLine = PipeSystem[i].CreatePipeLine(INDEX_START_X, INDEX_START_Y, FloorHeight, PipeGap * i,BlockSize);
                    acadDatabase.CurrentSpace.Add(PipeLine);

                    //绘制水管中断
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterPipeInterrupted,
                    new Point3d(PipeSystem[i].GetPipeX(), INDEX_START_Y - 300, 0), new Scale3d(1, 1, 1), Math.PI * 3/2);
                }

                //创建支管偏移数组
                double[] PipeOffsetX = new double[FloorNumbers];
                for (int i = 0; i < PipeOffsetX.Length; i++)
                {
                    PipeOffsetX[i] = PipeOffset_X + INDEX_START_X;
                    for(int j = 1; j < loweststorey.Length; j++)
                    {
                        if(i + 1 >= loweststorey[j])
                        {
                            PipeOffsetX[i] += PipeGap * 2;
                        }
                    }
                }

                


                //创建支管对象
                var BranchPipe = new List<ThWSSDBranchPipe>();
                for(int i = 0; i < FloorNumbers; i++)
                {                                     
                    BranchPipe.Add(new ThWSSDBranchPipe(i+1, FloorHeight, PipeOffsetX[i], INDEX_START_Y, StoreyList[i].getFlushFaucet(), StoreyList[i].getPRValve(), BlockSize, LAYINGMETHOD));                   
                }

             

                //支管对象创建
                for (int i = 0; i < BranchPipe.Count; i++)
                {
                    if (BranchPipe[i].BranchPipes is null)
                    {
                        continue;
                    }
                    var BPipeLines = BranchPipe[i].BranchPipes;
                    for (int j = 0; j < BPipeLines.Count; j++)
                    {
                        acadDatabase.CurrentSpace.Add(BPipeLines[j]);
                    }

                }

                //var waterSuplyUtils = WaterSuplyUtils.WaterSuplyBlockFilePath;
                WaterSuplyUtils.ImportNecessaryBlocks();//导入需要的模块






                for (int i = 0; i < FloorNumbers; i++)//对第3到最高层
                {
                    if(i == 0)
                    {
                        for (int j = 0; j < BranchPipe[i].GetCheckValveSite().Count; j++)
                        {
                            //绘制截止阀
                            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.CheckValve,
                            BranchPipe[i].GetCheckValveSite()[j], new Scale3d(0.5, 0.5, 1), 0);
                            //绘制水表
                            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", WaterSuplyBlockNames.WaterMeter,
                            BranchPipe[i].GetWaterMeterSite()[j], new Scale3d(0.5, 0.5, 1), 0);
                        }
                        //绘制自动排气阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-COOL-PIPE", WaterSuplyBlockNames.AutoExhaustValve,
                        BranchPipe[i].GetAutoExhaustValveSite(), new Scale3d(0.5, 0.5, 1), 0);
                        //绘制真空破坏器
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.VacuumBreaker,
                        BranchPipe[i].GetVacuumBreakerSite(), new Scale3d(1, 1, 1), 0);
                        //绘制水龙头
                        var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterTap,
                        BranchPipe[i].GetWaterTapSite(), new Scale3d(1, 1, 1), 0);
                        //设置水龙头的动态属性
                        objId.SetDynBlockValue("可见性", "向右");
                        continue;

                    }
                    if(i == 1)
                    {
                        continue;
                    }
                    
                    if (i + 1 != 12 && i + 1 != 23 && i + 1 != 31)//有减压阀层
                    {
                        //绘制减压阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.PressureReducingValve,
                        BranchPipe[i].GetPressureReducingValveSite(), new Scale3d(0.7, 0.7, 1), Math.PI * 3 / 2);
                    }
                    else//无减压阀层
                    {
                        //绘制截止阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.CheckValve,
                        BranchPipe[i].GetPressureReducingValveSite(), new Scale3d(0.5, 0.5, 1), Math.PI * 3 / 2);
                        //绘制自动排气阀
                        //acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-COOL-PIPE", WaterSuplyBlockNames.AutoExhaustValve,
                        //BranchPipe[i - 2].GetAutoExhaustValveSite(), new Scale3d(0.5, 0.5, 1), 0);
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-COOL-PIPE", WaterSuplyBlockNames.AutoExhaustValve,
                        BranchPipe[i].GetAutoExhaustValveSite(), new Scale3d(0.5, 0.5, 1), 0);
                    }

                    //绘制支管的截止阀、水表和水管中断
                    for (int j = 0; j < BranchPipe[i].GetCheckValveSite().Count; j++)
                    {
                        //绘制截止阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.CheckValve,
                        BranchPipe[i].GetCheckValveSite()[j], new Scale3d(0.5, 0.5, 1), 0);
                        //绘制水表
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", WaterSuplyBlockNames.WaterMeter,
                        BranchPipe[i].GetWaterMeterSite()[j], new Scale3d(0.5, 0.5, 1), 0);
                        //绘制水管中断
                        if (j < 4) //冲洗龙头不必绘制水管中断
                        {
                            double ang;
                            var scale = new Scale3d();
                            if (LAYINGMETHOD == 0)
                            {
                                ang = Math.PI;
                                scale = new Scale3d(0.8, 0.8, 0.8);
                            }
                            else
                            {
                                ang = Math.PI/2;
                                scale = new Scale3d(-0.8, 0.8, 0.8);
                            }
                            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterPipeInterrupted,
                            BranchPipe[i].GetWaterPipeInterrupted()[j], scale, ang);
                        }
                        else
                        {
                            //绘制真空破坏器
                            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.VacuumBreaker,
                            BranchPipe[i].GetVacuumBreakerSite(), new Scale3d(1, 1, 1), 0);
                            //绘制水龙头
                            var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterTap,
                            BranchPipe[i].GetWaterTapSite(), new Scale3d(1, 1, 1), 0);
                            //设置水龙头的动态属性
                            objId.SetDynBlockValue("可见性", "向右");

                        }
                    }
                }
            }
        }
    }
}
