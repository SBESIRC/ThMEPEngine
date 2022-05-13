using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.Assistant;
using ThMEPWSS.ViewModel;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.ExtensionsNs;
using static ThMEPWSS.Assistant.DrawUtils;
using ThMEPEngineCore.Model.Common;
using NetTopologySuite.Operation.Buffer;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Exception = System.Exception;
using ThMEPWSS.Pipe.Engine;
using ThMEPEngineCore.Model;
using static ThMEPWSS.Hydrant.Service.Common;
namespace ThMEPWSS.Hydrant.Service
{
      public class ThFireHalfFloor
        {
            public string LayingMethod { get; set; }
            public string SupplyFloor { get; set; }
            public string HalfFloorLayMethod { get; set; }
            public string EntryPoint { get; set; }
            public string ThroughWaterWell { get; set; }
            public string FirstFloorMeterLocation { get; set; }
            public string OutRoofStairwell { get; set; }
            public string CaseCode { get; set; }
            public Dictionary<string, int> ValidCodeDic { get; set; }
            private string BoolToStr(bool flag)
            {
                if (flag) return "1";
                else return "0";
            }
            private void GetCaseCode(ThFirePipeInfo setVM)
            {
                var halfVM = setVM.halfViewModel;
                LayingMethod = BoolToStr(setVM.LayingDynamicRadios[0].IsChecked);
                SupplyFloor = BoolToStr(halfVM.SupplyFloorDynamicRadios[0].IsChecked);
                HalfFloorLayMethod = BoolToStr(halfVM.HalfLayingDynamicRadios[0].IsChecked);
                EntryPoint = BoolToStr(halfVM.EntryLocationDynamicRadios[0].IsChecked);
                ThroughWaterWell = BoolToStr(halfVM.PipeThroughWellDynamicRadios[0].IsChecked);
                FirstFloorMeterLocation = BoolToStr(halfVM.FirstFloorMeterLocationDynamicRadios[0].IsChecked);
                OutRoofStairwell = BoolToStr(halfVM.OutRoofStairwellDynamicRadios[0].IsChecked);
                CaseCode = LayingMethod + SupplyFloor + HalfFloorLayMethod + EntryPoint + ThroughWaterWell;
            }
            public bool IsValidCode()
            {
                return ValidCodeDic.ContainsKey(CaseCode);
            }
            public void Draw(ThFireSysInfo sysIn, ThFireSysProcess sysProcess, bool prValveStyle)
            {
                int halfType = ValidCodeDic[CaseCode];
                var BranchPipe = new List<ThHalfFireBranchPipe>();
                for (int i = 0; i < sysIn.FloorNumbers; i++)
                {
                    var HouseholdNum = ThFireService.GetHouseholdNum(i, sysIn, sysProcess);
                    double Ngi = ThFireComputeService.InnerProduct(sysProcess.FloorCleanToolList[i][sysIn.AreaIndex].GetCleaningTools(), sysIn.WaterEquivalent) / HouseholdNum;
                    static double ComputeU0i(double Ngi, ThFireSysInfo sysIn)
                    {
                        double U0i = 0;
                        if (Math.Abs(Ngi) > 1e-6)
                        {
                            U0i = 100 * sysIn.MaxDayQuota * sysIn.NumberOfHouseholds * sysIn.MaxDayHourCoefficient / (0.2 * Ngi * sysIn.T * 3600);
                        }
                        return U0i;
                    }
                    double U0i = ComputeU0i(Ngi, sysIn);
                    var DN = ThFireService.GetDN(U0i, Ngi, HouseholdNum);
                    var halfBranchPipe = new ThHalfFireBranchPipe(i, DN, sysIn, sysProcess, halfType, prValveStyle);
                    if (i <= 1)
                    {
                        var firstFloor = (i == 0);
                        CaseChooser.Init1Floor(halfBranchPipe, halfType, FirstFloorMeterLocation, firstFloor);
                    }
                    else if (i > sysIn.FloorNumbers - 3)
                    {
                        var upperFloor = (i == sysIn.FloorNumbers - 1);
                        CaseChooser.InitUpFloor(halfBranchPipe, halfType, OutRoofStairwell, upperFloor);
                    }
                    else
                    {
                        CaseChooser.Init(halfBranchPipe, halfType);
                    }
                    BranchPipe.Add(halfBranchPipe);
                }
                for (int i = 0; i < BranchPipe.Count; i++)
                {
                    if (sysIn.PipeFloorList.Contains(i + 1))
                    {
                        BranchPipe[i].DrawBranchPipe();
                    }
                }
            }
        }
}