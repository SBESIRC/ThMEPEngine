using System;
using System.Linq;
using OfficeOpenXml;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class StaircaseAirExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            StaircaseAirModel StaircaseAir = fanmodel as StaircaseAirModel;
            setsheet.Cells["D2"].Value = fandatamodel.FanNum;
            setsheet.Cells["D3"].Value = fandatamodel.Name;
            setsheet.Cells["D4"].Value = Math.Max(StaircaseAir.QueryValue, (StaircaseAir.DoorOpeningVolume + StaircaseAir.LeakVolume)).ToString();
            setsheet.Cells["D5"].Value = (StaircaseAir.DoorOpeningVolume + StaircaseAir.LeakVolume).ToString();
            setsheet.Cells["D6"].Value = StaircaseAir.DoorOpeningVolume.ToString();
            setsheet.Cells["D7"].Value = StaircaseAir.LeakVolume.ToString();
            setsheet.Cells["D9"].Value = StaircaseAir.OverAk.ToString();
            setsheet.Cells["D10"].Value = "1";
            setsheet.Cells["D11"].Value = StaircaseAir.StairN1.ToString();
            setsheet.Cells["D12"].Value = Math.Round(StaircaseAir.LeakArea,2).ToString();
            setsheet.Cells["D13"].Value = "12";
            setsheet.Cells["D14"].Value = Math.Round(StaircaseAir.N2,2).ToString();
            setsheet.Cells["D15"].Value = StaircaseAir.QueryValue.ToString();
            setsheet.Cells["D16"].Value = StaircaseAir.Count_Floor.ToString();
            setsheet.Cells["D17"].Value = GetLoadRange(StaircaseAir.Load.ToString());
            setsheet.Cells["D18"].Value = GetStairLocation(StaircaseAir.Stair.ToString());
            setsheet.Cells["D19"].Value = GetStairSpaceState(StaircaseAir.Type_Area.ToString());
            int rowNo = 20;
            int rangerows = 5;
            for (int i = 1; i <= StaircaseAir.FrontRoomDoors2.Sum(f => f.Value.Count); i++)
            {
                setsheet.CopyRangeToNext(20, 1, 24, 4, rangerows * i);
            }
            foreach (var floor in StaircaseAir.FrontRoomDoors2)
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
