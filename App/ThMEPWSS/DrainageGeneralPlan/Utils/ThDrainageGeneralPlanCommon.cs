using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPWSS.DrainageGeneralPlan.Utils
{
    public class ThDrainageGeneralPlanCommon
    {
        public static List<string> BlkName = new List<string>() ;

        public static string LayerName_Drai_Main = "W-DRAI-OUT-PIPE";
        public static string LayerName_Drai_Out = "W-DRAI-OUT-PIPE-out";
        public static string LayerName_Rain_Main = "W-RAIN-OUT-PIPE";
        public static string LayerName_Rain_Out = "W-RAIN-PIPE-out";
        public static List<string> LayerList = new List<string>() { LayerName_Drai_Main, LayerName_Drai_Out, LayerName_Rain_Main, LayerName_Rain_Out };

        public static List<Line> Drai_Main ;
        public static List<Polyline> Drai_Out ;
        public static List<Line> Rain_Main;
        public static List<Polyline> Rain_Out;

        public static Dictionary<Line, List<Polyline>> Drai_MainToOut ;
        public static Dictionary<Line, List<Polyline>> Rain_MainToOut ;

        public static Dictionary<Polyline, List<Line>> Drai_OutToMain ;
        public static Dictionary<Polyline, List<Line>> Rain_OutToMain;
    }
}
