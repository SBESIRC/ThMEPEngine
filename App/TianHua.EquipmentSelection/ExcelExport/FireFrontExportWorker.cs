using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class FireFrontExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, Worksheet setsheet, Worksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            FireFrontModel fireFrontModel = fanmodel as FireFrontModel;
            setsheet.SetCellValue("D2", fandatamodel.FanNum);
            setsheet.SetCellValue("D3", fandatamodel.Name);
            setsheet.SetCellValue("D4", Math.Max(fireFrontModel.QueryValue, (fireFrontModel.DoorOpeningVolume + fireFrontModel.LeakVolume)).ToString());
            setsheet.SetCellValue("D5", (fireFrontModel.DoorOpeningVolume + fireFrontModel.LeakVolume).ToString());
            setsheet.SetCellValue("D6", fireFrontModel.DoorOpeningVolume.ToString());
            setsheet.SetCellValue("D8", fireFrontModel.LeakVolume.ToString());
            setsheet.SetCellValue("D9", fireFrontModel.OverAk.ToString());
            setsheet.SetCellValue("D10", "1");
            setsheet.SetCellValue("D11", Math.Min(fireFrontModel.Count_Floor, 3).ToString());
            setsheet.SetCellValue("D12", (fireFrontModel.Length_Valve * fireFrontModel.Width_Valve/1000000).ToString());
            setsheet.SetCellValue("D13", (fireFrontModel.Count_Floor < 3 ? 0 : fireFrontModel.Count_Floor-3).ToString());
            setsheet.SetCellValue("D14", fireFrontModel.QueryValue.ToString());
            setsheet.SetCellValue("D15", fireFrontModel.Count_Floor.ToString());
            setsheet.SetCellValue("D16", GetLoadRange(fireFrontModel.Load.ToString()));
            setsheet.SetCellValue("D17", fireFrontModel.Length_Valve.ToString());
            setsheet.SetCellValue("D18", fireFrontModel.Width_Valve.ToString());

            int formRowIndex = 19;
            for (int i = 1; i <= fireFrontModel.FrontRoomDoors2.Sum(f => f.Value.Count); i++)
            {
                setsheet.CopyRangeToNext("A19", "D21", 3 * i);
            }
            foreach (var floor in fireFrontModel.FrontRoomDoors2)
            {
                for (int i = 0; i < floor.Value.Count; i++)
                {
                    setsheet.SetCellValue("A" + formRowIndex, floor.Key);
                    setsheet.SetCellValue("B" + formRowIndex, "前室疏散门" + (i + 1));
                    setsheet.SetCellValue("D" + formRowIndex, floor.Value[i].Height_Door_Q.ToString());
                    setsheet.SetCellValue("D" + (formRowIndex + 1), floor.Value[i].Width_Door_Q.ToString());
                    setsheet.SetCellValue("D" + (formRowIndex + 2), floor.Value[i].Count_Door_Q.ToString());
                    formRowIndex += 3;
                }
            }

            copyoperator.CopyRangeToOtherSheet(setsheet, "A1:D"+ (formRowIndex - 1).ToString(), targetsheet);
        }
    }
}
