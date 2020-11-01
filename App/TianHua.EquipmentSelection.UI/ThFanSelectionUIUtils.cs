using AcHelper;
using System.IO;

namespace TianHua.FanSelection.UI
{
    public class ThFanSelectionUIUtils
    {
        public static string DefaultModelExportPath()
        {
            return Path.Combine(Active.DocumentDirectory, Active.DocumentName);
        }

        public static string DefaultModelExportCatalogPath()
        {
            return Path.Combine(DefaultModelExportPath(), ThFanSelectionUICommon.MODEL_EXPORTCATALOG);
        }
    }
}
