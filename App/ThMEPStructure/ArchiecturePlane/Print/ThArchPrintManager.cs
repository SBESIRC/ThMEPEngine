using System;
using System.Collections.Generic;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal class ThArchPrinterManager
    {
        public readonly static string DoorWindowDwgName = "建筑门窗填充样式文件";
        public readonly static string PlaneTemplateDwgName = "建筑平、立、剖图示意";
    }
    internal class ThArchPrintLayerManager: ThArchPrinterManager
    {
        // public
        public readonly static string CommonLayer = "0";
        public readonly static string AEWIND = "AE-WIND";
        public readonly static string AEWALL = "AE-WALL";
        public readonly static string AEDOORINSD = "AE-DOOR-INSD";
        public readonly static string AESTRUHACH = "AE-STRU-HACH";        
        public readonly static string DEFPOINTS = "DEFPOINTS";
        public readonly static string DEFPOINTS1 = "DEFPOINTS-1";
        #region----------建筑门窗填充样式文件------------
        public readonly static string ADDIMSINSD = "AD-DIMS-INSD";
        public readonly static string ADNAMEROOM = "AD-NAME-ROOM";
        public readonly static string ADPOSTARCH = "AD-POST-ARCH";        
        #endregion
        #region----------建筑平、立、剖图示意----------        
        public readonly static string AEELEV3 = "AE-ELEV-3";
        public readonly static string AEFLOR = "AE-FLOR";
        public readonly static string AEFNSH = "AE-FNSH";
        public readonly static string AEHDWR = "AE-HDWR";
        public readonly static string AEWALLHACH = "AE-WALL-HACH";
        public readonly static string SDETLHACH = "S-DETL-HACH";
        public readonly static string SBEAM = "S_BEAM";
        #endregion
        public Dictionary<string, HashSet<string>> DwgLayerInfos { get; private set; }
        private static readonly ThArchPrintLayerManager instance =
            new ThArchPrintLayerManager() { };
        static ThArchPrintLayerManager()
        {
        }
        internal ThArchPrintLayerManager()
        {
            DwgLayerInfos = new Dictionary<string, HashSet<string>>();
            var group1s = new HashSet<string>();
            group1s.Add(ADDIMSINSD);
            group1s.Add(ADNAMEROOM);
            group1s.Add(ADPOSTARCH);
            group1s.Add(AEDOORINSD);
            group1s.Add(AESTRUHACH);
            group1s.Add(AEWIND);
            group1s.Add(DEFPOINTS);
            DwgLayerInfos.Add(DoorWindowDwgName, group1s);

            var group2s = new HashSet<string>();
            group2s.Add(AEELEV3);
            group2s.Add(AEFLOR);
            group2s.Add(AEFNSH);
            group2s.Add(AEHDWR);
            group2s.Add(AEWIND);
            group2s.Add(AEWALL);
            group2s.Add(AEDOORINSD);
            group2s.Add(AESTRUHACH);
            group2s.Add(AEWALLHACH);
            group2s.Add(DEFPOINTS);
            group2s.Add(DEFPOINTS1);
            group2s.Add(SDETLHACH);
            group2s.Add(SBEAM);
            DwgLayerInfos.Add(PlaneTemplateDwgName, group2s);
        }
        public static ThArchPrintLayerManager Instance { get { return instance; } }
    }
    internal class ThArchPrintStyleManager: ThArchPrinterManager
    {
        public readonly static string THSTYLE1 = "TH-STYLE1";
        public readonly static string THSTYLE2 = "TH-STYLE2"; // 暂时未使用
        #region----------建筑门窗填充样式文件------------
        public readonly static string THSTYLE3 = "TH-STYLE3";
        #endregion
        #region----------建筑平、立、剖图示意----------  
        //
        #endregion
        public Dictionary<string, HashSet<string>> DwgStyleInfos { get; private set; }        
        private static readonly ThArchPrintStyleManager instance =
            new ThArchPrintStyleManager() { };
        static ThArchPrintStyleManager()
        {
        }
        internal ThArchPrintStyleManager()
        {
            DwgStyleInfos = new Dictionary<string, HashSet<string>>();
            var group1s = new HashSet<string>();
            group1s.Add(THSTYLE1);
            group1s.Add(THSTYLE3);
            DwgStyleInfos.Add(DoorWindowDwgName, group1s);

            var group2s = new HashSet<string>();
            group2s.Add(THSTYLE1);
            DwgStyleInfos.Add(PlaneTemplateDwgName, group2s);
        }
        public static ThArchPrintStyleManager Instance { get { return instance; } }
    }
    internal class ThArchPrintBlockManager:ThArchPrinterManager
    {
        #region----------建筑门窗填充样式文件------------
        /// <summary>
        /// 单扇平开门
        /// </summary>
        public readonly static string ADoor1 = "A-door-1";
        /// <summary>
        /// 双扇平开门
        /// </summary>
        public readonly static string ADoor2 = "A-door-2";
        /// <summary>
        /// 子母门
        /// </summary>
        public readonly static string ADoor3 = "A-door-3";
        /// <summary>
        /// 双扇推拉门
        /// </summary>
        public readonly static string ADoor4 = "A-door-4";
        /// <summary>
        /// 四扇推拉门
        /// </summary>
        public readonly static string ADoor5 = "A-door-5";
        /// <summary>
        /// 单扇管井门
        /// </summary>
        public readonly static string ADoor6 = "A-door-6";
        /// <summary>
        /// 双扇管井门
        /// </summary>
        public readonly static string ADoor7 = "A-door-7";
        /// <summary>
        /// 窗户
        /// </summary>
        public readonly static string AWin1 = "A-win-1"; 
        #endregion
        public Dictionary<string,HashSet<string>> DwgBlockInfos { get; private set; } 
        private static readonly ThArchPrintBlockManager instance = 
            new ThArchPrintBlockManager() { };        
        public Dictionary<string, Tuple<double, double>> WindowThickReferenceTbl { get; private set; }
        static ThArchPrintBlockManager()
        {
        }
        internal ThArchPrintBlockManager()
        {
            DwgBlockInfos = new Dictionary<string, HashSet<string>>();
            var group1s = new HashSet<string>();
            group1s.Add(ADoor1);
            group1s.Add(ADoor2);
            group1s.Add(ADoor3);
            group1s.Add(ADoor4);
            group1s.Add(ADoor5);
            group1s.Add(ADoor6);
            group1s.Add(ADoor7);
            group1s.Add(AWin1);
            DwgBlockInfos.Add(DoorWindowDwgName, group1s);

            // 墙厚
            WindowThickReferenceTbl = new Dictionary<string, Tuple<double, double>>();
            WindowThickReferenceTbl.Add(AWin1,Tuple.Create(100.0,30.0));
        }
        public static ThArchPrintBlockManager Instance { get { return instance; } }
    }
    internal class ThArchPrintLineTypeManager
    { 
        public readonly static string Hidden = "Hidden";
    }
}
