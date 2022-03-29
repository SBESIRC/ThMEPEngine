using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    public class ProjectSystemConfiguration
    {
        public static string urlFolder = Path.Combine(ThCADCommon.SupportPath(), "PowerDistributionSystem");
        public static string CircuitBreakerUrl = Path.Combine(urlFolder, "LV_AC_CircuitBreaker_ImportLibrary_19DX101.xlsx");
        public static string ATSEUrl = Path.Combine(urlFolder, "LV_AC_ATSE_ImportLibrary_19DX101.xlsx");
        public static string ContactorUrl = Path.Combine(urlFolder, "LV_AC_Contactor_19DX101.xlsx");
        public static string IsolatorUrl = Path.Combine(urlFolder, "LV_AC_Isolator_19DX101.xlsx");
        public static string MTSEUrl = Path.Combine(urlFolder, "LV_AC_MTSE_ImportLibrary_19DX101.xlsx");
        public static string ThermalRelayUrl = Path.Combine(urlFolder, "LV_AC_ThermalRelay_19DX101.xlsx");
        public static string BuswayUrl = Path.Combine(urlFolder, "LV_Busway_Selector_Default.xlsx");
        public static string ConductorUrl = Path.Combine(urlFolder, "LV_Conductor_Selector_Default.xlsx");
        public static string CurrentTransformerUrl = Path.Combine(urlFolder, "LV_Current_Transformer_19DX101.xlsx");
        public static string CableCondiutUrl = Path.Combine(urlFolder, "LV_AC_Cable_Condiut_MatchTable_19DX101.xlsx");
        public static string MotorTypeOneCoordinationUrl = Path.Combine(urlFolder, "LV_AC_Motor_TypeOneCoordination_19DX101.xlsx");
        public static string MotorTypeTwoCoordinationUrl = Path.Combine(urlFolder, "LV_AC_Motor_TypeTwoCoordination_19DX101.xlsx");

        public static List<string> SinglePhasePolesNum = new List<string> { "1P", "1P+N", "2P" };
        public static List<string> ThreePhasePolesNum = new List<string> { "3P", "3P+N", "4P" };
}
}
