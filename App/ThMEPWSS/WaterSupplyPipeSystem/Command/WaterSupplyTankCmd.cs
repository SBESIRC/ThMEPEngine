using AcHelper;
using Linq2Acad;
using System.Collections.Generic;
using System.Windows.Forms;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.WaterSupplyPipeSystem.model;
using ThMEPWSS.WaterSupplyPipeSystem.Method;
using ThMEPWSS.WaterSupplyPipeSystem.Data;
using ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase;
using ThMEPWSS.Uitl.ExtensionsNs;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.Service;
using Autodesk.AutoCAD.Geometry;
using System;
using ThMEPWSS.Pipe.Model;
using DotNetARX;

namespace ThMEPWSS.WaterSupplyPipeSystem.Command
{
    internal class WaterSupplyTankCmd
    {
        public static void ExecuteTank(WaterSupplyVM uiConfigs, Dictionary<string, List<string>> blockConfig)
        {
            var FloorHeightVM = FloorHeightsViewModel.Instance;
            var tankVM = uiConfigs.SetViewModel.tankViewModel;
            var floorHeightDic = FloorHeightsViewModel.Instance.GetFloorHeightsDict(100);

            var prValveStyle = uiConfigs.SetViewModel.PRValveStyleDynamicRadios[1].IsChecked;//true表示一户一阀
            var NumberofPressurizedFloors = 4;//加压楼层数
            if(tankVM.PressurizedFloor[0].IsChecked)
            {
                NumberofPressurizedFloors = 3;
            }

            using (Active.Document.LockDocument())//非模态框不能直接操作CAD，需要加锁
            using (var acadDatabase = AcadDatabase.Active())
            {
                var sysIn = new SysIn();
                sysIn.TankSet(acadDatabase, uiConfigs, blockConfig);

                var lowestFloor = tankVM.LowestFloor;
                var highestFloor = tankVM.HighestFloor;
                var sysProcess = new SysProcess();
                sysProcess.SetTank(sysIn);
                var sysOut = new SysOut();
                AddStoreyLine(sysOut, sysIn, highestFloor, uiConfigs.FloorAreaList[0].Count-1);//楼板线绘制

                var insertPt = sysIn.InsertPt.Clone();

                AddStDetail(insertPt, sysOut);
                var households = ThWCompute.CountKitchenNums(sysIn);

                ThWCompute.U0NgCompute(uiConfigs.FloorAreaList[0].Count - 1, highestFloor, NumberofPressurizedFloors, sysIn, sysProcess);

                double qg = 0;
                for (int i = 0; i < uiConfigs.FloorAreaList[0].Count - 1; i++)//分区遍历
                {
                    var upperHeight = insertPt.Y + highestFloor * sysIn.FloorHeight;//楼顶高Y
                    double offsetX = 9500 + 5855 * i;

                    var prValveGroupFloor = Tool.GetPrValveGroupFloor(i, uiConfigs, households, floorHeightDic);//减压阀组所在楼层
                    var prValveGroupPt = insertPt.OffsetXY(offsetX + 600, prValveGroupFloor * sysIn.FloorHeight - 700);

                    sysOut.PrValveGroups.Add(prValveGroupPt);//减压阀组

                    if (i == 0)
                    {
                        sysOut.TextLines.Add(new Line(prValveGroupPt.OffsetXY(-56, 17), prValveGroupPt.OffsetXY(-1878, -200)));
                        sysOut.PrValveDetail.Add(prValveGroupPt.OffsetXY(-746, -2883));
                    }

                    var autoValvePt = AddAutoValve(insertPt, offsetX, highestFloor, sysOut, sysIn);

                    if(i==0) sysOut.PipeLines.Add(new Line(insertPt.OffsetXY(offsetX, -712), 
                                insertPt.OffsetXY(offsetX, sysIn.FloorHeight * highestFloor + 405 - 0.1)));

                   
                    var qg1 = AddDN( acadDatabase,  insertPt,  offsetX, i, 0, highestFloor,0, prValveStyle, highestFloor, sysProcess,  sysIn);
                    var qg2 = AddDN( acadDatabase,  insertPt,  offsetX, i, highestFloor - NumberofPressurizedFloors, highestFloor,1, prValveStyle, NumberofPressurizedFloors , sysProcess,  sysIn);
                    qg += qg1;
                    qg += qg2;

                    if (i== uiConfigs.FloorAreaList[0].Count - 2)
                    {
                        sysOut.PipeLines.Add(new Line(new Point3d(insertPt.X + 9500, upperHeight + 817 + 87.7, 0),
                            new Point3d(insertPt.X + offsetX + 600, upperHeight + 817 + 87.7, 0)));
                        sysOut.PipeLines.Add(new Line(new Point3d(insertPt.X + 9500, autoValvePt.Y,0),autoValvePt));
                    }

                    var drawFirstPipe = true;//竖管绘制
                    var drawBranchPipeNote = true;//支管描述绘制
                    for(int j = 0; j < highestFloor; j++)//分层遍历
                    {
                        var houseNum = households[j][i+1];
                        var curUpperHeight = insertPt.Y + (j + 1) * sysIn.FloorHeight - 100;//当前楼顶高度 -100
                        var curLowerHeight = insertPt.Y + j * sysIn.FloorHeight;
                        var isPressureFloor = j >= highestFloor - NumberofPressurizedFloors;//加压楼层
                        var pt1 = new Point3d(insertPt.X + offsetX + 600, 150 + curLowerHeight, 0);
                        if (isPressureFloor) pt1 = pt1.OffsetX(600);//加压楼层
                        if (!prValveStyle)//一层一阀
                        {
                            pt1 = pt1.OffsetX(410);
                        }
                        var orgPt1 = pt1.Clone();
       
                        for (int k = 0; k < houseNum; k++)//住户遍历
                        {
                            var pt3 = new Point3d(insertPt.X+ offsetX + 600 + 1582.5, pt1.Y,0);
                            var pt2 = pt3.OffsetX(-472.5);
                            if (prValveStyle) pt2 = pt2.OffsetX(-262.5);

                            var _k = houseNum - k - 1;
                            var pt4 = pt3.OffsetX(300 + 120 * _k);
                            var pt5 = new Point3d(pt4.X, curUpperHeight - 120 * _k, 0);
                            var pt6 = pt5.OffsetX(700 + 120 * k);
                            sysOut.PipeLines.Add(new Line(pt1, pt2));
                            sysOut.PipeLines.Add(new Line(pt3, pt4));
                            sysOut.PipeLines.Add(new Line(pt4, pt5));
                            sysOut.PipeLines.Add(new Line(pt5, pt6));
                            if (prValveStyle)
                            {
                                if (isPressureFloor) //加压楼层
                                {
                                    if (NumberofPressurizedFloors == 4 && j == highestFloor - 4)//为4的最下面一层装减压阀
                                    {
                                        sysOut.MetersWithPrValve.Add(pt2);
                                    }
                                    else
                                    {
                                        sysOut.PipeLines.Add(new Line(pt2, pt2.OffsetX(262.5)));
                                        sysOut.InDoorWaterMeters.Add(pt2.OffsetX(262.5));
                                    }
                                }
                                else//非加压楼层
                                {
                                    if (FloorHeightVM.GeneralFloor * (j + 1) / 1000 <= tankVM.Elevation - 20 && !((j + 1) < prValveGroupFloor && (prValveGroupFloor - j - 1) < 4))
                                    {
                                        sysOut.MetersWithPrValve.Add(pt2);
                                    }
                                
                                    else
                                    {
                                        sysOut.PipeLines.Add(new Line(pt2, pt2.OffsetX(262.5)));
                                        sysOut.InDoorWaterMeters.Add(pt2.OffsetX(262.5));
                                    }
                                }
                            }
                            else sysOut.InDoorWaterMeters.Add(pt2); 

                            pt1 = pt1.OffsetY(250);
                            if(drawBranchPipeNote)
                            {
                                var branchNotePt = new Point3d(pt6.X, insertPt.Y + sysIn.FloorHeight * highestFloor + 150, 0);
                                sysOut.TextLines.Add(new Line(pt6, branchNotePt));
                                sysOut.TextLines.Add(new Line(branchNotePt, branchNotePt.OffsetX(2530)));
                                sysOut.Texts.Add(ThText.DbText(branchNotePt, "接至户内给水管DN25", "W-WSUP-DIMS"));
                                drawBranchPipeNote = false;
                            }
                        }
                        if(!prValveStyle && houseNum > 0)
                        {
                            var pt0 = new Point3d(pt1.X, curUpperHeight, 0);
                            sysOut.PipeLines.Add(new Line(pt0, orgPt1));
                            if(isPressureFloor) //加压楼层
                            {
                                if (NumberofPressurizedFloors == 4 && j == highestFloor - 4)//为4的最下面一层装减压阀
                                {
                                    sysOut.PipeLines.Add(new Line(pt0, pt0.OffsetX(-100)));
                                    sysOut.PipeLines.Add(new Line(pt0.OffsetX(-310), pt0.OffsetX(-410)));
                                    sysOut.PrValves.Add(pt0.OffsetX(-310));
                                }
                                else
                                {
                                    sysOut.PipeLines.Add(new Line(pt0, pt0.OffsetX(-410)));
                                }
                            }
                            else//非加压楼层
                            {
                                if(FloorHeightVM.GeneralFloor * (j+1) / 1000 <= tankVM.Elevation -20 && !((j+1) < prValveGroupFloor && (prValveGroupFloor-j-1)<4))
                                {
                                    sysOut.PipeLines.Add(new Line(pt0, pt0.OffsetX(-100)));
                                    sysOut.PipeLines.Add(new Line(pt0.OffsetX(-310), pt0.OffsetX(-410)));
                                    sysOut.PrValves.Add(pt0.OffsetX(-310));
                                }
                                else
                                {
                                    sysOut.PipeLines.Add(new Line(pt0, pt0.OffsetX(-410)));
                                }
                            }
    
                            
                            
                            if(drawFirstPipe)//竖管绘制
                            {
                                sysOut.PipeLines.Add(new Line(pt0.OffsetX(-410), prValveGroupPt));
                                sysOut.PipeLines.Add(new Line(prValveGroupPt.OffsetY(400),
                                    insertPt.OffsetXY(offsetX + 600, sysIn.FloorHeight * highestFloor + 817 + 87.7)));

                                sysOut.PipeLines.Add(new Line(insertPt.OffsetXY(offsetX + 1200, sysIn.FloorHeight * (highestFloor - NumberofPressurizedFloors + 1) - 100), autoValvePt));//第二根立管
                                drawFirstPipe = false;

                            }
                        }
                        else
                        {
                            if (houseNum > 0 && drawFirstPipe)//竖管绘制
                            {
                                sysOut.PipeLines.Add(new Line(orgPt1, prValveGroupPt));
                                sysOut.PipeLines.Add(new Line(prValveGroupPt.OffsetY(400),
                                    insertPt.OffsetXY(offsetX + 600, sysIn.FloorHeight * highestFloor + 817 + 87.7)));
                                sysOut.PipeLines.Add(new Line(insertPt.OffsetXY(offsetX + 1200, sysIn.FloorHeight * (highestFloor - NumberofPressurizedFloors) + 150), autoValvePt));//第二根立管

                                drawFirstPipe = false;
                            }
        
                        }
                    }
                }

                if (tankVM.SterilizerType[0].IsChecked)
                {
                    sysOut.InTankDetail.Add(insertPt.OffsetXY(-1626.6, sysIn.FloorHeight * highestFloor));
                }
                else
                {
                    sysOut.OutTankDetail.Add(insertPt.OffsetXY(-1626.6, sysIn.FloorHeight * highestFloor));
                }
                AddTotalDN(acadDatabase, insertPt, sysProcess, sysIn.FloorHeight,  highestFloor,0);
                AddTotalDN(acadDatabase, insertPt, sysProcess, sysIn.FloorHeight,  highestFloor,1);

                sysOut.Draw(tankVM,qg);
            }
        }

