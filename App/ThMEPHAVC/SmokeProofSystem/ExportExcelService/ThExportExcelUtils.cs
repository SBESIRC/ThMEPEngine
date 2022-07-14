using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService
{
    public class ThExportExcelUtils
    {
        private static ExcelPackage CreateExcelPackage(string path)
        {
            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            return new ExcelPackage(new FileInfo(path));
        }

        public static ExcelPackage CreateModelCalculateExcelPackage()
        {
            return CreateExcelPackage(Path.Combine(ThCADCommon.SupportPath(), "DesignData", "SmokeProofCalc.xlsx"));
        }

        public static ExcelPackage CreateSmokeProofExcelPackage()
        {
            return CreateExcelPackage(Path.Combine(ThCADCommon.SupportPath(), "DesignData", "SmokeProofScenario.xlsx"));
        }
    }
}
