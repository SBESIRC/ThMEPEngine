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
    /// 消防控制室直接手动控制
    /// </summary>
    class ThFireControlRoomVerticalWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            return new List<Entity>();
        }

        public override Dictionary<int, List<Entity>> DrawVertical()
        {
            int currentIndex = 16;
            Dictionary<int, List<Entity>> ResultDic = new Dictionary<int, List<Entity>>();
            int FireControlRoomMaxFloor = 0;//消防控制室最高楼层
            int SmokeMachineCount = 0;//防排抽烟机个数

            for (int FloorNum = AllFireDistrictData.Count - 1; FloorNum >= 0; FloorNum--)
            {
                List<Entity> Result = new List<Entity>();
                //拿到该防火分区数据
                var AreaData = AllFireDistrictData[FloorNum];
                int Count = AreaData.Data.BlockData.BlockStatistics["防排抽烟机"];
                if (Count > 0)
                {
                    SmokeMachineCount += Count;
                    FireControlRoomMaxFloor = Math.Max(FireControlRoomMaxFloor, FloorNum + 1);
                    Result.AddRange(DrawFireControlRoomLine(currentIndex, FloorNum, SmokeMachineCount));
                }
                ResultDic.Add(FloorNum + 1, Result);
            }
            //都存在，才画
            if (SmokeMachineCount > 0)
            {
                for (int floorNum = 1; floorNum < FireControlRoomMaxFloor; floorNum++)
                {
                    Line Endline = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (floorNum - 1), 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * floorNum, 0));
                    ResultDic[floorNum].Add(Endline);
                }
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (FireControlRoomMaxFloor - 1), 0), new Point3d(OuterFrameLength * (currentIndex - 1) + Offset, OuterFrameLength * (FireControlRoomMaxFloor - 1) + 1900, 0));
                ResultDic[FireControlRoomMaxFloor].Add(Endline1);
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

                //画手动控制线路模块
                if (FireCompartmentParameter.FixedPartType != 3)
                {
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.ManualControlCircuitModuleContainsFireRoom : ThAutoFireAlarmSystemCommon.ManualControlCircuitModuleExcludingFireRoom);

                    InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (16 - 1) + 650, OuterFrameLength * 0 - 1000, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", SmokeMachineCount.ToString() } });
                }
            }
            else
                ResultDic = new Dictionary<int, List<Entity>>();
            return ResultDic;
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

            var objid= InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 650, OuterFrameLength * floorNum + 350, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", smokeMachineCount.ToString() } });
            using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            {
                BlockReference br = acad.Element<BlockReference>(objid);
                result.Add(br);
            }
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