        public static void AddStoreyLine(SysOut sysOut, SysIn sysIn, int highestFloor,int areaCnt)
        {
            var floorHeightDic = FloorHeightsViewModel.Instance.GetSpecialFloorHeightsDict(highestFloor);
            var pt1 = sysIn.InsertPt.OffsetX(-9500);
            for (int i = 0; i < highestFloor + 1; i++)
            {
                var pt2 = pt1.OffsetX(sysIn.FloorLength + 9500 + 5855 * (areaCnt-2));
                sysOut.FloorLines.Add(new Line(pt1, pt2));

                var floorText = Convert.ToString(i + 1) + "F";
                if (i == highestFloor) floorText = "RF";
                sysOut.Texts.Add(ThText.DbText(pt1.OffsetXY(1500,100), floorText, "W-NOTE"));
                string height = "X.XX";
                if (floorHeightDic.ContainsKey(Convert.ToString(i + 1)))
                {
                    height = floorHeightDic[Convert.ToString(i + 1)];
                }
                sysOut.ElevationDic.Add(pt1, height);

                pt1 = pt1.OffsetY(sysIn.FloorHeight);
            }
        }

        public static Point3d AddAutoValve(Point3d insertPt, double offsetX, int highestFloor, SysOut sysOut, SysIn sysIn)
        {
            var autoValvePt = insertPt.OffsetXY(offsetX + 1200, sysIn.FloorHeight * highestFloor + 1466);//自动排气阀点
            sysOut.AutoValves.Add(autoValvePt);
            sysOut.TextLines.Add(new Line(autoValvePt.OffsetY(439), autoValvePt.OffsetXY(343, 903)));
            sysOut.TextLines.Add(new Line(autoValvePt.OffsetXY(2478, 903), autoValvePt.OffsetXY(343, 903)));
            sysOut.Texts.Add(ThText.DbText(autoValvePt.OffsetXY(490, 903), "自动排气阀DN25", "W-WSUP-DIMS"));

            return autoValvePt;
        }

