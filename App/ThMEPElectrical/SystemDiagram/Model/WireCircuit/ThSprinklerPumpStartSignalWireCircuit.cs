using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            for (int FloorNum =  0; FloorNum  < AllFireDistrictData.Count; FloorNum++)
            {
                //拿到该防火分区数据
                var AreaData = AllFireDistrictData[FloorNum];
                int Count = AreaData.Data.BlockData.BlockStatistics["喷淋泵"];
                if (AreaData.Data.BlockData.BlockStatistics["喷淋泵"] > 0 && AreaData.Data.BlockData.BlockStatistics["灭火系统压力开关"] > 0)
                {
                    SprayPumpMaxFloor = FloorNum + 1;
                    Result.AddRange(DrawSprayPumpLine(currentIndex, FloorNum));
                }
            }
            //都存在，才画
            if (SprayPumpMaxFloor > 0)
            {
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, 0, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * SprayPumpMaxFloor - 350, 0));
                Result.Add(Endline1);
                //设置线型
                Result.ForEach(o =>
                {
                    o.Linetype = this.CircuitLinetype;
                    o.Layer = this.CircuitLayer;
                    o.ColorIndex = this.CircuitColorIndex;
                });
                Line Endline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 650, 0, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * SprayPumpMaxFloor - 1900, 0))
                {
                    Linetype = "ByLayer",
                    Layer = "E-FAS-WIRE4",
                    ColorIndex = 4
                };
                Result.Add(Endline2);
            }
            else
                Result = new List<Entity>();
            return Result;
        }

        /// <summary>
        /// 画喷淋泵附加线
        /// </summary>
        /// <param name="CurrentIndex"></param>
        /// <param name="floorNum"></param>
        /// <returns></returns>
        private List<Entity> DrawSprayPumpLine(int CurrentIndex, int floorNum)
        {
            List<Entity> result = new List<Entity>();
            Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 1650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0));
            result.Add(Endline1);

            Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 3) + 2400, OuterFrameLength * floorNum + 2650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2700, OuterFrameLength * floorNum + 2650, 0));
            result.Add(Endline2);

            Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset - 100, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 1300, 0));
            result.Add(Endline3);

            Line Endline4 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1750, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2600, OuterFrameLength * floorNum + 1200, 0));
            result.Add(Endline4);

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
