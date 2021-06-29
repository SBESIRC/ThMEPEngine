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
    /// 喷淋泵启动信号线
    /// </summary>
    public class ThSprinklerPumpStartSignalVerticalWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            int currentIndex = 20;
            List<Entity> Result = new List<Entity>();
            int SprayPumpMaxFloor = 0;//喷淋泵最高楼层
            int SprayPumpMinFloor = 0;//喷淋泵最低楼层
            int SprayPumpCount = 0;//喷淋泵个数
            int PressureSwitchMaxFloor = 0;//灭火系统压力开关最高楼层
            for (int FloorNum = 0; FloorNum < AllFireDistrictData.Count; FloorNum++)
            {
                //拿到该防火分区数据
                var AreaData = AllFireDistrictData[FloorNum];
                if (AreaData.Data.BlockData.BlockStatistics["灭火系统压力开关"] > 0)
                {
                    PressureSwitchMaxFloor = FloorNum + 1;
                    Result.AddRange(DrawSprayPumpLine(currentIndex, FloorNum, AreaData.Data.BlockData.BlockStatistics["消火栓泵"] > 0, AreaData.Data.BlockData.BlockStatistics["喷淋泵"] > 0));
                }
                if (AreaData.Data.BlockData.BlockStatistics["喷淋泵"] > 0)
                {
                    SprayPumpCount += AreaData.Data.BlockData.BlockStatistics["喷淋泵"];
                    SprayPumpMaxFloor = FloorNum + 1;
                    if (SprayPumpMinFloor == 0)
                        SprayPumpMinFloor = FloorNum + 1;
                }
            }
            //都存在，才画
            if (PressureSwitchMaxFloor > 0)
            {
                if (SprayPumpMinFloor > 0)
                {
                    Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 450, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * SprayPumpMinFloor - 1700, 0));
                    Result.Add(Endline1);
                }
                else
                {
                    Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * PressureSwitchMaxFloor - 450, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, 0, 0));
                    Result.Add(Endline1);
                }
            }
            //设置线型
            Result.ForEach(o =>
            {
                o.Linetype = this.CircuitLinetype;
                o.Layer = this.CircuitLayer;
                o.ColorIndex = this.CircuitColorIndex;
            });
            if (SprayPumpMaxFloor > 0)
            {
                Line Endline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 650, 0, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * SprayPumpMaxFloor - 1900, 0))
                {
                    Linetype = "ByLayer",
                    Layer = "E-FAS-WIRE4",
                    ColorIndex = 4
                };
                Result.Add(Endline2);
            }
            if (FireCompartmentParameter.FixedPartType != 3)
            {
                if (SprayPumpMinFloor > 0)
                {
                    InsertBlockService.InsertSprinklerPump(new Vector3d(0, OuterFrameLength * (SprayPumpMinFloor - 1), 0));
                }
                else
                {
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.SprinklerPumpDirectStartSignalLineModuleContainsFireRoom : ThAutoFireAlarmSystemCommon.SprinklerPumpDirectStartSignalLineModuleExcludingFireRoom);
                }
                InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (20 - 1) + 650, OuterFrameLength * 0 - 1000, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", SprayPumpCount.ToString() } });
            }
            return Result;
        }

        private List<Entity> DrawSprayPumpLine(int CurrentIndex, int floorNum, bool drawFirePump, bool drawSprinklerPump)
        {
            List<Entity> result = new List<Entity>();
            if (drawSprinklerPump)
            {
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 1650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0));
                result.Add(Endline1);
                if (drawFirePump)
                {
                    Line newLine = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 2) + 2600, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 2) + 2700, OuterFrameLength * floorNum + 2550, 0));
                    result.Add(newLine);

                    Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 2) + Offset, OuterFrameLength * floorNum + 2550, 0), new Point3d(OuterFrameLength * (CurrentIndex - 2) + Offset, OuterFrameLength * floorNum + 1300, 0));
                    result.Add(Endline2);

                    Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 2) + Offset - 100, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 2) + Offset, OuterFrameLength * floorNum + 1300, 0));
                    result.Add(Endline3);

                    Line Endline4 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 2) + 1750, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 2) + 2600, OuterFrameLength * floorNum + 1200, 0));
                    result.Add(Endline4);
                }

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
                if (drawFirePump)
                {
                    Line newLine = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 2) + 2600, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 2) + 2700, OuterFrameLength * floorNum + 2550, 0));
                    result.Add(newLine);
                    Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 2) + Offset, OuterFrameLength * floorNum + 2550, 0), new Point3d(OuterFrameLength * (CurrentIndex - 2) + Offset, OuterFrameLength * floorNum + 1300, 0));
                    result.Add(Endline2);
                    Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 2) + Offset - 100, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 2) + Offset, OuterFrameLength * floorNum + 1300, 0));
                    result.Add(Endline3);
                    Line Endline4 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 2) + 1750, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 2) + 2600, OuterFrameLength * floorNum + 1200, 0));
                    result.Add(Endline4);
                    //Line Endline9 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 2) + 2600, OuterFrameLength * floorNum + 2650, 0));
                    //result.Add(Endline9);
                }
                Line Endline5 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 2650, 0));
                result.Add(Endline5);

                Line Endline6 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2550, 0));
                result.Add(Endline6);
            }
            return result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = 3;
            this.CircuitLayer = "E-CTRL-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "BORDER2";
            this.StartIndexBlock = 18;
            this.Offset = 2700;
            this.EndIndexBlock = 20;
        }
    }
}
