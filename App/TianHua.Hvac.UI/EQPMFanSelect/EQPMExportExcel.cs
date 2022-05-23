using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ThCADExtension;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPHVAC.EQPMFanModelEnums;
using ThMEPHVAC.EQPMFanSelect;

namespace TianHua.Hvac.UI.EQPMFanSelect
{
    class EQPMExportExcel
    {
        private string exportExcelPath = "";
        private string ParameterExcelPath = Path.Combine(ThCADCommon.SupportPath(), "DesignData", "FanPara.xlsx");
        private string CalculateExcelPath = Path.Combine(ThCADCommon.SupportPath(), "DesignData", "FanCalc.xlsx");
        public EQPMExportExcel(string savePath) 
        {
            exportExcelPath = savePath;
        }
        public void ExportFanParameter(List<ExportFanParaModel> targetFans) 
        {
            File.Copy(ParameterExcelPath, exportExcelPath, true);
            var writeExcel = new WriteExcelHelper(exportExcelPath);
            var allSheetNames = GetSheetNames(targetFans.Select(c => c.ScenarioCode).ToList());
            writeExcel.CopySheetFromIndex(allSheetNames, 0);
            writeExcel.DeleteExcelSheet(new List<int> { 0 });
            foreach (var item in allSheetNames)
            {
                var thisFans = targetFans.Where(c => item.StartsWith(c.ScenarioCode)).ToList();
                var dataTable = FanExprotToDataTable(thisFans);
                writeExcel.WriteDataTableToTemplateExcel(dataTable, item, 3);
            }
        }
        #region 导出风机参数表相关
        List<string> GetSheetNames(List<string> targetCode) 
        {
            List<string> sheetNames = new List<string>();
            targetCode = targetCode.Distinct().ToList();
            foreach (var item in targetCode) 
            {
                sheetNames.Add(string.Format("{0}风机材料表", item));
            }
            return sheetNames;
        }
        DataTable FanExprotToDataTable(List<ExportFanParaModel> targetFans) 
        {
            DataTable dataTable = new DataTable();
            //构建datatable的列
            for (int i = 0; i < 27; i++)
            {
                DataColumn column = new DataColumn();
                dataTable.Columns.Add(column);
            }
            foreach(var item in targetFans)
            {
                var dataRow = dataTable.NewRow();
                dataRow[0] = item.No;
                dataRow[1] = item.Coverage;
                dataRow[2] = item.FanForm;
                dataRow[3] = item.CalcAirVolume;
                dataRow[4] = item.FanDelivery;
                dataRow[5] = item.Pa;
                dataRow[6] = item.StaticPa;
                dataRow[7] = item.FanEnergyLevel;
                dataRow[8] = item.FanEfficiency;
                dataRow[9] = item.FanRpm;
                dataRow[10] = item.DriveMode;
                dataRow[11] = item.ElectricalEnergyLevel;
                dataRow[12] = item.MotorPower;
                dataRow[13] = item.PowerSource;
                dataRow[14] = item.ElectricalRpm;
                dataRow[15] = item.IsDoubleSpeed;
                dataRow[16] = item.IsFrequency;
                dataRow[17] = item.WS;
                dataRow[18] = item.IsFirefighting;
                dataRow[19] = item.dB;
                dataRow[20] = item.Weight;
                dataRow[21] = item.Length;
                dataRow[22] = item.Width;
                dataRow[23] = item.Height;
                dataRow[24] = item.VibrationMode;
                dataRow[25] = item.Amount;
                dataRow[26] = item.Remark;
                dataTable.Rows.Add(dataRow);
            }
            return dataTable;
        }

        #endregion

        #region 导出风机计算书相关
        public void ExportFanCalc(DataTable calcDataTable)
        {
            File.Copy(CalculateExcelPath, exportExcelPath, true);
            var writeExcel = new WriteExcelHelper(exportExcelPath);
            writeExcel.WriteDataTableToTemplateExcel(calcDataTable,"", 3);
        }
        public DataTable GetFanCalcDataTable(List<FanDataModel> targetFans)
        {
            DataTable dataTable = new DataTable();
            //构建datatable的列
            for (int i = 0; i < 27; i++)
            {
                DataColumn column = new DataColumn();
                dataTable.Columns.Add(column);
            }
            foreach (var item in targetFans)
            {
                if (string.IsNullOrEmpty(item.FanModelCCCF) || item.FanModelCCCF.Contains("无") || item.FanModelCCCF.Contains("未知"))
                    continue;
                if (item.IsChildFan)
                    continue;
                var dataRow = dataTable.NewRow();
                dataRow[0] = string.Format("{0}-{1}-{2}-{3}", item.Name, item.InstallSpace, item.InstallFloor, item.VentNum);
                dataRow[1] = item.ServiceArea;
                dataRow[2] = CommonUtil.GetEnumDescription(item.Scenario);
                dataRow[12] = item.VolumeCalcModel.AirCalcValue.ToString();
                dataRow[13] = item.AirVolume.ToString();
                dataRow[14] = item.DragModel.DuctLength;
                dataRow[15] = item.DragModel.Friction;
                dataRow[16] = item.DragModel.LocRes;
                dataRow[17] = item.DragModel.DuctResistance;
                dataRow[18] = item.DragModel.Damper;
                dataRow[19] = item.DragModel.EndReservedAirPressure;
                dataRow[20] = item.DragModel.DynPress;
                dataRow[21] = item.DragModel.CalcResistance;
                dataRow[22] = item.WindResis;
                dataRow[23] = item.FanModelTypeCalcModel.FanModelPower;
                dataTable.Rows.Add(dataRow);
                if (item.Control != EnumFanControl.TwoSpeed)
                    continue;
                var cFan = targetFans.Find(s => s.IsChildFan && s.PID == item.ID);
                if (cFan == null)
                    continue;
                var cDataRow = dataTable.NewRow();
                cDataRow[0] = "";
                cDataRow[1] = "-";
                cDataRow[2] = "-";
                cDataRow[12] = cFan.VolumeCalcModel.AirCalcValue.ToString();
                cDataRow[13] = cFan.AirVolume.ToString();
                cDataRow[14] = cFan.DragModel.DuctLength;
                cDataRow[15] = cFan.DragModel.Friction;
                cDataRow[16] = cFan.DragModel.LocRes;
                cDataRow[17] = cFan.DragModel.DuctResistance;
                cDataRow[18] = cFan.DragModel.Damper;
                cDataRow[19] = cFan.DragModel.EndReservedAirPressure;
                cDataRow[20] = cFan.DragModel.DynPress;
                cDataRow[21] = cFan.DragModel.CalcResistance;
                cDataRow[22] = cFan.WindResis;
                cDataRow[23] = cFan.FanModelTypeCalcModel.FanModelPower;
                dataTable.Rows.Add(cDataRow);
            }
            return dataTable;
        }
        #endregion
    }
}
