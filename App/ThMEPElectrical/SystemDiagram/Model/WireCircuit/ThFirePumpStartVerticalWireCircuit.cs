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
    /// 消防泵启动信号线
    /// </summary>
    public class ThFirePumpStartVerticalWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            return new List<Entity>();
        }

        public override Dictionary<int, List<Entity>> DrawVertical()
        {
            int currentIndex = 19;
            Dictionary<int, List<Entity>> ResultDic = new Dictionary<int, List<Entity>>();
            int PressureSwitchMaxFloor = 0;//灭火系统流量开关最高楼层
            int PressureSwitchMinFloor = 0;//灭火系统流量开关最低楼层
            int FireHydrantPumpMaxFloor = 0;//消火栓泵最高楼层
            int FireHydrantPumpMinFloor = 0;//消火栓泵最低楼层
            int FireHydrantPumpCount = 0;//消火栓泵个数
            for (int FloorNum = 0; FloorNum < AllFireDistrictData.Count; FloorNum++)
            {
                List<Entity> Result = new List<Entity>();
                //拿到该防火分区数据
                var AreaData = AllFireDistrictData[FloorNum];
                if (AreaData.Data.BlockData.BlockStatistics["灭火系统流量开关"] > 0)
                {
                    if (PressureSwitchMinFloor == 0)
                        PressureSwitchMinFloor = FloorNum + 1;
                    PressureSwitchMaxFloor = FloorNum + 1;
                    Result.AddRange(DrawFirePumpStartLine(currentIndex, FloorNum));
                }
                if (AreaData.Data.BlockData.BlockStatistics["消火栓泵"] > 0)
                {
                    FireHydrantPumpCount += AreaData.Data.BlockData.BlockStatistics["消火栓泵"];
                    FireHydrantPumpMaxFloor = FloorNum + 1;
                    if (FireHydrantPumpMinFloor == 0)
                        FireHydrantPumpMinFloor = FloorNum + 1;
                    Result.AddRange(DrawFireHydrantPumpLine(currentIndex, FloorNum));
                }
                ResultDic.Add(FloorNum + 1, Result);
            }
            //都存在，才画
            if (PressureSwitchMinFloor > 0)
            {
                if (FireHydrantPumpMinFloor > 0)
                {
                    if (PressureSwitchMaxFloor == FireHydrantPumpMinFloor)
                    {
                        Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * FireHydrantPumpMinFloor - 1700, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 250, 0));
                        ResultDic[FireHydrantPumpMinFloor].Add(Endline1);
                    }
                    else if (PressureSwitchMaxFloor > FireHydrantPumpMinFloor)
                    {
                        Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * FireHydrantPumpMinFloor - 1700, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * FireHydrantPumpMinFloor, 0));
                        ResultDic[FireHydrantPumpMinFloor].Add(Endline1);
                        for (int floorNum = FireHydrantPumpMinFloor + 1; floorNum < PressureSwitchMaxFloor; floorNum++)
                        {
                            Entity Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum, 0));
                            ResultDic[floorNum].Add(Endline);
                        }
                        Line Endline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 250, 0));
                        ResultDic[PressureSwitchMaxFloor].Add(Endline2);
                    }

                    if (PressureSwitchMinFloor < FireHydrantPumpMinFloor)
                    {
                        Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMinFloor - 250, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMinFloor, 0));
                        ResultDic[PressureSwitchMinFloor].Add(Endline1);
                        for (int floorNum = PressureSwitchMinFloor + 1; floorNum < FireHydrantPumpMinFloor; floorNum++)
                        {
                            Entity Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum, 0));
                            ResultDic[floorNum].Add(Endline);
                        }
                        Line Endline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * FireHydrantPumpMinFloor - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * FireHydrantPumpMinFloor - 1700, 0));
                        ResultDic[FireHydrantPumpMinFloor].Add(Endline2);
                    }
                }
                else
                {
                    for (int floorNum = 1; floorNum < PressureSwitchMaxFloor; floorNum++)
                    {
                        Entity Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum - 3000, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum, 0));
                        ResultDic[floorNum].Add(Endline);
                    }
                    Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (PressureSwitchMaxFloor - 1), 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 250, 0));
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

            if (FireHydrantPumpMaxFloor > 0)
            {
                for (int floorNum = 1; floorNum < FireHydrantPumpMaxFloor; floorNum++)
                {
                    Entity Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * (floorNum - 1), 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * floorNum, 0))
                    {
                        Linetype = "ByLayer",
                        Layer = "E-FAS-WIRE4",
                        ColorIndex = (int)ColorIndex.BYLAYER// 4
                    };
                    ResultDic[floorNum].Add(Endline);
                }
                Entity Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * (FireHydrantPumpMaxFloor - 1), 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * FireHydrantPumpMaxFloor - 1900, 0))
                {
                    Linetype = "ByLayer",
                    Layer = "E-FAS-WIRE4",
                    ColorIndex = (int)ColorIndex.BYLAYER// 4
                };
                ResultDic[FireHydrantPumpMaxFloor].Add(Endline1);
            }
            //画液位信号线路模块
            if (FireCompartmentParameter.FixedPartType != 3)
            {
                if (FireHydrantPumpMinFloor > 0)
                {
                    InsertBlockService.InsertFireHydrantPump(new Vector3d(0, OuterFrameLength * (FireHydrantPumpMinFloor - 1), 0));
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.FireHydrantPumpManualControlCircuitModuleContainsFireRoom : ThAutoFireAlarmSystemCommon.FireHydrantPumpManualControlCircuitModuleExcludingFireRoom);
                    InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (19 - 1) + 650, OuterFrameLength * 0 - 1000, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", FireHydrantPumpCount.ToString() } });
                }
                else if (PressureSwitchMaxFloor > 0)
                {
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.FireHydrantPumpDirectStartSignalLineModuleContainsFireRoom : ThAutoFireAlarmSystemCommon.FireHydrantPumpDirectStartSignalLineModuleExcludingFireRoom);
                }
            }
            return ResultDic;
        }

        /// <summary>
        /// 画消火栓泵的附加线
        /// </summary>
        /// <param name="floorNum"></param>
        /// <returns></returns>
        private List<Entity> DrawFireHydrantPumpLine(int CurrentIndex, int floorNum)
        {
            List<Entity> result = new List<Entity>();
            Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset - 100, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 1300, 0));
            result.Add(Endline3);

            Line Endline4 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1750, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 1200, 0));
            result.Add(Endline4);

            Line Endline5 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 1300, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2550, 0));
            result.Add(Endline5);

            Line Endline6 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset -100, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2550, 0));
            result.Add(Endline6);
            return result;
        }

        /// <summary>
        /// 画灭火系统流量开关附加线
        /// </summary>
        /// <param name="floorNum"></param>
        /// <returns></returns>
        private List<Entity> DrawFirePumpStartLine(int CurrentIndex, int floorNum)
        {
            List<Entity> result = new List<Entity>();
            Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2250, OuterFrameLength * floorNum + 1650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2250, OuterFrameLength * floorNum + 2750, 0));
            result.Add(Endline2);

            Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2250, OuterFrameLength * floorNum + 2750, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2750, 0));
            result.Add(Endline3);
            return result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = (int)ColorIndex.BYLAYER;
            this.CircuitLayer = "E-CTRL-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "BORDER2";
            this.Offset = 2700;
        }
    }
}
