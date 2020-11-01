using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class RefugeFontRoomExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, Worksheet setsheet, Worksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            RefugeFontRoomModel refugeFontRoomModel = fanmodel as RefugeFontRoomModel;
            setsheet.SetCellValue("D2", fandatamodel.FanNum);
            setsheet.SetCellValue("D3", fandatamodel.Name);
            setsheet.SetCellValue("D4", refugeFontRoomModel.DoorOpeningVolume.ToString());
            setsheet.SetCellValue("D5", refugeFontRoomModel.OverAk.ToString());
            setsheet.SetCellValue("D6", "1");
            int rowNo = 7;
            for (int i = 1; i <= refugeFontRoomModel.FrontRoomDoors2.Sum(f => f.Value.Count); i++)
            {
                setsheet.CopyRangeToNext("A7", "D9", 3 * i);
            }
            foreach (var floor in refugeFontRoomModel.FrontRoomDoors2)
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
