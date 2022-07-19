using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.SmokeProofSystem.ExportExcelService.ExportWorkers;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService
{
    public abstract class BaseExportWorker
    {
        public static BaseExportWorker Create(BaseSmokeProofViewModel model)
        {
            if (model is FireElevatorFrontRoomViewModel fm)
            {
                foreach (var floor in fm.ListTabControl)
                {
                    foreach (var d in floor.FloorInfoItems)
                    {
                        if (d.DoorNum * d.DoorHeight * d.DoorWidth == 0)
                        {
                            floor.FloorInfoItems.Remove(d);
                        }
                    }
                }
                return new FireFrontExportWorker();
            }
            else if (model is SeparateOrSharedNaturalViewModel fn)
            {
                foreach (var floor in fn.FrontRoomTabControl)
                {
                    foreach (var d in floor.FloorInfoItems)
                    {
                        if (d.DoorNum * d.DoorHeight * d.DoorWidth == 0)
                        {
                            floor.FloorInfoItems.Remove(d);
                        }
                    }
                }
                foreach (var floor in fn.StairRoomTabControl)
                {
                    foreach (var d in floor.FloorInfoItems)
                    {
                        if (d.DoorNum * d.DoorHeight * d.DoorWidth == 0)
                        {
                            floor.FloorInfoItems.Remove(d);
                        }
                    }
                }
                return new SeparateOrSharedNaturalExportWorker();
            }
            else if (model is SeparateOrSharedWindViewModel ft)
            {
                foreach (var floor in ft.FrontRoomTabControl)
                {
                    foreach (var d in floor.FloorInfoItems)
                    {
                        if (d.DoorNum * d.DoorHeight * d.DoorWidth == 0)
                        {
                            floor.FloorInfoItems.Remove(d);
                        }
                    }
                }
                return new SeparateOrSharedWindExportWorker();
            }
            else if (model is StaircaseNoWindViewModel sn)
            {
                foreach (var floor in sn.FrontRoomTabControl)
                {
                    foreach (var d in floor.FloorInfoItems)
                    {
                        if (d.DoorNum * d.DoorHeight * d.DoorWidth * d.DoorSpace == 0)
                        {
                            floor.FloorInfoItems.Remove(d);
                        }
                    }
                }
                return new StaircaseNoWindExportWorker();
            }
            else if (model is StaircaseWindViewModel sa)
            {
                foreach (var floor in sa.ListTabControl)
                {
                    foreach (var d in floor.FloorInfoItems)
                    {
                        if (d.DoorNum * d.DoorHeight * d.DoorWidth * d.DoorSpace == 0)
                        {
                            floor.FloorInfoItems.Remove(d);
                        }
                    }
                }
                return new StaircaseWindExportWorker();
            }
            else if (model is EvacuationWalkViewModel)
            {
                return new EvacuationWalkExportWorker();
            }
            else if (model is EvacuationFrontViewModel rf)
            {
                foreach (var floor in rf.ListTabControl)
                {
                    foreach (var d in floor.FloorInfoItems)
                    {
                        if (d.DoorNum * d.DoorHeight * d.DoorWidth == 0)
                        {
                            floor.FloorInfoItems.Remove(d);
                        }
                    }
                }
                return new EvacuationFrontExportWorker();
            }
            return null;
        }
        public string GetLoadRange(string load)
        {
            switch (load)
            {
                case "LoadHeightLow":
                    return "h<=24m";
                case "LoadHeightMiddle":
                    return "24m<h<=50m";
                case "LoadHeightHigh":
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

        public abstract void ExportToExcel(BaseSmokeProofViewModel baseModel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, ExcelRangeCopyOperator excelfile);
    }
}
