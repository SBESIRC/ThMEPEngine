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
    /// 火灾自动报警、控制总线(竖线)
    /// </summary>
    public class ThAlarmControlVerticalWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            List<Entity> Result = new List<Entity>();
            int SumCount = 0;
            for (int FloorNum = 0; FloorNum < AllFireDistrictData.Count; FloorNum++)
            {
                int FindCount = 0;
                //拿到该防火分区数据
                var AreaData = AllFireDistrictData[FloorNum];
                ThAutoFireAlarmSystemCommon.AlarmControlWireCircuitBlocks.ForEach(name =>
                {
                    FindCount += AreaData.Data.BlockData.BlockStatistics[name] * ThBlockConfigModel.BlockConfig.First(x => x.UniqueName == name).CoefficientOfExpansion;//计数*权重
                });
                FindCount = FindCount / FireCompartmentParameter.ControlBusCount + 1;
                InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (StartIndexBlock - 1) + 2300, OuterFrameLength * FloorNum + 1500, 0), new Scale3d(-100, 100, 100), 0, new Dictionary<string, string>() { { "N", FindCount.ToString() } });
                SumCount += FindCount;
            }
            InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (StartIndexBlock - 1) + 1500, OuterFrameLength * 0 - 1000, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", SumCount.ToString() } });
            return Result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = 4;
            this.CircuitLayer = "E-FAS-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "CONTINUOUS";
            this.StartIndexBlock = 4;
            this.Offset = 1500;
            this.EndIndexBlock = 21;
            SpecialBlockIndex = new int[] { 5, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
        }
    }
}
