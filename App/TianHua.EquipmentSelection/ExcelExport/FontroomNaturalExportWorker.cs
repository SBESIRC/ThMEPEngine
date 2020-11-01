using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class FontroomNaturalExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, Worksheet setsheet, Worksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            FontroomNaturalModel fontroomNaturalModel = fanmodel as FontroomNaturalModel;
            setsheet.SetCellValue("D2", fandatamodel.FanNum);
            setsheet.SetCellValue("D3", fandatamodel.Name);
            setsheet.SetCellValue("D4", Math.Max(fontroomNaturalModel.QueryValue, (fontroomNaturalModel.DoorOpeningVolume + fontroomNaturalModel.LeakVolume)).ToString());
            setsheet.SetCellValue("D5", (fontroomNaturalModel.DoorOpeningVolume + fontroomNaturalModel.LeakVolume).ToString());
            setsheet.SetCellValue("D6", fontroomNaturalModel.DoorOpeningVolume.ToString());
            setsheet.SetCellValue("D8", fontroomNaturalModel.LeakVolume.ToString());
            setsheet.SetCellValue("D9", fontroomNaturalModel.OverAk.ToString());

            double v = 0.6 * fontroomNaturalModel.OverAl / (fontroomNaturalModel.OverAk + 1);
            setsheet.SetCellValue("D10", Math.Round(v, 2).ToString());
            setsheet.SetCellValue("D11", Math.Min(fontroomNaturalModel.Count_Floor, 3).ToString());
            setsheet.SetCellValue("D12", (fontroomNaturalModel.Length_Valve * fontroomNaturalModel.Width_Valve/1000000).ToString());
            setsheet.SetCellValue("D13", (fontroomNaturalModel.Count_Floor < 3 ? 0 : fontroomNaturalModel.Count_Floor-3).ToString());
            setsheet.SetCellValue("D14", fontroomNaturalModel.OverAl.ToString());
            setsheet.SetCellValue("D15", fontroomNaturalModel.OverAk.ToString());
            setsheet.SetCellValue("D16", fontroomNaturalModel.QueryValue.ToString());
            setsheet.SetCellValue("D17", fontroomNaturalModel.Count_Floor.ToString());
            setsheet.SetCellValue("D18", GetLoadRange(fontroomNaturalModel.Load.ToString()));
            setsheet.SetCellValue("D19", fontroomNaturalModel.Length_Valve.ToString());
            setsheet.SetCellValue("D20", fontroomNaturalModel.Width_Valve.ToString());

            int rowNo = 21;
            for (int i = 1; i <= fontroomNaturalModel.FrontRoomDoors2.Sum(f=>f.Value.Count) + fontroomNaturalModel.StairCaseDoors2.Sum(f => f.Value.Count); i++)
            {
                setsheet.CopyRangeToNext("A21", "D23", 3 * i);
            }
            foreach (var floor in fontroomNaturalModel.FrontRoomDoors2)
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
            foreach (var floor in fontroomNaturalModel.StairCaseDoors2)
            {
                for (int i = 0; i < floor.Value.Count; i++)
                {
                    setsheet.SetCellValue("A" + rowNo, floor.Key);
                    setsheet.SetCellValue("B" + rowNo, "楼梯间疏散门" + (i + 1));
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
