using AcHelper;
using System.IO;
using OfficeOpenXml;
using ThCADExtension;

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

        private static ExcelPackage CreateExcelPackage(string path)
        {
            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            return new ExcelPackage(new FileInfo(path));
        }

        public static ExcelPackage CreateModelExportExcelPackage()
        {
            return CreateExcelPackage(Path.Combine(ThCADCommon.SupportPath(), "DesignData", "FanPara.xlsx"));
        }

        public static ExcelPackage CreateModelCalculateExcelPackage()
        {
            return CreateExcelPackage(Path.Combine(ThCADCommon.SupportPath(), "DesignData", "FanCalc.xlsx"));
        }

        public static ExcelPackage CreateSmokeProofExcelPackage()
        {
            return CreateExcelPackage(Path.Combine(ThCADCommon.SupportPath(), "DesignData", "SmokeProofScenario.xlsx"));
        }

        public static ExcelPackage CreateSmokeDischargeExcelPackage()
        {
            return CreateExcelPackage(Path.Combine(ThCADCommon.SupportPath(), "DesignData", "SmokeDischargeScenario.xlsx"));
        }
    }
}
