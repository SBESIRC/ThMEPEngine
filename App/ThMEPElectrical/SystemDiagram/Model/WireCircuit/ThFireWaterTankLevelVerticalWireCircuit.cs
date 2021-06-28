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
    /// 消防控制室消防水箱、水池液位显示
    /// </summary>
    public class ThFireWaterTankLevelVerticalWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            int currentIndex = 21;
            List<Entity> Result = new List<Entity>();
            int FireWaterTankMaxFloor = 0;// 消防水箱最高楼层
            int FireExtinguisherPoolMinFloor = 0;//消防水池最低楼层
            int FireExtinguisherPoolMaxFloor = 0;//消防水池最高楼层
            int FireExtinguisherPoolCount =0;    //消防水池个数
            for (int FloorNum = 0; FloorNum < AllFireDistrictData.Count; FloorNum++)
            {
                //拿到该防火分区数据
                var AreaData = AllFireDistrictData[FloorNum];
                if (AreaData.Data.BlockData.BlockStatistics["消防水箱"] > 0)
                {
                    FireWaterTankMaxFloor = FloorNum + 1;
                    Result.AddRange(DrawFireWaterTankLine(currentIndex, FloorNum));
                }
                if (AreaData.Data.BlockData.BlockStatistics["消防水池"] > 0)
                {
                    FireExtinguisherPoolCount += AreaData.Data.BlockData.BlockStatistics["消防水池"];
                    if (FireExtinguisherPoolMinFloor == 0)
                    {
                        FireExtinguisherPoolMinFloor = FloorNum + 1;
                    }
                    FireExtinguisherPoolMaxFloor = FloorNum + 1;
                    Result.AddRange(DrawFireExtinguisherPoolLine(currentIndex, FloorNum));
                }
            }
            //都存在，才画
            if (FireWaterTankMaxFloor > 0 || FireExtinguisherPoolMaxFloor > 0)
            {
                if (FireWaterTankMaxFloor >= FireExtinguisherPoolMaxFloor)
                {
                    Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, 0, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (FireWaterTankMaxFloor - 1) + 2350, 0));
                    Result.Add(Endline1);
                }
                else
                {
                    Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, 0, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (FireExtinguisherPoolMaxFloor - 1) + 2250, 0));
                    Result.Add(Endline1);
                }
                
                //设置线型
                Result.ForEach(o =>
                {
                    o.Linetype = this.CircuitLinetype;
                    o.Layer = this.CircuitLayer;
                    o.ColorIndex = this.CircuitColorIndex;
                });

                //画液位信号线路模块
                if (FireCompartmentParameter.FixedPartType != 3)
                {
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.LiquidLevelSignalCircuitModuleContainsFireRoom : ThAutoFireAlarmSystemCommon.LiquidLevelSignalCircuitModuleExcludingFireRoom);

                    InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (21 - 1) + Offset, OuterFrameLength * 0 - 1000, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", FireExtinguisherPoolCount.ToString() } });
                }
            }
            else
                Result = new List<Entity>();
            return Result;
        }

        /// <summary>
        /// 画消防水池附加线
        /// </summary>
        /// <param name="floorNum"></param>
        /// <returns></returns>
        private List<Entity> DrawFireExtinguisherPoolLine(int CurrentIndex, int floorNum)
        {
            List<Entity> result = new List<Entity>();
            Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1650, OuterFrameLength * floorNum + 2350, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset-100, OuterFrameLength * floorNum +2350, 0));
            result.Add(Endline2);

            Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset - 100, OuterFrameLength * floorNum + 2350, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2250, 0));
            result.Add(Endline3);
            return result;
        }

        /// <summary>
        /// 画消防水箱附加线
        /// </summary>
        /// <param name="floorNum"></param>
        /// <returns></returns>
        private List<Entity> DrawFireWaterTankLine(int CurrentIndex, int floorNum)
        {
            List<Entity> result = new List<Entity>();
            Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 4) + 1650, OuterFrameLength * floorNum + 2350, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 2350, 0));
            result.Add(Endline2);
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