        public static void AddStDetail(Point3d insertPt, SysOut sysOut)
        {
            var pipeStPt = insertPt.OffsetXY(9500 - 3731, -712);
            sysOut.PipeLines.Add(new Line(pipeStPt.OffsetX(3731), pipeStPt));
            sysOut.TextLines.Add(new Line(pipeStPt.OffsetXY(-50, 50), pipeStPt.OffsetXY(50, -50)));
            sysOut.TextLines.Add(new Line(pipeStPt, pipeStPt.OffsetY(1022)));
            sysOut.TextLines.Add(new Line(pipeStPt.OffsetXY(-3690, 1022), pipeStPt.OffsetY(1022)));
            sysOut.Texts.Add(ThText.DbText(pipeStPt.OffsetXY(-3690, 1022), "接自地库生活水泵房加压给水管", "W-WSUP-NOTE"));
            sysOut.Texts.Add(ThText.DbText(pipeStPt.OffsetXY(628, 150), "DN50", "W-WSUP-NOTE"));
            sysOut.ElevationDic.Add(pipeStPt.OffsetX(2980), "");
            sysOut.Texts.Add(ThText.DbText(pipeStPt.OffsetXY(1646, 270), "贴梁底", "W-DRAI-DIMS"));
            sysOut.GateValves.Add(pipeStPt.OffsetX(2294));


     
        }

