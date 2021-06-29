using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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
            int currentIndex = 19;
            List<Entity> Result = new List<Entity>();
            int PressureSwitchMaxFloor = 0;//灭火系统流量开关最高楼层
            int FireHydrantPumpMaxFloor = 0;//消火栓泵最高楼层
            int FireHydrantPumpMinFloor = 0;//消火栓泵最低楼层
            int FireHydrantPumpCount = 0;//消火栓泵个数
            for (int FloorNum = 0; FloorNum < AllFireDistrictData.Count; FloorNum++)
            {
                //拿到该防火分区数据
                var AreaData = AllFireDistrictData[FloorNum];
                if (AreaData.Data.BlockData.BlockStatistics["灭火系统流量开关"] > 0)
                {
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
            }
            //都存在，才画
            if (FireHydrantPumpMinFloor > 0 && PressureSwitchMaxFloor > FireHydrantPumpMinFloor)
            {
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * FireHydrantPumpMinFloor - 1700, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 250, 0));
                Result.Add(Endline1);
            }
            else
            {
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, 0, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 250, 0));
                Result.Add(Endline1);
            }
            //设置线型
            Result.ForEach(o =>
            {
                o.Linetype = this.CircuitLinetype;
                o.Layer = this.CircuitLayer;
                o.ColorIndex = this.CircuitColorIndex;
            });
            if (FireHydrantPumpMaxFloor > 0)
            {
                Line Endline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 650, 0, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * FireHydrantPumpMaxFloor - 1900, 0))
                {
                    Linetype = "ByLayer",
                    Layer = "E-FAS-WIRE4",
                    ColorIndex = 4
                };
                Result.Add(Endline2);
            }
            //画液位信号线路模块
            if (FireCompartmentParameter.FixedPartType != 3)
            {
                if (FireHydrantPumpMinFloor > 0)
                {
                    InsertBlockService.InsertFireHydrantPump(new Vector3d(0, OuterFrameLength * (FireHydrantPumpMinFloor - 1), 0));
                }
                else if (PressureSwitchMaxFloor > 0)
                {
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.FireHydrantPumpDirectStartSignalLineModuleContainsFireRoom : ThAutoFireAlarmSystemCommon.FireHydrantPumpDirectStartSignalLineModuleExcludingFireRoom);
                }
                InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.FirePumpRoomCircuitModuleContainsFireRoom : ThAutoFireAlarmSystemCommon.FirePumpRoomCircuitModuleExcludingFireRoom);

                InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (19 - 1) + 650, OuterFrameLength * 0 - 1000, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", FireHydrantPumpCount.ToString() } });
                
            }
            return Result;
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
            this.CircuitColorIndex = 3;
            this.CircuitLayer = "E-CTRL-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "BORDER2";
            this.Offset = 2700;
        }
    }
}
