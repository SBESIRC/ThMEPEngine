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
                CircuitType = ThPDSCircuitType.EmergencyLighting,
                TextKey = "WLE",
                KV = 0.22,
                Phase = ThPDSPhase.一相,
                DemandFactor = 1.0,
                PowerFactor =  0.85,
                FireLoad = true,
            },
            new ThPDSCircuitModel
            {
                CircuitType = ThPDSCircuitType.Lighting,
                TextKey = "WL",
                KV = 0.22,
                Phase = ThPDSPhase.一相,
                DemandFactor = 0.8,
                PowerFactor =  0.85,
                FireLoad = false,
            },
            new ThPDSCircuitModel
            {
                CircuitType = ThPDSCircuitType.Socket,
                TextKey = "WS",
                KV = 0.22,
                Phase = ThPDSPhase.一相,
                DemandFactor = 0.8,
                PowerFactor =  0.85,
                FireLoad = false,
            },
            new ThPDSCircuitModel
            {
                CircuitType = ThPDSCircuitType.PowerEquipment,
                TextKey = "WE*-*",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 0.8,
                PowerFactor =  0.8,
                FireLoad = true,
            },
            new ThPDSCircuitModel
            {
                CircuitType = ThPDSCircuitType.PowerEquipment,
                TextKey = "W*-*",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 0.8,
                PowerFactor =  0.8,
                FireLoad = false,
            },
            new ThPDSCircuitModel
            {
                CircuitType = ThPDSCircuitType.PowerEquipment,
                TextKey = "WM",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 0.8,
                PowerFactor =  0.8,
                FireLoad = false,
            },
            new ThPDSCircuitModel
            {
                CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                TextKey = "WPE",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 1.0,
                PowerFactor =  0.8,
                FireLoad = true,
            },
            new ThPDSCircuitModel
            {
                CircuitType = ThPDSCircuitType.PowerEquipment,
                TextKey = "WP",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 0.8,
                PowerFactor =  0.8,
                FireLoad = false,
            },
            new ThPDSCircuitModel
            {
                CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                TextKey = "WE",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 1.0,
                PowerFactor =  0.8,
                FireLoad = true,
            },
            new ThPDSCircuitModel
            {
                CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                TextKey = "WFEL",
                KV = 0.036,
                Phase = ThPDSPhase.None,
                DemandFactor = 1.0,
                PowerFactor =  0.85,
                FireLoad = true,
            },
            new ThPDSCircuitModel
            {
                CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                TextKey = "WC",
                KV = 0.036,
                Phase = ThPDSPhase.None,
                DemandFactor = 2.0, //实际不存在，为存储方便设为2
                PowerFactor =  2.0, //实际不存在，为存储方便设为2
                FireLoad = false, //实际不存在，为存储方便设为false
            },
        };
    }
}
