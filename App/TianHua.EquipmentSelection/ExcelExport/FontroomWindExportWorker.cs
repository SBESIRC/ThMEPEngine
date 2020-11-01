using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class FontroomWindExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, Worksheet setsheet, Worksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            FontroomWindModel frontRoomWind = fanmodel as FontroomWindModel;
            setsheet.SetCellValue("D2", fandatamodel.FanNum);
            setsheet.SetCellValue("D3", fandatamodel.Name);
            setsheet.SetCellValue("D4", Math.Max(frontRoomWind.QueryValue, (frontRoomWind.DoorOpeningVolume + frontRoomWind.LeakVolume)).ToString());
            setsheet.SetCellValue("D5", (frontRoomWind.DoorOpeningVolume + frontRoomWind.LeakVolume).ToString());
            setsheet.SetCellValue("D6", frontRoomWind.DoorOpeningVolume.ToString());
            setsheet.SetCellValue("D8", frontRoomWind.LeakVolume.ToString());
            setsheet.SetCellValue("D9", frontRoomWind.OverAk.ToString());
            setsheet.SetCellValue("D10", "0.7");
            setsheet.SetCellValue("D11", Math.Min(frontRoomWind.Count_Floor, 3).ToString());
            setsheet.SetCellValue("D12", (frontRoomWind.Length_Valve * frontRoomWind.Width_Valve/1000000).ToString());
            setsheet.SetCellValue("D13", (frontRoomWind.Count_Floor < 3 ? 0 : frontRoomWind.Count_Floor - 3).ToString());
            setsheet.SetCellValue("D14", frontRoomWind.QueryValue.ToString());
            setsheet.SetCellValue("D15", frontRoomWind.Count_Floor.ToString());
            setsheet.SetCellValue("D16", GetLoadRange(frontRoomWind.Load.ToString()));
            setsheet.SetCellValue("D17", frontRoomWind.Length_Valve.ToString());
            setsheet.SetCellValue("D18", frontRoomWind.Width_Valve.ToString());
            int rowNo = 19;
            for (int i = 1; i <= frontRoomWind.FrontRoomDoors2.Sum(f => f.Value.Count); i++)
            {
                setsheet.CopyRangeToNext("A19", "D21", 3 * i);
            }
            foreach (var floor in frontRoomWind.FrontRoomDoors2)
            {
                for (int i = 0; i < floor.Value.Count; i++)
                {
                    setsheet.SetCellValue("A" + rowNo, floor.Key);
                    setsheet.SetCellValue("B" + rowNo, "前室疏散门" + (i + 1));
                    setsheet.SetCellValue("D" + rowNo, floor.Value[i].Height_Door_Q.ToString());
                    setsheet.SetCellValue("D" + (rowNo + 1), floor.Value[i].Width_Door_Q.ToString());
                    setsheet.SetCellValue("D" + (rowNo + 2), floor.Value[i].Count_Door_Q.ToString());
                    rowNo += 3;
                }
            }
            copyoperator.CopyRangeToOtherSheet(setsheet, "A1:D" + (rowNo - 1).ToString(), targetsheet);
        }
    }
}
