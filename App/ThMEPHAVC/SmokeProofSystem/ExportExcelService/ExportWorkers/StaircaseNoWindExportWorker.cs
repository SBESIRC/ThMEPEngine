﻿using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService.ExportWorkers
{
    class StaircaseNoWindExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(BaseSmokeProofViewModel baseModel, string systemName, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, ExcelRangeCopyOperator copyoperator)
        {
            StaircaseNoWindViewModel StaircaseNoAir = baseModel as StaircaseNoWindViewModel;
            setsheet.Cells["D2"].Value = systemName;
            setsheet.Cells["D4"].Value = Math.Max(StaircaseNoAir.FinalValue, (StaircaseNoAir.OpenDorrAirSupply + StaircaseNoAir.VentilationLeakage)).ToString();
            setsheet.Cells["D5"].Value = (StaircaseNoAir.OpenDorrAirSupply + StaircaseNoAir.VentilationLeakage).ToString();
            setsheet.Cells["D6"].Value = StaircaseNoAir.OpenDorrAirSupply.ToString();
            setsheet.Cells["D7"].Value = StaircaseNoAir.VentilationLeakage.ToString();
            setsheet.Cells["D9"].Value = StaircaseNoAir.OverAk.ToString();
            setsheet.Cells["D10"].Value = "1";
            setsheet.Cells["D11"].Value = StaircaseNoAir.StairN1.ToString();
            setsheet.Cells["D12"].Value = Math.Round(StaircaseNoAir.LeakArea, 2).ToString();
            setsheet.Cells["D13"].Value = "12";
            setsheet.Cells["D14"].Value = Math.Round(StaircaseNoAir.N2, 2).ToString();
            setsheet.Cells["D15"].Value = StaircaseNoAir.FinalValue.ToString();
            setsheet.Cells["D16"].Value = StaircaseNoAir.FloorNum.ToString();
            setsheet.Cells["D17"].Value = GetLoadRange(StaircaseNoAir.FloorType.ToString());
            setsheet.Cells["D18"].Value = GetStairLocation(StaircaseNoAir.StairPosition.ToString());
            setsheet.Cells["D19"].Value = GetStairSpaceState(StaircaseNoAir.BusinessType.ToString());
            int rowNo = 20;
            int rangerows = 5;
            for (int i = 1; i <= StaircaseNoAir.FrontRoomTabControl.Sum(f => f.FloorInfoItems.Count); i++)
            {
                setsheet.CopyRangeToNext(20, 1, 24, 4, rangerows * i);
            }
            foreach (var floor in StaircaseNoAir.FrontRoomTabControl)
            {
                for (int i = 0; i < floor.FloorInfoItems.Count; i++)
                {
                    setsheet.Cells["A" + rowNo].Value = floor.FloorName;
                    setsheet.Cells["B" + rowNo].Value = "前室疏散门" + (i + 1);
                    setsheet.Cells["D" + rowNo].Value = floor.FloorInfoItems[i].DoorHeight.ToString();
                    setsheet.Cells["D" + (rowNo + 1)].Value = floor.FloorInfoItems[i].DoorWidth.ToString();
                    setsheet.Cells["D" + (rowNo + 2)].Value = floor.FloorInfoItems[i].DoorNum.ToString();
                    setsheet.Cells["D" + (rowNo + 3)].Value = floor.FloorInfoItems[i].DoorSpace.ToString();
                    setsheet.Cells["D" + (rowNo + 4)].Value = floor.FloorInfoItems[i].DoorType.ToString();
                    rowNo += rangerows;
                }
            }
            copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, rowNo - 1, 4, targetsheet);
        }
    }
}
