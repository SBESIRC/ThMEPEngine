using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.SmokeProofSystem.Model;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService
{
    public class ExcelExportEngine
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ExcelExportEngine instance = new ExcelExportEngine();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ExcelExportEngine() { }
        internal ExcelExportEngine() { }
        public static ExcelExportEngine Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public ExcelWorkbook Sourcebook { get; set; }
        public ExcelWorksheet Targetsheet { get; set; }
        public VolumeExportModel Model { get; set; }
        public ExcelRangeCopyOperator RangeCopyOperator { get; set; }

        public void Run()
        {
            if (Model.baseSmokeProofViewModel == null)
            {
                return;
            }
            var worker = BaseExportWorker.Create(Model.baseSmokeProofViewModel);
            if (worker != null)
            {
                var sourcesheet = Sourcebook.GetSheetFromSheetName(Model.ScenarioTitle);
                worker.ExportToExcel(Model.baseSmokeProofViewModel, sourcesheet, Targetsheet, RangeCopyOperator);
            }
        }
    }
}
