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
    /// 消防控制室直接手动控制
    /// </summary>
    class ThFireControlRoomVerticalWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            int currentIndex = 16;
            List<Entity> Result = new List<Entity>();
            int FireControlRoomMaxFloor = 0;//消防控制室最高楼层
            int SmokeMachineCount = 0;//防排抽烟机个数

            for (int FloorNum = AllFireDistrictData.Count-1; FloorNum >=0; FloorNum--)
            {
                //拿到该防火分区数据
                var AreaData = AllFireDistrictData[FloorNum];
                int Count = AreaData.Data.BlockData.BlockStatistics["防排抽烟机"];
                if (Count>0)
                {
                    SmokeMachineCount += Count;
                    FireControlRoomMaxFloor =Math.Max(FireControlRoomMaxFloor, FloorNum + 1);
                    Result.AddRange(DrawFireControlRoomLine(currentIndex, FloorNum, SmokeMachineCount));
                }
            }
            //都存在，才画
            if (SmokeMachineCount > 0)
            {
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, 0, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * FireControlRoomMaxFloor - 1900, 0));
                Result.Add(Endline1);
                //设置线型
                Result.ForEach(o =>
                {
                    o.Linetype = this.CircuitLinetype;
                    o.Layer = this.CircuitLayer;
                    o.ColorIndex = this.CircuitColorIndex;
                });
            }
            else
                Result = new List<Entity>();
            return Result;
        }

        /// <summary>
        /// 画消防控制室直接手动控制附加线
        /// </summary>
        /// <param name="floorNum"></param>
        /// <returns></returns>
        private List<Entity> DrawFireControlRoomLine(int CurrentIndex, int floorNum, int smokeMachineCount)
        {
            List<Entity> result = new List<Entity>();
            Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset, OuterFrameLength * floorNum + 1100, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset+100, OuterFrameLength * floorNum +1200, 0));
            result.Add(Endline2);

            Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset + 100, OuterFrameLength * floorNum + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + Offset+300, OuterFrameLength * floorNum + 1200, 0));
            result.Add(Endline3);

            InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 650, OuterFrameLength * floorNum + 350, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", smokeMachineCount.ToString() } });
            return result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = 4;
            this.CircuitLayer = "E-FAS-WIRE4";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "PHANTOM2";
            this.Offset = 650;
        }
    }
}
