using System;
using System.Linq;
using OfficeOpenXml;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class FontroomNaturalExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel fanmodel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            FontroomNaturalModel fontroomNaturalModel = fanmodel as FontroomNaturalModel;
            setsheet.Cells["D2"].Value = fandatamodel.FanNum;
            setsheet.Cells["D3"].Value = fandatamodel.Name;
            setsheet.Cells["D4"].Value = Math.Max(fontroomNaturalModel.QueryValue, (fontroomNaturalModel.DoorOpeningVolume + fontroomNaturalModel.LeakVolume)).ToString();
            setsheet.Cells["D5"].Value = (fontroomNaturalModel.DoorOpeningVolume + fontroomNaturalModel.LeakVolume).ToString();
            setsheet.Cells["D6"].Value = fontroomNaturalModel.DoorOpeningVolume.ToString();
            setsheet.Cells["D8"].Value = fontroomNaturalModel.LeakVolume.ToString();
            setsheet.Cells["D9"].Value = fontroomNaturalModel.OverAk.ToString();

            double v = 0.6 * fontroomNaturalModel.OverAl / (fontroomNaturalModel.OverAk + 1);
            setsheet.Cells["D10"].Value = Math.Round(v, 2).ToString();
            setsheet.Cells["D11"].Value = Math.Min(fontroomNaturalModel.Count_Floor, 3).ToString();
            setsheet.Cells["D12"].Value = (fontroomNaturalModel.Length_Valve * fontroomNaturalModel.Width_Valve / 1000000).ToString();
            setsheet.Cells["D13"].Value = (fontroomNaturalModel.Count_Floor < 3 ? 0 : fontroomNaturalModel.Count_Floor - 3).ToString();
            setsheet.Cells["D14"].Value = fontroomNaturalModel.OverAl.ToString();
            setsheet.Cells["D15"].Value = fontroomNaturalModel.OverAk.ToString();
            setsheet.Cells["D16"].Value = fontroomNaturalModel.QueryValue.ToString();
            setsheet.Cells["D17"].Value = fontroomNaturalModel.Count_Floor.ToString();
            setsheet.Cells["D18"].Value = GetLoadRange(fontroomNaturalModel.Load.ToString());
            setsheet.Cells["D19"].Value = fontroomNaturalModel.Length_Valve.ToString();
            setsheet.Cells["D20"].Value = fontroomNaturalModel.Width_Valve.ToString();

            int rowNo = 21;
            for (int i = 1; i <= fontroomNaturalModel.FrontRoomDoors2.Sum(f => f.Value.Count) + fontroomNaturalModel.StairCaseDoors2.Sum(f => f.Value.Count); i++)
            {
                //setsheet.CopyRangeToNext("A21", "D23", 3 * i);
                setsheet.CopyRangeToNext(21,1,23,4,3*i);
            }
            foreach (var floor in fontroomNaturalModel.FrontRoomDoors2)
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
            foreach (var floor in fontroomNaturalModel.StairCaseDoors2)
            {
                for (int i = 0; i < floor.Value.Count; i++)
                {
                    setsheet.Cells["A" + rowNo].Value = floor.Key;
                    setsheet.Cells["B" + rowNo].Value = "楼梯间疏散门" + (i + 1);
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
