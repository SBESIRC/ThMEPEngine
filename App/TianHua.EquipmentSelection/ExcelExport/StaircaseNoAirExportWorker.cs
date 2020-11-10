using System;
using System.Linq;
using OfficeOpenXml;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class StaircaseNoAirExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            StaircaseNoAirModel StaircaseNoAir = fanmodel as StaircaseNoAirModel;
            setsheet.Cells["D2"].Value = fandatamodel.FanNum;
            setsheet.Cells["D3"].Value = fandatamodel.Name;
            setsheet.Cells["D4"].Value = Math.Max(StaircaseNoAir.QueryValue, (StaircaseNoAir.DoorOpeningVolume + StaircaseNoAir.LeakVolume)).ToString();
            setsheet.Cells["D5"].Value = (StaircaseNoAir.DoorOpeningVolume + StaircaseNoAir.LeakVolume).ToString();
            setsheet.Cells["D6"].Value = StaircaseNoAir.DoorOpeningVolume.ToString();
            setsheet.Cells["D7"].Value = StaircaseNoAir.LeakVolume.ToString();
            setsheet.Cells["D9"].Value = StaircaseNoAir.OverAk.ToString();
            setsheet.Cells["D10"].Value = "1";
            setsheet.Cells["D11"].Value = StaircaseNoAir.StairN1.ToString();
            setsheet.Cells["D12"].Value = StaircaseNoAir.LeakArea.ToString();
            setsheet.Cells["D13"].Value = "12";
            setsheet.Cells["D14"].Value = StaircaseNoAir.N2.ToString();
            setsheet.Cells["D15"].Value = StaircaseNoAir.QueryValue.ToString();
            setsheet.Cells["D16"].Value = StaircaseNoAir.Count_Floor.ToString();
            setsheet.Cells["D17"].Value = GetLoadRange(StaircaseNoAir.Load.ToString());
            setsheet.Cells["D18"].Value = GetStairLocation(StaircaseNoAir.Stair.ToString());
            setsheet.Cells["D19"].Value = GetStairSpaceState(StaircaseNoAir.Type_Area.ToString());
            int rowNo = 20;
            int rangerows = 5;
            for (int i = 1; i <= StaircaseNoAir.FrontRoomDoors2.Sum(f => f.Value.Count); i++)
            {
                setsheet.CopyRangeToNext(20, 1, 24, 4, rangerows * i);
            }
            foreach (var floor in StaircaseNoAir.FrontRoomDoors2)
            {
                for (int i = 0; i < floor.Value.Count; i++)
                {
                    setsheet.Cells["A" + rowNo].Value = floor.Key;
                    setsheet.Cells["B" + rowNo].Value = "前室疏散门" + (i + 1);
                    setsheet.Cells["D" + rowNo].Value = floor.Value[i].Height_Door_Q.ToString();
                    setsheet.Cells["D" + (rowNo + 1)].Value = floor.Value[i].Width_Door_Q.ToString();
                    setsheet.Cells["D" + (rowNo + 2)].Value = floor.Value[i].Count_Door_Q.ToString();
                    setsheet.Cells["D" + (rowNo + 3)].Value = floor.Value[i].Crack_Door_Q.ToString();
                    setsheet.Cells["D" + (rowNo + 4)].Value = floor.Value[i].Type.ToString();
                    rowNo += rangerows;
                }
            }
            copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, rowNo - 1, 4, targetsheet);
        }
    }
}
