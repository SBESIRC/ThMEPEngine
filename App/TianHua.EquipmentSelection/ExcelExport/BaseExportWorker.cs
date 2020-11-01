using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public abstract class BaseExportWorker
    {
        public static BaseExportWorker Create(IFanModel model)
        {
            if (model is ExhaustCalcModel exm)
            {
                switch (exm.PlumeSelection)
                {
                    case "轴对称型":
                        return new ExhaustAxisymmetricExportWorker();
                    case "阳台溢出型":
                        return new ExhaustBalconyExportWorker();
                    case "窗口型":
                        return new ExhaustWindowExportWorker();
                    default:
                        break;
                }
            }
            if (model is FireFrontModel fm)
            {
                foreach (var floor in fm.FrontRoomDoors2)
                {
                    floor.Value.RemoveAll(d => d.Count_Door_Q * d.Height_Door_Q * d.Width_Door_Q == 0);
                 }
                return new FireFrontExportWorker();
            }
            else if (model is FontroomNaturalModel fn)
            {
                foreach (var floor in fn.FrontRoomDoors2)
                {
                    floor.Value.RemoveAll(d => d.Count_Door_Q * d.Height_Door_Q * d.Width_Door_Q == 0);
                }
                foreach (var floor in fn.StairCaseDoors2)
                {
                    floor.Value.RemoveAll(d => d.Count_Door_Q * d.Height_Door_Q * d.Width_Door_Q == 0);
                }
                return new FontroomNaturalExportWorker();
            }
            else if (model is FontroomWindModel ft)
            {
                foreach (var floor in ft.FrontRoomDoors2)
                {
                    floor.Value.RemoveAll(d => d.Count_Door_Q * d.Height_Door_Q * d.Width_Door_Q == 0);
                }
                return new FontroomWindExportWorker();
            }
            else if (model is StaircaseNoAirModel sn)
            {
                foreach (var floor in sn.FrontRoomDoors2)
                {
                    floor.Value.RemoveAll(d => d.Count_Door_Q * d.Height_Door_Q * d.Width_Door_Q * d.Crack_Door_Q == 0);
                }
                return new StaircaseNoAirExportWorker();
            }
            else if (model is StaircaseAirModel sa)
            {
                foreach (var floor in sa.FrontRoomDoors2)
                {
                    floor.Value.RemoveAll(d => d.Count_Door_Q * d.Height_Door_Q * d.Width_Door_Q * d.Crack_Door_Q == 0);
                }
                return new StaircaseAirExportWorker();
            }
            else if (model is RefugeRoomAndCorridorModel)
            {
                return new RefugeCorridorExportWorker();
            }
            else if (model is RefugeFontRoomModel rf)
            {
                foreach (var floor in rf.FrontRoomDoors2)
                {
                    floor.Value.RemoveAll(d => d.Count_Door_Q * d.Height_Door_Q * d.Width_Door_Q == 0);
                }
                return new RefugeFontRoomExportWorker();
            }
            return null;
        }
        public string GetLoadRange(string load)
        {
            switch (load)
            {
                case "LoadHeightLow" :
                    return "h<=24m";
                case "LoadHeightMiddle" :
                    return "24m<h<=50m";
                case "LoadHeightHigh" :
                    return "50m<h<100m";
                default:
                    break;
            }
            return string.Empty;
        }
        public string GetStairLocation(string location)
        {
            switch (location)
            {
                case "OnGround":
                    return "地上";
                case "UnderGound":
                    return "地下";
                default:
                    break;
            }
            return string.Empty;
        }
        public string GetStairSpaceState(string state)
        {
            switch (state)
            {
                case "Residence":
                    return "住宅";
                case "Business":
                    return "商业";
                default:
                    break;
            }
            return string.Empty;
        }

        public abstract void ExportToExcel(IFanModel fanmodel, Worksheet setsheet, Worksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator excelfile);

        private void CleanZeroItem(Dictionary<string, List<ThEvacuationDoor>> floors)
        {
            foreach (var floor in floors)
            {
                floor.Value.RemoveAll(d => d.Count_Door_Q * d.Height_Door_Q * d.Width_Door_Q == 0);
            }
        }
    }
}
