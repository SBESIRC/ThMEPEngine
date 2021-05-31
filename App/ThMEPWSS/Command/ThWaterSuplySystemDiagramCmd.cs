using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Command
{
    public class ThWaterSuplySystemDiagramCmd : IAcadCommand, IDisposable
    {
        readonly DrainageViewModel UiConfigs;

        public ThWaterSuplySystemDiagramCmd(DrainageViewModel uiConfigs)
        {
            UiConfigs = uiConfigs;
        }

        public void Dispose()
        {
        }

        public void Execute()
        {
            if(UiConfigs.SelectRadionButton.Content is null)
            {
                MessageBox.Show("不存在有效分组，请重新读取");
                return;
            }
            var areaIndex = Convert.ToInt32(UiConfigs.SelectRadionButton.Content.Split('组')[1]) - 1;//读取分区
            var setViewModel = UiConfigs.DynamicRadioButtons[areaIndex].SetViewModel;

            var layingMethod = (int)LayingMethod.Piercing;//敷设方式默认为穿梁
            if (setViewModel.DynamicRadios[1].IsChecked)
            {
                layingMethod = (int)LayingMethod.Buried;//敷设方式为埋地
            }


            if (!ThWCompute.IsCorrectNum(Convert.ToString(setViewModel.FloorLineSpace), "int"))
            {
                MessageBox.Show("楼层线输入有误，只能是正整数");
                return;
            }
            var FloorHeight = setViewModel.FloorLineSpace;  //楼层线间距 mm
            
            FloorHeight = ThWCompute.AutomaticValue(FloorHeight, 1500, 9999);

            var FlushFaucet = new List<int>();//冲洗龙头层
            var strType = new List<char>();
            if(!ThWCompute.IsCorrectNum(setViewModel.FaucetFloor, "str"))
            {
                MessageBox.Show("冲洗龙头输入有误，只能是数字，\"-\",\",\"");
                return;
            }
            
            if (setViewModel.FaucetFloor != "")
            {
                foreach (var f in setViewModel.FaucetFloor.Split(','))
                {
                    FlushFaucet.Add(Convert.ToInt32(f));
                }
            }

            var NoPRValve = new List<int>();//无减压阀层
            if (!ThWCompute.IsCorrectNum(setViewModel.NoCheckValve, "str"))
            {
                MessageBox.Show("无减压阀层输入有误，只能是数字，\"-\",\",\"");
                return;
            }
            
            if (setViewModel.NoCheckValve != "")
            {
                foreach (var f in setViewModel.NoCheckValve.Split(','))
                {
                    NoPRValve.Add(Convert.ToInt32(f));
                }
            }

            var selectedArea = UiConfigs.SelectedArea;
            var floorAreaList = UiConfigs.FloorAreaList;
            var floorNumList = UiConfigs.FloorNumList;

            var floorNumbers = 1;//楼层数统计
            foreach (var fn in floorNumList)
            {
                if (fn.Max() > floorNumbers)
                {
                    floorNumbers = fn.Max();
                }
            }
            if (!ThWCompute.IsCorrectNum(Convert.ToString(setViewModel.MaxDayQuota), "int"))
            {
                MessageBox.Show("最高日用水定额输入有误，只能是正整数");
                return;
            }
           
            var QL = setViewModel.MaxDayQuota;  //最高日用水定额 QL
            QL = ThWCompute.AutomaticValue(QL, 130, 320);
           
            if (!ThWCompute.IsCorrectNum(Convert.ToString(setViewModel.MaxDayHourCoefficient), "float"))
            {
                MessageBox.Show("最高日小时变化系数输入有误，只能是正数");
                return;
            }
           
            var Kh = Convert.ToDouble(setViewModel.MaxDayHourCoefficient.ToString("0.0"));  //最高日小时变化系数  Kh
            Kh = ThWCompute.AutomaticValue(Kh, 2.0, 2.8);

            if (!ThWCompute.IsCorrectNum(Convert.ToString(setViewModel.NumberOfHouseholds), "float"))
            {
                MessageBox.Show("每户人数输入有误，只能是正数");
                return;
            }

            var m = Convert.ToDouble(setViewModel.NumberOfHouseholds.ToString("0.0"));   //每户人数  m
            m = ThWCompute.AutomaticValue(m, 1, 6);

            //立管编号及对应的最低、最高层
            var pipeNumber = new List<String>();
            var lowestStorey = new List<int>();
            var highestStorey = new List<int>();
            
            
            for (int i = 0; i < setViewModel.PartitionDatas.Count; i++)
            {
                pipeNumber.Add(setViewModel.PartitionDatas[i].RiserNumber);
                lowestStorey.Add(Convert.ToInt32(setViewModel.PartitionDatas[i].MinimumFloorNumber));
                highestStorey.Add(Convert.ToInt32(setViewModel.PartitionDatas[i].HighestFloorNumber));
                if(lowestStorey[i] > highestStorey[i])
                {
                    MessageBox.Show("当前行最底层的值大于最高层");
                    return;
                }
            }
            var floorExist = new List<int>();
            for(int i = 0; i < pipeNumber.Count; i++)
            {
                for(int j = lowestStorey[i]; j <= highestStorey[i]; j++)
                {
                    if(floorExist.Contains(j))
                    {
                        MessageBox.Show("存在楼层重复");
                        return;
                    }
                    else
                    {
                        floorExist.Add(j);
                    }
                }
            }
            
            

            double floorLength = 20000;//楼板线长度

            var WaterEquivalent = new double[] { 0.5, 0.75, 1, 0.75, 1, 0.5, 1, 1.2 };//用水当量数

            using (Active.Document.LockDocument())//非模态框不能直接操作CAD，需要加锁
            using (var acadDatabase = AcadDatabase.Active()) 
            {
                var insertOpt = new PromptPointOptions("\n指定图纸的插入点");
                var insertPt = Active.Editor.GetPoint(insertOpt);
                double indexStartX = insertPt.Value.X;
                double indexStartY = insertPt.Value.Y;

                var notExistFloor = new List<int>();//不存在的楼层号列表
                for (int i = 0; i < floorNumbers; i++)
                {
                    var hasNum = false;
                    foreach (var f in floorNumList)
                    {
                        if (f.Contains(i + 1))
                        {
                            hasNum = true;
                            break;
                        }
                    }
                    if (!hasNum)
                    {
                        notExistFloor.Add(i + 1);
                    }
                }

                //统计厨房数
                var households = ThWCompute.CountKitchenNums(floorAreaList, selectedArea, floorNumList, floorNumbers);
                //统计卫生洁具数
                var floorCleanToolList = ThWCompute.CountCleanToolNums(floorAreaList, households, floorNumList, selectedArea, notExistFloor);

                WaterSuplyUtils.ImportNecessaryBlocks();//导入需要的模块
                var bt = acadDatabase.Element<BlockTable>(acadDatabase.Database.BlockTableId);//创建BlockTable
                var BlockSize = ThWCompute.CreateBlockSizeList(bt);//获取并添加 block 尺寸

                //楼板线生成
                var StoreyList = ThWCompute.CreateStoreysList(floorNumbers, FloorHeight, FlushFaucet, NoPRValve, households);

                //楼层线绘制
                for (int i = 0; i < floorNumbers + 1; i++)
                {
                    StoreyList[i].DrawStorey(i, floorNumbers, indexStartX, indexStartY, floorLength);
                }

                double T = 24;
                double PipeOffset_X = 10000; //第一根竖管相对于楼板起始 X 的偏移量
                double PipeGap = -600;  //竖管间的偏移量

                var maxHouseholdNums = 0;
                for (int i = 0; i < floorNumbers; i++)
                {
                    if (floorCleanToolList[i][areaIndex].GetHouseholdNums() > maxHouseholdNums)
                    {
                        maxHouseholdNums = floorCleanToolList[i][areaIndex].GetHouseholdNums();
                    }
                }

                // 创建竖管系统列表
                var NGLIST = new List<double[]>();
                var U0LIST = new List<double[]>();
                var PipeSystem = ThWCompute.CreatePipeSystem(ref NGLIST, ref U0LIST, lowestStorey, highestStorey, PipeOffset_X,
                    floorCleanToolList, areaIndex, PipeGap, WaterEquivalent, setViewModel, T, maxHouseholdNums, pipeNumber);

                //竖管对象绘制
                var pt = new List<double>();
                for (int i = 0; i < PipeSystem.Count; i++)
                {
                    PipeSystem[i].DrawPipeLine(i, indexStartX, indexStartY, FloorHeight, PipeGap, PipeSystem.Count);
                    pt.Add(PipeSystem[i].GetPipeX());
                    ;

                }

                //创建支管偏移数组
                double[] PipeOffsetX = new double[floorNumbers];
                for (int i = 0; i < PipeOffsetX.Length; i++)
                {
                    PipeOffsetX[i] = PipeOffset_X + indexStartX;
                    for (int j = 1; j < lowestStorey.Count; j++)
                    {
                        if (i + 1 >= lowestStorey[j])
                        {
                            PipeOffsetX[i] += PipeGap;
                        }
                    }
                }

                //创建支管对象
                var BranchPipe = new List<ThWSSDBranchPipe>();
                        
                        
                for (int i = 0; i < floorNumbers; i++)
                {
                    var HouseholdNum = floorCleanToolList[i][areaIndex].GetHouseholdNums();
                    if (HouseholdNum == 0)
                    {
                        HouseholdNum = maxHouseholdNums;
                    }
                    double Ngi = ThWCompute.InnerProduct(floorCleanToolList[i][areaIndex].GetCleaningTools(), WaterEquivalent) / HouseholdNum;
                    double U0i = 100 * QL * m * Kh / (0.2 * Ngi * T * 3600);
                    var pipeCompute = new PipeCompute(U0i, Ngi);
                    var DN = pipeCompute.PipeDiameterCompute();
                    BranchPipe.Add(new ThWSSDBranchPipe(DN, StoreyList[i], indexStartY, PipeOffsetX[i], BlockSize, layingMethod));
                }

                //支管绘制
                for (int i = 0; i < BranchPipe.Count; i++)
                {
                    BranchPipe[i].DrawBranchPipe();
                }

                for (int i = 0; i < floorNumbers; i++)
                {
                    if (i + 1 == 5)//第五层放置减压阀详图
                    {
                        //绘制减压阀详图
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", WaterSuplyBlockNames.PRValveDetail,
                        BranchPipe[i].GetPRValveDetailSite(), new Scale3d(1, 1, 1), 0);
                        var ptls = new Point3d[3];
                        ptls[0] = BranchPipe[i-1].GetPressureReducingValveSite();
                        ptls[1] = new Point3d(BranchPipe[i-1].GetPressureReducingValveSite().X + 500, BranchPipe[i].GetPRValveDetailSite().Y, 0);
                        ptls[2] = BranchPipe[i].GetPRValveDetailSite();
                        var polyline = new Polyline3d(0,new Point3dCollection(ptls),false);
                        acadDatabase.CurrentSpace.Add(polyline);
                    }
                   
                    if (i + 1 == 3) //第三层放置敷设方式说明
                    {
                        BranchPipe[i].DrawLayMethodNote();
                    }


                    if (i+1 == 6)//第六层放置水管引出标高
                    {
                        for (int j = 0; j < BranchPipe[i].GetCheckValveSite().Count; j++)
                        {
                            var ptLs = new Point3d[4];

                            ptLs[0] = new Point3d(BranchPipe[i].GetWaterPipeInterrupted()[0].X, BranchPipe[i].GetCheckValveSite()[j].Y, 0);
                            ptLs[1] = new Point3d(ptLs[0].X + 500 + i * 300, ptLs[0].Y, 0);
                            ptLs[2] = new Point3d(ptLs[1].X, ptLs[1].Y + 350 * (BranchPipe[i].GetCheckValveSite().Count - j - 1), 0);
                            ptLs[3] = new Point3d(ptLs[2].X + 500, ptLs[2].Y, 0);

                            var lineNote = new Polyline3d(0, new Point3dCollection(ptLs), false);
                            lineNote.LayerId = DbHelper.GetLayerId("W-WSUP-NOTE");
                            acadDatabase.CurrentSpace.Add(lineNote);

                            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-NOTE", WaterSuplyBlockNames.Elevation,
                            ptLs[2], new Scale3d(0.5, 0.5, 0.5), 0);
                        }
                    }

                    if (BranchPipe[i].BranchPipes == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < BranchPipe[i].GetCheckValveSite().Count; j++)
                    {
                        //绘制截止阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.CheckValve,
                        BranchPipe[i].GetCheckValveSite()[j], new Scale3d(0.5, 0.5, 0.5), 0);
                        //绘制水表
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", WaterSuplyBlockNames.WaterMeter,
                        BranchPipe[i].GetWaterMeterSite()[j], new Scale3d(0.5, 0.5, 0.5), 0);
                        //绘制水管中断
                        if (j < floorCleanToolList[i][areaIndex].GetHouseholdNums())
                        {
                            acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterPipeInterrupted,
                            BranchPipe[i].GetWaterPipeInterrupted()[j], new Scale3d(0.8 - 1.6 * layingMethod, 0.8, 0.8), Math.PI * (1 - layingMethod/2.0));
                        }
                    }
                    if (!NoPRValve.Contains(i + 1))//有减压阀层
                    {
                        //绘制减压阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.PressureReducingValve,
                        BranchPipe[i].GetPressureReducingValveSite(), new Scale3d(0.7, 0.7, 0.7), Math.PI * 3 / 2);
                    }
                    else//无减压阀层
                    {
                        //绘制截止阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.CheckValve,
                        BranchPipe[i].GetPressureReducingValveSite(), new Scale3d(0.5, 0.5, 0.5), Math.PI * 3 / 2);
                        //绘制自动排气阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-COOL-PIPE", WaterSuplyBlockNames.AutoExhaustValve,
                        BranchPipe[i].GetAutoExhaustValveSite(), new Scale3d(0.5, 0.5, 0.5), 0);
                        if(i + 1 == floorNumbers)//最高层放置自动排气阀说明
                        {
                            BranchPipe[i].DrawAutoExhaustValveNote();
                        }
                    }
                    if (FlushFaucet.Contains(i + 1))
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