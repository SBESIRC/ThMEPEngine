using System.Collections.Generic;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal class ThDoorBlkConfigTable
    {
        private static Dictionary<string, string> DoorBlkConfig { get; set; }
        private static readonly ThDoorBlkConfigTable instance = new ThDoorBlkConfigTable() { };
        static ThDoorBlkConfigTable() { }
        internal ThDoorBlkConfigTable() 
        {
            DoorBlkConfig = new Dictionary<string,string>();
        }
        public static ThDoorBlkConfigTable Instance { get { return instance; } }
        
        public void Read()
        {

        }
        private Dictionary<string, string> TianHuaDefaultConfg()
        {
            var results = new Dictionary<string, string>();
            results.Add("单扇平开门", ThArchPrintBlockManager.ADoor1);
            results.Add("双扇平开门", ThArchPrintBlockManager.ADoor2);
            results.Add("子母门", ThArchPrintBlockManager.ADoor3);
            results.Add("双扇推拉门", ThArchPrintBlockManager.ADoor4);
            results.Add("四扇推拉门", ThArchPrintBlockManager.ADoor5);
            results.Add("单扇管井门", ThArchPrintBlockManager.ADoor6);
            results.Add("双扇管井门", ThArchPrintBlockManager.ADoor7);
            return results;
        }
    }
}
