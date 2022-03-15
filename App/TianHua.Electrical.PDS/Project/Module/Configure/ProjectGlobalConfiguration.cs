using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    public class ProjectGlobalConfiguration
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

        //以下内容为暂定，因为全局参数配置UI还没做好
        public static string MotorUIChoise = "分立元件";
        public static double MotorPower = 10;//kw
    }
}
