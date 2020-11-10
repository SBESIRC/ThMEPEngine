using System.Linq;
using OfficeOpenXml;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class RefugeFontRoomExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            RefugeFontRoomModel refugeFontRoomModel = fanmodel as RefugeFontRoomModel;
            setsheet.Cells["D2"].Value = fandatamodel.FanNum;
            setsheet.Cells["D3"].Value = fandatamodel.Name;
            setsheet.Cells["D4"].Value = refugeFontRoomModel.DoorOpeningVolume.ToString();
            setsheet.Cells["D5"].Value = refugeFontRoomModel.OverAk.ToString();
            setsheet.Cells["D6"].Value = "1";
            int rowNo = 7;
            for (int i = 1; i <= refugeFontRoomModel.FrontRoomDoors2.Sum(f => f.Value.Count); i++)
            {
                setsheet.CopyRangeToNext(7, 1, 9, 4, 3 * i);
            }
            foreach (var floor in refugeFontRoomModel.FrontRoomDoors2)
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
            copyoperator.CopyRangeToOtherSheet(setsheet, 1,1, rowNo - 1, 4, targetsheet);
        }
    }
}
