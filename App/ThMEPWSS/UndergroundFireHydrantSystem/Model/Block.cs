using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    class Block
    {
        public class WaterSuplyBlockNames
        {
            public const string CheckValve = "截止阀";
            public const string AutoExhaustValve = "自动排气阀系统1";
            public const string PressureReducingValve = "减压阀";
            public const string VacuumBreaker = "真空破坏器";
            public const string WaterMeter = "水表1";
            public const string WaterPipeInterrupted = "水管中断";
            public const string WaterTap = "水龙头1";
            public const string Elevation = "标高";
            public const string PipeDiameter = "给水管径100";
            public const string PRValveDetail = "减压阀详图";
            public const string FloorFraming = "楼层框定";
            public const string Casing = "套管系统";
            public const string ButterflyValve = "蝶阀";
            public const string GateValve = "闸阀";
            public const string LoopMark = "消火栓环管标记";
            public const string LoopNodeMark = "消火栓环管节点标记";
            public const string FireHydrant = "室内消火栓系统";
        }


        public class WaterSuplyUtils
        {
            public static string WaterSuplyBlockFilePath
            {
                get
                {
                    return ThCADCommon.WSSDwgPath();
                }
            }
            //加载需要使用的模块
            public static void ImportNecessaryBlocks()
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
                using (AcadDatabase blockDb = AcadDatabase.Open(WaterSuplyBlockFilePath, DwgOpenMode.ReadOnly, false))//引用模块的位置
                {
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.CheckValve));//截止阀
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.AutoExhaustValve));//自动排气阀系统1
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PressureReducingValve));//减压阀
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.VacuumBreaker));//真空破坏器
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.WaterMeter));//水表1
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.WaterPipeInterrupted));//水管中断
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.WaterTap));//水龙头
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.Elevation));//标高
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PipeDiameter));//给水管经100
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.PRValveDetail));//减压阀详图
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.Casing));//套管系统
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.ButterflyValve));//蝶阀
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.GateValve));//闸阀
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.LoopMark));//
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.LoopNodeMark));//
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.FireHydrant));//
                }
            }
        }
    }
}
