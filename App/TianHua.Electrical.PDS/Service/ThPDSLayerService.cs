using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSLayerService
    {
        /// <summary>
        /// 标注
        /// </summary>
        /// <returns></returns>
        public static List<string> CircuitMarkLayers()
        {
            return new List<string>
            {
                "E-UNIV-NOTE", 
                "E-*-DIMS",
            };
        }

        /// <summary>
        /// 桥架
        /// </summary>
        /// <returns></returns>
        public static List<string> CabletrayLayers()
        {
            return new List<string> 
            { 
                "E-POWR-CMTB",
                "E-LITE-CMTB",
                "E-UNIV-EL2",
                //"E-CTRL-CMTB",
            };
        }

        /// <summary>
        /// 回路
        /// </summary>
        /// <returns></returns>
        public static List<string> CableLayers()
        {
            return new List<string> 
            { 
                "E-LITE-WIRE", 
                "E-LITE-WIRE2",
                "E-LITE-WIRE-LV",
                "E-POWR-WIRE",
                "E-POWR-WIRE2",
                "E-POWR-WIRE3",
                //"E-CTRL-WIRE",
            };
        }

        public static ThPDSCircuitType SelectCircuitType(string layer)
        {
            switch(layer)
            {
                case "E-POWR-WIRE":
                    return ThPDSCircuitType.PowerEquipment;
                case "E-POWR-WIRE2":
                    return ThPDSCircuitType.Socket;
                case "E-POWR-WIRE3":
                    return ThPDSCircuitType.PowerEquipment;
                case "E-LITE-WIRE":
                    return ThPDSCircuitType.Lighting;
                case "E-LITE-WIRE2":
                    return ThPDSCircuitType.EmergencyLighting;
                case "E-LITE-WIRE-LV":
                    return ThPDSCircuitType.Lighting;
                default:
                    return ThPDSCircuitType.None;
            }
        }

        public static ObjectId CreateAITestCableLayer(this Database database)
        {
            return database.CreateAILayer("AI-TEST-CABLE", 1);
        }

        public static ObjectId CreateAITestCabletrayLayer(this Database database)
        {
            return database.CreateAILayer("AI-TEST-CABLETRAY", 2);
        }

        public static ObjectId CreateAITestDistributionLayer(this Database database)
        {
            return database.CreateAILayer("AI-TEST-DISTRIBUTION", 3);
        }
    }
}
