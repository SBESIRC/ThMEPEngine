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
            public const string WaterPipeInterrupted = "水管中断";
            public const string LoopMark = "消火栓环管标记";
            public const string SubLoopMark = "消火栓环管节点标记";
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
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.WaterPipeInterrupted));//水管中断
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.LoopMark));//消火栓环管标记
                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.SubLoopMark));//消火栓环管节点标记
                }
            }
        }
    }
}
