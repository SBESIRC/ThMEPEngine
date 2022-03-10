using System.Collections.Generic;
using System.Linq;
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

        public static void SelectCircuitType(ThPDSCircuit circuit, string layer)
        {
            switch(layer)
            {
                case "E-POWR-WIRE":
                    circuit.Type = ThPDSCircuitType.PowerEquipment;
                    break;
                case "E-POWR-WIRE2":
                    circuit.Type = ThPDSCircuitType.Socket;
                    break;
                case "E-POWR-WIRE3":
                    circuit.Type = ThPDSCircuitType.PowerEquipment;
                    break;
                case "E-LITE-WIRE":
                    circuit.Type = ThPDSCircuitType.Lighting;
                    break;
                case "E-LITE-WIRE2":
                    circuit.Type = ThPDSCircuitType.EmergencyLighting;
                    break;
                case "E-LITE-WIRE-LV":
                    circuit.Type = ThPDSCircuitType.Lighting;
                    break;
                default:
                    circuit.Type = ThPDSCircuitType.None;
                    break;
            }
            Assign(circuit);
        }

        private static void Assign(ThPDSCircuit circuit )
        {
            if(circuit.Type != ThPDSCircuitType.None)
            {
                var config = ThPDSCircuitConfigModel.BlockConfig.Where(o => o.CircuitType == circuit.Type).First();
                circuit.Phase = config.Phase;
                circuit.DemandFactor = config.DemandFactor;
                circuit.PowerFactor = config.PowerFactor;
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
