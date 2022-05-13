using System.Linq;
using System.Collections.Generic;

namespace ThMEPStructure.ArchiecturePlane.Service
{
    internal class ThArchPrintLayerManager
    {
        public static string CommonLayer = "0";

        public static List<string> AllLayers
        {
            get
            {
                var layers = new List<string>() { };
                return layers.Distinct().ToList();
            }
        }
    }
    internal class ThArchPrintStyleManager
    {
        public static string THSTYLE3 = "TH-STYLE3";
        public static string THSTYLE1 = "TH-STYLE1";
        public static string THSTYLE2 = "TH-STYLE2";
        public static List<string> AllTextStyles
        {
            get
            {
                var styles = new List<string> {THSTYLE1,THSTYLE3 ,THSTYLE2};
                return styles.Distinct().ToList();
            }
        }
    }
    internal class ThArchPrintBlockManager
    {
        public static List<string> AllBlockNames
        {
            get
            {
                return new List<string> { };
            }
        }
    }
}
