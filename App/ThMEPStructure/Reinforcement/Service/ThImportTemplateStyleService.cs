using System.Collections.Generic;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Service
{
    public static class ThImportTemplateStyleService
    {
        public static string ThStyle3TextStyle = "TH-STYLE3";
        public static void Import(this Database database)
        {
            using (var acadDb = AcadDatabase.Use(database))
            using (var blockDb = AcadDatabase.Open(ThCADCommon.ReinforceTemplateDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                Layers.ForEach(layer => acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false));
                TextStyles.ForEach(ts => acadDb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(ts),false));
                DimStyles.ForEach(ds => acadDb.DimStyles.Import(blockDb.DimStyles.ElementOrDefault(ds),false));
                LineTypes.ForEach(lt => acadDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(lt), false));
            }
        }
        private static List<string> Layers
        { 
            get
            {
                return new List<string> { 
                    "COLU_DE_TH","COLU_DE_DIM","COLU_DE_TEXT","Defpoints",
                    "LABEL" ,"LINK","REIN", "TAB", "TAB_TEXT","THIN",
                    "xk-biaozhu","z-详图标注","tab-表头"};
            }
        }
        private static List<string> TextStyles
        {
            get
            {
                return new List<string> { "TSSD_REIN", "TSSD_Norm","Bw_Dim","BW_Rein",
                    ThStyle3TextStyle};
            }
        }
        private static List<string> DimStyles
        {
            get
            {
                return new List<string> { "FT_25_100","TSSD_25_100", "TSSD_100_100", };
            }
        }
        private static List<string> LineTypes
        {
            get
            {
                return new List<string> { "DASH"};
            }
        }
    }
}
