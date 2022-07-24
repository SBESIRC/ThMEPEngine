using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService.ExportWorkers
{
    public class SeparateOrSharedNaturalExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(BaseSmokeProofViewModel baseModel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, ExcelRangeCopyOperator copyoperator)
        {
            SeparateOrSharedNaturalViewModel fontroomNaturalModel = baseModel as SeparateOrSharedNaturalViewModel;
            setsheet.Cells["D2"].Value = "";
            setsheet.Cells["D3"].Value = "";
            setsheet.Cells["D4"].Value = Math.Max(fontroomNaturalModel.FinalValue, (fontroomNaturalModel.OpenDorrAirSupply + fontroomNaturalModel.VentilationLeakage)).ToString();
            setsheet.Cells["D5"].Value = (fontroomNaturalModel.OpenDorrAirSupply + fontroomNaturalModel.VentilationLeakage).ToString();
            setsheet.Cells["D6"].Value = fontroomNaturalModel.OpenDorrAirSupply.ToString();
            setsheet.Cells["D8"].Value = fontroomNaturalModel.VentilationLeakage.ToString();
            setsheet.Cells["D9"].Value = fontroomNaturalModel.OverAk.ToString();

            double v = 0.6 * fontroomNaturalModel.OverAl / (fontroomNaturalModel.OverAk + 1);
            setsheet.Cells["D10"].Value = Math.Round(v, 2).ToString();
            setsheet.Cells["D11"].Value = Math.Min(fontroomNaturalModel.FloorNum, 3).ToString();
            setsheet.Cells["D12"].Value = (fontroomNaturalModel.SectionLength * fontroomNaturalModel.SectionWidth / 1000000).ToString();
            setsheet.Cells["D13"].Value = (fontroomNaturalModel.FloorNum < 3 ? 0 : fontroomNaturalModel.FloorNum - 3).ToString();
            setsheet.Cells["D14"].Value = fontroomNaturalModel.OverAl.ToString();
            setsheet.Cells["D15"].Value = fontroomNaturalModel.OverAk.ToString();
            setsheet.Cells["D16"].Value = fontroomNaturalModel.FinalValue.ToString();
            setsheet.Cells["D17"].Value = fontroomNaturalModel.FloorNum.ToString();
            setsheet.Cells["D18"].Value = GetLoadRange(fontroomNaturalModel.FloorType.ToString());
            setsheet.Cells["D19"].Value = fontroomNaturalModel.SectionLength.ToString();
            setsheet.Cells["D20"].Value = fontroomNaturalModel.SectionWidth.ToString();

            int rowNo = 21;
            for (int i = 1; i <= fontroomNaturalModel.FrontRoomTabControl.Sum(f => f.FloorInfoItems.Count) + fontroomNaturalModel.StairRoomTabControl.Sum(f => f.FloorInfoItems.Count); i++)
            {
                //setsheet.CopyRangeToNext("A21", "D23", 3 * i);
                setsheet.CopyRangeToNext(21, 1, 23, 4, 3 * i);
            }
            foreach (var floor in fontroomNaturalModel.FrontRoomTabControl)
            {
                for (int i = 0; i < floor.FloorInfoItems.Count; i++)
                {
                    setsheet.Cells["A" + rowNo].Value = floor.FloorName;
                    setsheet.Cells["B" + rowNo].Value = "前室疏散门" + (i + 1);
                    setsheet.Cells["D" + rowNo].Value = floor.FloorInfoItems[i].DoorHeight.ToString();
                    setsheet.Cells["D" + (rowNo + 1)].Value = floor.FloorInfoItems[i].DoorWidth.ToString();
                    setsheet.Cells["D" + (rowNo + 2)].Value = floor.FloorInfoItems[i].DoorNum.ToString();
                    rowNo += 3;
                }
            }
            foreach (var floor in fontroomNaturalModel.StairRoomTabControl)
            {
                for (int i = 0; i < floor.FloorInfoItems.Count; i++)
                {
                    setsheet.Cells["A" + rowNo].Value = floor.FloorName;
                    setsheet.Cells["B" + rowNo].Value = "楼梯间疏散门" + (i + 1);
                    setsheet.Cells["D" + rowNo].Value = floor.FloorInfoItems[i].DoorHeight.ToString();
                    setsheet.Cells["D" + (rowNo + 1)].Value = floor.FloorInfoItems[i].DoorWidth.ToString();
                    setsheet.Cells["D" + (rowNo + 2)].Value = floor.FloorInfoItems[i].DoorNum.ToString();
                    rowNo += 3;
                }
            }
            copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, rowNo - 1, 4, targetsheet);
        }
    }
}
