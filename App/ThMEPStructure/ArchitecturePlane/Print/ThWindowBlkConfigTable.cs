using System.Collections.Generic;

namespace ThMEPStructure.ArchitecturePlane.Print
{
    internal class ThWindowBlkConfigTable
    {
        private static Dictionary<string, string> WindowBlkConfig { get; set; }
        private static readonly ThWindowBlkConfigTable instance = new ThWindowBlkConfigTable() { };
        static ThWindowBlkConfigTable() { }
        internal ThWindowBlkConfigTable() 
        {
            WindowBlkConfig = new Dictionary<string,string>();
        }
        public static ThWindowBlkConfigTable Instance { get { return instance; } }
        
        public void Read()
        {

        }
        private Dictionary<string, string> TianHuaDefaultConfg()
        {
            var results = new Dictionary<string, string>();
            results.Add("窗", ThArchPrintBlockManager.AWin1);
            return results;
        }
    }
}
