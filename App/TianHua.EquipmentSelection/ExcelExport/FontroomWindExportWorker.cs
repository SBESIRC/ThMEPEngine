using System;
using System.Linq;
using OfficeOpenXml;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class FontroomWindExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            FontroomWindModel frontRoomWind = fanmodel as FontroomWindModel;
            setsheet.Cells["D2"].Value = fandatamodel.FanNum;
            setsheet.Cells["D3"].Value = fandatamodel.Name;
            setsheet.Cells["D4"].Value = Math.Max(frontRoomWind.QueryValue, (frontRoomWind.DoorOpeningVolume + frontRoomWind.LeakVolume)).ToString();
            setsheet.Cells["D5"].Value = (frontRoomWind.DoorOpeningVolume + frontRoomWind.LeakVolume).ToString();
            setsheet.Cells["D6"].Value = frontRoomWind.DoorOpeningVolume.ToString();
            setsheet.Cells["D8"].Value = frontRoomWind.LeakVolume.ToString();
            setsheet.Cells["D9"].Value = frontRoomWind.OverAk.ToString();
            setsheet.Cells["D10"].Value = "0.7";
            setsheet.Cells["D11"].Value = Math.Min(frontRoomWind.Count_Floor, 3).ToString();
            setsheet.Cells["D12"].Value = (frontRoomWind.Length_Valve * frontRoomWind.Width_Valve / 1000000).ToString();
            setsheet.Cells["D13"].Value = (frontRoomWind.Count_Floor < 3 ? 0 : frontRoomWind.Count_Floor - 3).ToString();
            setsheet.Cells["D14"].Value = frontRoomWind.QueryValue.ToString();
            setsheet.Cells["D15"].Value = frontRoomWind.Count_Floor.ToString();
            setsheet.Cells["D16"].Value = GetLoadRange(frontRoomWind.Load.ToString());
            setsheet.Cells["D17"].Value = frontRoomWind.Length_Valve.ToString();
            setsheet.Cells["D18"].Value = frontRoomWind.Width_Valve.ToString();
            int rowNo = 19;
            for (int i = 1; i <= frontRoomWind.FrontRoomDoors2.Sum(f => f.Value.Count); i++)
            {
                setsheet.CopyRangeToNext(19, 1, 21, 4, 3 * i);
            }
            foreach (var floor in frontRoomWind.FrontRoomDoors2)
            {
                for (int i = 0; i < floor.Value.Count; i++)
                {
                    setsheet.Cells["A" + rowNo].Value = floor.Key;
                    setsheet.Cells["B" + rowNo].Value = "前室疏散门" + (i + 1);
                    setsheet.Cells["D" + rowNo].Value = floor.Value[i].Height_Door_Q.ToString();
                    setsheet.Cells["D" + (rowNo + 1)].Value = floor.Value[i].Width_Door_Q.ToString();
                    setsheet.Cells["D" + (rowNo + 2)].Value = floor.Value[i].Count_Door_Q.ToString();
                    rowNo += 3;
                }
            }
            copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, rowNo - 1, 4, targetsheet);
        }
    }
}
