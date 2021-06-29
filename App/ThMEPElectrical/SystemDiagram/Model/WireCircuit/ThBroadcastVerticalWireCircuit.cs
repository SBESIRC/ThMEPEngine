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
    /// 消防应急广播总线(竖线)
    /// </summary>
    public class ThBroadcastVerticalWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            List<Entity> Result = new List<Entity>();
            int SumCount = 0;
            for (int FloorNum = 0; FloorNum < AllFireDistrictData.Count; FloorNum++)
            {
                //拿到该防火分区数据
                var AreaData = AllFireDistrictData[FloorNum];
                int FindCount = AreaData.Data.BlockData.BlockStatistics["消防广播火栓强制启动模块"];
                InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (StartIndexBlock - 1) + 2300, OuterFrameLength * FloorNum + 1150, 0), new Scale3d(-100, 100, 100), 0, new Dictionary<string, string>() { { "N", FindCount.ToString() } });
                SumCount += FindCount;
            }
            InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (StartIndexBlock - 1) + 1500, OuterFrameLength * 0 - 1000, 0), new Scale3d(-100, 100, 100), Math.PI / 4, new Dictionary<string, string>() { { "N", SumCount.ToString() } });
            return Result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = 2;
            this.CircuitLayer = "E-BRST-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "BORDER";
            this.StartIndexBlock = 1;
            this.Offset = 1150;
            this.EndIndexBlock = 8;
            SpecialBlockIndex = new int[] { 5 };
        }
    }
}
