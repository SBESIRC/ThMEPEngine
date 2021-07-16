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
    /// 消防控制室消防水箱、水池液位显示
    /// </summary>
    public class ThFireWaterTankLevelVerticalWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            return new List<Entity>();
        }

        public override Dictionary<int, List<Entity>> DrawVertical()
        {
            int currentIndex = 21;
            Dictionary<int, List<Entity>> ResultDic = new Dictionary<int, List<Entity>>();
            int FireWaterTankMaxFloor = 0;// 消防水箱最高楼层
            int FireExtinguisherPoolMinFloor = 0;//消防水池最低楼层
            int FireExtinguisherPoolMaxFloor = 0;//消防水池最高楼层
            int FireExtinguisherPoolCount = 0;    //消防水池个数

            for (int FloorNum = 0; FloorNum < AllFireDistrictData.Count; FloorNum++)
            {
                List<Entity> Result = new List<Entity>();
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
                ResultDic.Add(FloorNum + 1, Result);
            }
            //都存在，才画
            if (FireWaterTankMaxFloor > 0 || FireExtinguisherPoolMaxFloor > 0)
            {
                int MaxFloor = Math.Max(FireWaterTankMaxFloor, FireExtinguisherPoolMaxFloor);
                if (FireWaterTankMaxFloor == MaxFloor)
                {
                    for (int floorNum = 1; floorNum < MaxFloor; floorNum++)
                    {
                        Line Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (floorNum - 1), 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum, 0));
                        ResultDic[floorNum].Add(Endline);
                    }
                    Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (MaxFloor - 1), 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (MaxFloor - 1) + 2350, 0));
                    ResultDic[MaxFloor].Add(Endline1);
                }
                else
                {
                    for (int floorNum = 1; floorNum < MaxFloor; floorNum++)
                    {
                        Line Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (floorNum - 1), 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum, 0));
                        ResultDic[floorNum].Add(Endline);
                    }
                    Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (MaxFloor - 1), 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (MaxFloor - 1) + 2250, 0));
                    ResultDic[MaxFloor].Add(Endline1);
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

                //画液位信号线路模块
                if (FireCompartmentParameter.FixedPartType != 3)
                {
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.LiquidLevelSignalCircuitModuleContainsFireRoom : ThAutoFireAlarmSystemCommon.LiquidLevelSignalCircuitModuleExcludingFireRoom);

                    InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (21 - 1) + Offset, OuterFrameLength * 0 - 1000, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", FireExtinguisherPoolCount.ToString() } });
                }
            }
            else
                ResultDic = new Dictionary<int, List<Entity>>();
            return ResultDic;
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