        public static double AddDN(AcadDatabase acadDatabase, Point3d insertPt, double offsetX, int i, int minFloor, int maxFloor, int flag,
            bool prValveStyle, int highestFloor, SysProcess sysProcess, SysIn sysIn)
        {
            var DnList = new List<string>();
            double Qg = 0;//流量
            for (int j = minFloor; j < maxFloor; j++)//分层遍历
            {
                
                var HouseholdNum = Tool.GetHouseholdNum(j, i+1, sysIn, sysProcess);
                var DN = Tool.GetDN(sysProcess.U0LIST[2 * i + flag][j], sysProcess.NGLIST[2 * i + flag][j], HouseholdNum,out double qg);
                if (j == maxFloor - 1) Qg += qg;
                DnList.Add(DN);
            }
            for (int j = 0; j < highestFloor; j++)
            {
                //管径图样插入 (DN50)
                if (j != highestFloor - 1 && j != 0)
                {
                    var DNj = DnList[j];
                    var DNjp = DnList[j + 1];
                    var DNjn = DnList[j - 1];
                    if (DNj.Equals(DNjp) && DNj.Equals(DNjn))
                    {
                        continue;
                    }
                }
                if (DnList[j] != "")
                {
                    var angle = Math.PI / 2;
                    var Position = insertPt.OffsetXY(offsetX + 600 + 600 * flag, sysIn.FloorHeight * (j + 1) + sysIn.FloorHeight * (maxFloor- highestFloor) * flag - 700 - sysIn.FloorHeight / 3);
                    if (!prValveStyle) Position = Position.OffsetY(sysIn.FloorHeight);
                    if (!prValveStyle && j == highestFloor - 1)//一层一阀 最高层、
                    {
                        Position = Position.OffsetY(-500);
                    }
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-DIMS", WaterSuplyBlockNames.PipeDiameter,
                    Position, new Scale3d(1, 1, 1), angle);

                    objID.SetDynBlockValue("可见性", DnList[j]);
                    
                }
            }

            return Qg;
        }

        public static void AddTotalDN(AcadDatabase acadDatabase, Point3d insertPt, SysProcess sysProcess, double floorHeight,int highestFloor,int flag)
        {
            var U0 = 0.0;
            var Ng = 0.0;
            var cnt = sysProcess.U0LIST.Count / 2;
            var lastIndex = sysProcess.U0LIST[0].Length - 1;
            if (flag==0)
            {
                for(int i = 0; i < cnt; i++)
                {
                    U0+= sysProcess.U0LIST[2 * i][lastIndex];
                    Ng+= sysProcess.NGLIST[2 * i][lastIndex];
                }
            }
            else
            {
                for (int i = 0; i < cnt; i++)
                {
                    U0 += sysProcess.U0LIST[2 * i+1][lastIndex];
                    Ng += sysProcess.NGLIST[2 * i+1][lastIndex];
                }
            }
            U0 /= cnt;
            var DN = Tool.GetDN(U0, Ng);
            //var DN = PipeDiameterCompute();

            var Position = insertPt.OffsetXY(9500-500, floorHeight * highestFloor +925 +flag * 561);
            var angle = 0;
            
            var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-WSUP-DIMS", WaterSuplyBlockNames.PipeDiameter,
            Position, new Scale3d(1, 1, 1), angle);

            objID.SetDynBlockValue("可见性", DN);
        }
    }
}
