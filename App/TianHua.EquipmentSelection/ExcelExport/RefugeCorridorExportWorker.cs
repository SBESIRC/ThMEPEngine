using OfficeOpenXml;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class RefugeCorridorExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            RefugeRoomAndCorridorModel refugeCorridorModel = fanmodel as RefugeRoomAndCorridorModel;
            setsheet.Cells["D2"].Value = fandatamodel.FanNum;
            setsheet.Cells["D3"].Value = fandatamodel.Name;
            setsheet.Cells["D4"].Value = refugeCorridorModel.WindVolume.ToString();
            setsheet.Cells["D5"].Value = refugeCorridorModel.Area_Net.ToString();
            setsheet.Cells["D6"].Value = refugeCorridorModel.AirVol_Spec.ToString();
            copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, 6, 4, targetsheet);
        }
    }
}
