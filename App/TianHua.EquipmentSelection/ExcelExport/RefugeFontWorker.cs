using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class RefugeFontWorker : BaseExportWorker
    {
        public override void ExportToExcel(ThFanVolumeModel fanmodel, Worksheet setsheet, Worksheet targetsheet, FanDataModel fandatamodel, ExcelFile excelfile)
        {
            FireFrontModel fireFrontModel = fanmodel as FireFrontModel;
            setsheet.SetCellValue("D2", fandatamodel.FanNum);
            setsheet.SetCellValue("D3", fandatamodel.Name);
            setsheet.SetCellValue("D5", (fireFrontModel.DoorOpeningVolume + fireFrontModel.LeakVolume).ToString());
            setsheet.SetCellValue("D6", fireFrontModel.DoorOpeningVolume.ToString());
            setsheet.SetCellValue("D8", fireFrontModel.LeakVolume.ToString());
            setsheet.SetCellValue("D9", fireFrontModel.OverAk.ToString());
            setsheet.SetCellValue("D10", "1");
            setsheet.SetCellValue("D11", Math.Min(fireFrontModel.Count_Floor, 3).ToString());
            setsheet.SetCellValue("D12", (fireFrontModel.Length_Valve * fireFrontModel.Width_Valve).ToString());
            setsheet.SetCellValue("D13", (fireFrontModel.Count_Floor < 3 ? 0 : fireFrontModel.Count_Floor).ToString());
            setsheet.SetCellValue("D15", fireFrontModel.Count_Floor.ToString());
            setsheet.SetCellValue("D16", fireFrontModel.Load.ToString());
            setsheet.SetCellValue("D17", fireFrontModel.Length_Valve.ToString());
            setsheet.SetCellValue("D18", fireFrontModel.Width_Valve.ToString());
            int rowNo = 19;
            foreach (var FrontRoomDoor in fireFrontModel.FrontRoomDoors)
            {
                setsheet.SetCellValue("D" + rowNo, FrontRoomDoor.Height_Door_Q.ToString());
                setsheet.SetCellValue("D" + (rowNo + 1), FrontRoomDoor.Width_Door_Q.ToString());
                setsheet.SetCellValue("D" + (rowNo + 2), FrontRoomDoor.Count_Door_Q.ToString());
                rowNo += 3;
            }
            excelfile.CopyRangeToOtherSheet(setsheet, "A1:D27", targetsheet);
        }
    }
}
