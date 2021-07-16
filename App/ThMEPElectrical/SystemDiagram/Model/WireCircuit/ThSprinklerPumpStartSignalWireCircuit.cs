using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SystemDiagram.Service;

namespace ThMEPElectrical.SystemDiagram.Model.WireCircuit
{
    /// <summary>
    /// 喷淋泵启动信号线
    /// </summary>
    public class ThSprinklerPumpStartSignalVerticalWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            return new List<Entity>();
        }

        public override Dictionary<int, List<Entity>> DrawVertical()
        {
            int currentIndex = 20;
            Dictionary<int, List<Entity>> ResultDic = new Dictionary<int, List<Entity>>();
            int SprayPumpMaxFloor = 0;//喷淋泵最高楼层
            int SprayPumpMinFloor = 0;//喷淋泵最低楼层
            int SprayPumpCount = 0;//喷淋泵个数
            int PressureSwitchMaxFloor = 0;//灭火系统压力开关最高楼层
            int PressureSwitchMinFloor = 0;//灭火系统压力开关最低楼层
            List<int> PressureSwitchDrawLowFloorNoList = new List<int>();
            for (int FloorNum = 0; FloorNum < AllFireDistrictData.Count; FloorNum++)
            {
                List<Entity> Result = new List<Entity>();
                //拿到该防火分区数据
                var AreaData = AllFireDistrictData[FloorNum];
                if (AreaData.Data.BlockData.BlockStatistics["喷淋泵"] > 0)
                {
                    SprayPumpCount += AreaData.Data.BlockData.BlockStatistics["喷淋泵"];
                    SprayPumpMaxFloor = FloorNum + 1;
                    if (SprayPumpMinFloor == 0)
                        SprayPumpMinFloor = FloorNum + 1;
                }
                if (AreaData.Data.BlockData.BlockStatistics["灭火系统压力开关"] > 0)
                {
                    if (PressureSwitchMinFloor == 0)
                        PressureSwitchMinFloor = FloorNum + 1;
                    PressureSwitchMaxFloor = FloorNum + 1;
                    if (SprayPumpMinFloor > 0)
                    {
                        Result.AddRange(DrawSprayPumpLine(currentIndex, FloorNum, AreaData.Data.BlockData.BlockStatistics["喷淋泵"] > 0));
                    }
                    else
                    {
                        PressureSwitchDrawLowFloorNoList.Add(FloorNum);
                    }
                }
                ResultDic.Add(FloorNum + 1, Result);
            }
            PressureSwitchDrawLowFloorNoList.ForEach(FloorNum =>
            {
                ResultDic[FloorNum + 1].AddRange(DrawSprayPumpLineLowFloor(currentIndex, FloorNum, SprayPumpMinFloor > 0));
            });
            //都存在，才画
            if (PressureSwitchMaxFloor > 0)
            {
                if (SprayPumpMinFloor > 0)
                {
                    if (PressureSwitchMaxFloor == SprayPumpMinFloor)
                    {
                        Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 1700, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 450, 0));
                        ResultDic[PressureSwitchMaxFloor].Add(Endline1);
                    }
                    else if (PressureSwitchMaxFloor > SprayPumpMinFloor)
                    {
                        Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * SprayPumpMinFloor - 1700, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * SprayPumpMinFloor, 0));
                        ResultDic[SprayPumpMinFloor].Add(Endline1);
                        for (int floorNum = SprayPumpMinFloor + 1; floorNum < PressureSwitchMaxFloor; floorNum++)
                        {
                            Entity Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum, 0));
                            ResultDic[floorNum].Add(Endline);
                        }
                        Line Endline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 450, 0));
                        ResultDic[PressureSwitchMaxFloor].Add(Endline2);
                    }

                    if (PressureSwitchMinFloor < SprayPumpMinFloor)
                    {
                        Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMinFloor - 250, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMinFloor, 0));
                        ResultDic[PressureSwitchMinFloor].Add(Endline1);
                        for (int floorNum = PressureSwitchMinFloor + 1; floorNum < SprayPumpMinFloor; floorNum++)
                        {
                            Entity Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum, 0));
                            ResultDic[floorNum].Add(Endline);
                        }
                        Line Endline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * SprayPumpMinFloor - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * SprayPumpMinFloor - 1700, 0));
                        ResultDic[SprayPumpMinFloor].Add(Endline2);
                    }
                }
                else
                {
                    for (int floorNum =  1; floorNum < PressureSwitchMaxFloor; floorNum++)
                    {
                        Entity Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum, 0));
                        ResultDic[floorNum].Add(Endline);
                    }
                    Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 450, 0));
                    ResultDic[PressureSwitchMaxFloor].Add(Endline1);
                }
            }
            //设置线型
            ResultDic.Values.ForEach(x =>
            {
                x.ForEach(o =>
                {
                    o.Linetype = this.CircuitLinetype;
                    o.Layer = this.CircuitLayer;
                    o.ColorIndex = this.CircuitColorIndex;
                });
            });
            if (SprayPumpMaxFloor > 0)
            {
                for (int floorNum = 1; floorNum < SprayPumpMaxFloor; floorNum++)
                {
                    Entity Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * floorNum - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * floorNum, 0))
                    {
                        Linetype = "ByLayer",
                        Layer = "E-FAS-WIRE4",
                        ColorIndex = (int)ColorIndex.BYLAYER //4
                    };
                    ResultDic[floorNum].Add(Endline);
                }
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * (SprayPumpMaxFloor - 1), 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * SprayPumpMaxFloor - 1900, 0))
                {
                    Linetype = "ByLayer",
                    Layer = "E-FAS-WIRE4",
                    ColorIndex = (int)ColorIndex.BYLAYER //4
                };
                ResultDic[SprayPumpMaxFloor].Add(Endline1);
            }
            if (FireCompartmentParameter.FixedPartType != 3)
            {
                if (SprayPumpMinFloor > 0)
                {
                    InsertBlockService.InsertSprinklerPump(new Vector3d(0, OuterFrameLength * (SprayPumpMinFloor - 1), 0));
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.SprinklerPumpManualControlCircuitModuleContainsFireRoom : ThAutoFireAlarmSystemCommon.SprinklerPumpManualControlCircuitModuleExcludingFireRoom);
                    InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (20 - 1) + 650, OuterFrameLength * 0 - 1000, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", SprayPumpCount.ToString() } });
                }
                else if (PressureSwitchMinFloor > 0)
                {
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.SprinklerPumpDirectStartSignalLineModuleContainsFireRoom : ThAutoFireAlarmSystemCommon.SprinklerPumpDirectStartSignalLineModuleExcludingFireRoom);
                }
            }
            return ResultDic;
        }

        private List<Entity> DrawSprayPumpLineLowFloor(int CurrentIndex, int floorNum, bool IsUp)
        {
            List<Entity> result = new List<Entity>();
            Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 1650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0));
            result.Add(Endline1);
            Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 2650, 0));
            result.Add(Endline2);
            if (IsUp)
            {
                Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2750, 0));
                result.Add(Endline3);
            }
            else
            {
                Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2550, 0));
                result.Add(Endline3);
            }
            return result;
        }
        private List<Entity> DrawSprayPumpLine(int CurrentIndex, int floorNum, bool drawSprinklerPump)
        {
            List<Entity> result = new List<Entity>();
            if (drawSprinklerPump)
            {
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 1650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0));
                result.Add(Endline1);

                Line Endline5 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 2650, 0));
                result.Add(Endline5);

                Line Endline6 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2550, 0));
                result.Add(Endline6);

                //Line Endline7 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2550, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 1300, 0));
                //result.Add(Endline7);

                Line Endline8 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset - 100, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 1300, 0));
                result.Add(Endline8);

                Line Endline9 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1750, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 1200, 0));
                result.Add(Endline9);
            }
            else
            {
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 1650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0));
                result.Add(Endline1);
                Line Endline5 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 2650, 0));
                result.Add(Endline5);

                Line Endline6 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2550, 0));
                result.Add(Endline6);
            }
            return result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = (int)ColorIndex.BYLAYER;
            this.CircuitLayer = "E-CTRL-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "BORDER2";
            this.StartIndexBlock = 18;
            this.Offset = 2700;
            this.EndIndexBlock = 20;
        }
    }
}
