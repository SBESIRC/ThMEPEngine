using System;
using System.Linq;
using OfficeOpenXml;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class FireFrontExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            FireFrontModel fireFrontModel = fanmodel as FireFrontModel;
            setsheet.Cells["D2"].Value = fandatamodel.FanNum;
            setsheet.Cells["D3"].Value = fandatamodel.Name;
            setsheet.Cells["D4"].Value = Math.Max(fireFrontModel.QueryValue, (fireFrontModel.DoorOpeningVolume + fireFrontModel.LeakVolume)).ToString();
            setsheet.Cells["D5"].Value = (fireFrontModel.DoorOpeningVolume + fireFrontModel.LeakVolume).ToString();
            setsheet.Cells["D6"].Value = fireFrontModel.DoorOpeningVolume.ToString();
            setsheet.Cells["D8"].Value = fireFrontModel.LeakVolume.ToString();
            setsheet.Cells["D9"].Value = fireFrontModel.OverAk.ToString();
            setsheet.Cells["D10"].Value = "1";
            setsheet.Cells["D11"].Value = Math.Min(fireFrontModel.Count_Floor, 3).ToString();
            setsheet.Cells["D12"].Value = (fireFrontModel.Length_Valve * fireFrontModel.Width_Valve / 1000000).ToString();
            setsheet.Cells["D13"].Value = (fireFrontModel.Count_Floor < 3 ? 0 : fireFrontModel.Count_Floor - 3).ToString();
            setsheet.Cells["D14"].Value = fireFrontModel.QueryValue.ToString();
            setsheet.Cells["D15"].Value = fireFrontModel.Count_Floor.ToString();
            setsheet.Cells["D16"].Value = GetLoadRange(fireFrontModel.Load.ToString());
            setsheet.Cells["D17"].Value = fireFrontModel.Length_Valve.ToString();
            setsheet.Cells["D18"].Value = fireFrontModel.Width_Valve.ToString();

            int formRowIndex = 19;
            for (int i = 1; i <= fireFrontModel.FrontRoomDoors2.Sum(f => f.Value.Count); i++)
            {
                setsheet.CopyRangeToNext(19, 1, 21, 4, 3 * i);
            }
            foreach (var floor in fireFrontModel.FrontRoomDoors2)
            {
                for (int i = 0; i < floor.Value.Count; i++)
                {
                    setsheet.Cells["A" + formRowIndex].Value = floor.Key;
                    setsheet.Cells["B" + formRowIndex].Value = "前室疏散门" + (i + 1);
                    setsheet.Cells["D" + formRowIndex].Value = floor.Value[i].Height_Door_Q.ToString();
                    setsheet.Cells["D" + (formRowIndex + 1)].Value = floor.Value[i].Width_Door_Q.ToString();
                    setsheet.Cells["D" + (formRowIndex + 2)].Value = floor.Value[i].Count_Door_Q.ToString();
                    formRowIndex += 3;
                }
            }
            copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, formRowIndex - 1, 4, targetsheet);
        }
    }
}
