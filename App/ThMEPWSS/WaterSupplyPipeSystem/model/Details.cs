using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.WaterSupplyPipeSystem.Data;
using ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase;

namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class Details
    {
        public static void Add(int i, AcadDatabase acadDatabase, List<ThWSSDBranchPipe> BranchPipe,
            ref bool layFlag, ref bool elevateFlag, SysIn sysIn, SysProcess sysProcess, bool prValveStyle)
        {
            var floorCleanToolList = sysProcess.FloorCleanToolList;
            var areaIndex = sysIn.AreaIndex;
            var layingMethod = sysIn.LayingMethod;
            var NoPRValve = sysIn.NoPRValve;
            var highestStorey = sysIn.HighestStorey;
            var floorNumbers = sysIn.FloorNumbers;
            var FlushFaucet = sysIn.FlushFaucet;
            var FloorHeight = sysIn.FloorLength;
            var insertPoint = sysIn.InsertPt;
            if(!prValveStyle) PRValveDetail.Add(i, acadDatabase, BranchPipe);
            if (i + 1 >= 3) //第三层放置敷设方式说明
            {
                if (BranchPipe[i].GetHouseholds() != 0 && layFlag)
                {
                    BranchPipe[i].DrawLayMethodNote();
                    layFlag = false;
                }
            }
            Elevation.Add(i, acadDatabase, BranchPipe, ref elevateFlag, FlushFaucet, layingMethod, FloorHeight, insertPoint);//第六层放置水管引出标高

            if (BranchPipe[i].BranchPipes == null)
            {
                return;
            }

            for (int j = 0; j < BranchPipe[i].GetCheckValveSite().Count; j++)
            {
                //绘制截止阀
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.CheckValve,
                BranchPipe[i].GetCheckValveSite()[j], GetScalce(0.7), 0);
                //绘制水表
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterMeter,
                BranchPipe[i].GetWaterMeterSite()[j], GetScalce(0.7), 0);
                if(prValveStyle)
                {
                    //绘制减压阀
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.PressureReducingValve,
                    BranchPipe[i].PRValveSite[j], GetScalce(0.7), 0);
                }
                //绘制水管中断
                if (j < floorCleanToolList[i][areaIndex].GetHouseholdNums())
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterPipeInterrupted,
                    BranchPipe[i].GetWaterPipeInterrupted()[j], GetScalce(BranchPipe[i].BlockRatio, layingMethod), Math.PI * (1 - layingMethod / 2.0));
                }
            }
            if(!prValveStyle)
            {
                if (!NoPRValve.Contains(i + 1))//有减压阀层
                {
                    //绘制减压阀
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.PressureReducingValve,
                    BranchPipe[i].GetPressureReducingValveSite(), GetScalce(BranchPipe[i].BlockRatio), Math.PI * 3 / 2);
                }
                else//无减压阀层
                {
                    //绘制截止阀
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.CheckValve,
                    BranchPipe[i].GetPressureReducingValveSite(), GetScalce(BranchPipe[i].BlockRatio), Math.PI * 3 / 2);
                }
            }


            //绘制自动排气阀
            if (highestStorey.Contains(i + 1))
            {
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-COOL-PIPE", WaterSuplyBlockNames.AutoExhaustValve,
                BranchPipe[i].GetAutoExhaustValveSite(), GetScalce(BranchPipe[i].AutoValveRatio), 0);
                if (i + 1 == floorNumbers)//最高层放置自动排气阀说明
                {
                    BranchPipe[i].DrawAutoExhaustValveNote();
                }
            }

            if (FlushFaucet.Contains(i + 1))
            {
                //绘制真空破坏器
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.VacuumBreaker,
                BranchPipe[i].GetVacuumBreakerSite(), GetScalce(BranchPipe[i].BlockRatio), 0);
                //绘制水龙头
                var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterTap,
                BranchPipe[i].GetWaterTapSite(), GetScalce(BranchPipe[i].BlockRatio), 0);
                //设置水龙头的动态属性
                objId.SetDynBlockValue("可见性", "向右");
            }
        }

        public static void Add(int i, int halfType, List<HalfBranchPipe> BranchPipe, ref bool layFlag, ref bool elevateFlag,
            SysIn sysIn, SysProcess sysProcess, bool prValveStyle)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var floorCleanToolList = sysProcess.FloorCleanToolList;
                var areaIndex = sysIn.AreaIndex;
                var layingMethod = sysIn.LayingMethod;
                var NoPRValve = sysIn.NoPRValve;
                var highestStorey = sysIn.HighestStorey;
                var floorNumbers = sysIn.FloorNumbers;
                var FlushFaucet = sysIn.FlushFaucet;
                var FloorHeight = sysIn.FloorLength;
                var insertPoint = sysIn.InsertPt;

                if (!prValveStyle) PRValveDetail.Add(i, acadDatabase, BranchPipe);
                if (i + 1 >= 3) //第三层放置敷设方式说明
                {
                    if (BranchPipe[i].Households[BranchPipe[i].AreaIndex] != 0 && layFlag)
                    {
                        BranchPipe[i].DrawLayMethodNote();
                        layFlag = false;
                    }
                }
                if(i== floorNumbers-1)//最高层放置楼梯敷设
                {
                    BranchPipe[i].DrawStairCallout();
                }
                Elevation.Add(i, acadDatabase, BranchPipe, ref elevateFlag, insertPoint);//第六层放置水管引出标高

                if (BranchPipe[i].BranchPipes == null) return;

                for (int j = 0; j < BranchPipe[i].CheckValveSite.Count; j++)
                {
                    //绘制截止阀
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.CheckValve,
                    BranchPipe[i].CheckValveSite[j], GetScalce(0.7), 0);
                    if (prValveStyle)
                    {
                        //绘制减压阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.PressureReducingValve,
                        BranchPipe[i].PRValveSite[j], GetScalce(0.7), 0);
                    }
                    //绘制水表
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", WaterSuplyBlockNames.WaterMeter,
                    BranchPipe[i].WaterMeterSite[j], GetScalce(0.7), 0);
                    //绘制水管中断
                    if (j < floorCleanToolList[i][areaIndex].GetHouseholdNums())
                    {
                        double angle = Math.PI;
                        if (halfType == 3 || halfType == 4) angle = Math.PI / 2;
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterPipeInterrupted,
                        BranchPipe[i].WaterPipeInterrupted[j], new Scale3d(1, 1, 1), angle);
                    }
                }
                if (!prValveStyle)
                {
                    if (!NoPRValve.Contains(i + 1))//有减压阀层
                    {
                        //绘制减压阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.PressureReducingValve,
                        BranchPipe[i].PressureReducingValveSite, GetScalce(BranchPipe[i].BlockRatio), Math.PI * 3 / 2);
                    }
                    else//无减压阀层
                    {
                        //绘制截止阀
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.CheckValve,
                        BranchPipe[i].PressureReducingValveSite, GetScalce(BranchPipe[i].BlockRatio), Math.PI * 3 / 2);
                    }
                }

                //绘制自动排气阀
                if (highestStorey.Contains(i + 1))
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-COOL-PIPE", WaterSuplyBlockNames.AutoExhaustValve,
                    BranchPipe[i].AutoExhaustValveSite, GetScalce(BranchPipe[i].AutoValveRatio), 0);
                    if (i + 1 == floorNumbers)//最高层放置自动排气阀说明
                    {
                        BranchPipe[i].DrawAutoExhaustValveNote();
                    }
                }

                if (FlushFaucet.Contains(i + 1))
                {
                    //绘制真空破坏器
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.VacuumBreaker,
                    BranchPipe[i].VacuumBreakerSite, GetScalce(BranchPipe[i].BlockRatio), 0);
                    //绘制水龙头
                    var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-EQPM", WaterSuplyBlockNames.WaterTap,
                    BranchPipe[i].WaterTapSite, GetScalce(BranchPipe[i].BlockRatio), 0);
                    //设置水龙头的动态属性
                    objId.SetDynBlockValue("可见性", "向右");
                }
            }
        }


        public static Scale3d GetScalce(double ratio)
        {
            return new Scale3d(ratio, ratio, ratio);
        }


        public static Scale3d GetScalce(double ratio, int layingMethod)
        {
            if (layingMethod == 0)
            {
                return new Scale3d(ratio, ratio, ratio);
            }
            return new Scale3d(-ratio, ratio, ratio);
        }
    }
}
