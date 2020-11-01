using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class StaircaseNoAirExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, Worksheet setsheet, Worksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            StaircaseNoAirModel StaircaseNoAir = fanmodel as StaircaseNoAirModel;
            setsheet.SetCellValue("D2", fandatamodel.FanNum);
            setsheet.SetCellValue("D3", fandatamodel.Name);
            setsheet.SetCellValue("D4", Math.Max(StaircaseNoAir.QueryValue, (StaircaseNoAir.DoorOpeningVolume + StaircaseNoAir.LeakVolume)).ToString());
            setsheet.SetCellValue("D5", (StaircaseNoAir.DoorOpeningVolume + StaircaseNoAir.LeakVolume).ToString());
            setsheet.SetCellValue("D6", StaircaseNoAir.DoorOpeningVolume.ToString());
            setsheet.SetCellValue("D7", StaircaseNoAir.LeakVolume.ToString());
            setsheet.SetCellValue("D9", StaircaseNoAir.OverAk.ToString());
            setsheet.SetCellValue("D10", "1");
            setsheet.SetCellValue("D11", StaircaseNoAir.StairN1.ToString());
            setsheet.SetCellValue("D12", StaircaseNoAir.LeakArea.ToString());
            setsheet.SetCellValue("D13", "12");
            setsheet.SetCellValue("D14", StaircaseNoAir.N2.ToString());
            setsheet.SetCellValue("D15", StaircaseNoAir.QueryValue.ToString());
            setsheet.SetCellValue("D16", StaircaseNoAir.Count_Floor.ToString());
            setsheet.SetCellValue("D17", GetLoadRange(StaircaseNoAir.Load.ToString()));
            setsheet.SetCellValue("D18", GetStairLocation(StaircaseNoAir.Stair.ToString()));
            setsheet.SetCellValue("D19", GetStairSpaceState(StaircaseNoAir.Type_Area.ToString()));
            int rowNo = 20;
            int rangerows = 5;
            for (int i = 1; i <= StaircaseNoAir.FrontRoomDoors2.Sum(f => f.Value.Count); i++)
            {
                setsheet.CopyRangeToNext("A20", "D24", rangerows * i);
            }
            foreach (var floor in StaircaseNoAir.FrontRoomDoors2)
            {
                for (int i = 0; i < floor.Value.Count; i++)
                {
                    setsheet.SetCellValue("A" + rowNo, floor.Key);
                    setsheet.SetCellValue("B" + rowNo, "前室疏散门" + (i + 1));
                    setsheet.SetCellValue("D" + rowNo, floor.Value[i].Height_Door_Q.ToString());
                    setsheet.SetCellValue("D" + (rowNo + 1), floor.Value[i].Width_Door_Q.ToString());
                    setsheet.SetCellValue("D" + (rowNo + 2), floor.Value[i].Count_Door_Q.ToString());
                    setsheet.SetCellValue("D" + (rowNo + 3), floor.Value[i].Crack_Door_Q.ToString());
                    setsheet.SetCellValue("D" + (rowNo + 4), floor.Value[i].Type.ToString());
                    rowNo += rangerows;
                }
            }
            copyoperator.CopyRangeToOtherSheet(setsheet, "A1:D" + (rowNo - 1).ToString(), targetsheet);
        }
    }
}
