using System.Collections.Generic;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSCircuitConfigModel
    {
        public static List<ThPDSCircuitModel> BlockConfig = new List<ThPDSCircuitModel>
            {
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.Lighting,
                    TextKey = "WL",
                    KV = 220,
                    Phase = 1,
                    DemandFactor = 0.8,
                    PowerFactor =  0.85,
                    FireLoad = false,
                },
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.Socket,
                    TextKey = "WS",
                    KV = 220,
                    Phase = 1,
                    DemandFactor = 0.8,
                    PowerFactor =  0.85,
                    FireLoad = false,
                },
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.PowerEquipment,
                    TextKey = "W*-*",
                    KV = 380,
                    Phase = 3,
                    DemandFactor = 0.8,
                    PowerFactor =  0.8,
                    FireLoad = false,
                },
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.PowerEquipment,
                    TextKey = "WE*-*",
                    KV = 380,
                    Phase = 3,
                    DemandFactor = 0.8,
                    PowerFactor =  0.8,
                    FireLoad = true,
                },
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.PowerEquipment,
                    TextKey = "WM",
                    KV = 380,
                    Phase = 3,
                    DemandFactor = 0.8,
                    PowerFactor =  0.8,
                    FireLoad = false,
                },
                
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.EmergencyLighting,
                    TextKey = "WLE",
                    KV = 220,
                    Phase = 1,
                    DemandFactor = 1.0,
                    PowerFactor =  0.85,
                    FireLoad = true,
                },
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                    TextKey = "WPE",
                    KV = 380,
                    Phase = 3,
                    DemandFactor = 1.0,
                    PowerFactor =  0.8,
                    FireLoad = true,
                },
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.PowerEquipment,
                    TextKey = "WP",
                    KV = 380,
                    Phase = 3,
                    DemandFactor = 0.8,
                    PowerFactor =  0.8,
                    FireLoad = false,
                },
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                    TextKey = "WE",
                    KV = 380,
                    Phase = 3,
                    DemandFactor = 1.0,
                    PowerFactor =  0.8,
                    FireLoad = true,
                },
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                    TextKey = "WFEL",
                    KV = 36,
                    Phase = 2, //实际不存在，为存储方便设为2
                    DemandFactor = 1.0,
                    PowerFactor =  0.85,
                    FireLoad = true,
                },
                new ThPDSCircuitModel
                {
                    CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                    TextKey = "WC",
                    KV = 36,
                    Phase = 2, //实际不存在，为存储方便设为2
                    DemandFactor = 2.0, //实际不存在，为存储方便设为2
                    PowerFactor =  2.0, //实际不存在，为存储方便设为2
                    FireLoad = false, //实际不存在，为存储方便设为false
                },
            };
    }
}
