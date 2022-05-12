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
     public static class ThFireService
        {
            public static int GetAreaIndex(ThFireDiagramInfo uiConfigs)
            {
                foreach (var list in uiConfigs.FloorNumList)
                {
                    foreach (var num in list)
                    {
                        FireCaseManager.Init(new ThHalfFireBranchPipe(new ThFireHalfFloor()), num);
                    }
                }
                return uiConfigs.StartNum;
            }
            public static bool GetFloorExist(List<int> highestStorey, List<int> lowestStorey, List<string> pipeNumber,
                out List<int> floorExist, out List<int> pipeFloorList)
            {
                floorExist = new List<int>();
                pipeFloorList = new List<int>();
                for (int i = 0; i < pipeNumber.Count; i++)
                {
                    for (int j = lowestStorey[i]; j <= highestStorey[i]; j++)
                    {
                        pipeFloorList.Add(j);
                        if (j == 0)
                        {
                            break;
                        }
                        if (floorExist.Contains(j))
                        {
                            MessageBox.Show("水管楼层重复");
                            return false;
                        }
                        else
                        {
                            floorExist.Add(j);
                        }
                    }
                }
                return true;
            }
            public static List<int> GetNotExistFloor(int floorNumbers, List<List<int>> floorNumList)
            {
                var notExistFloor = new List<int>();
                for (int i = 0; i < floorNumbers; i++)
                {
                    var hasNum = false;
                    foreach (var f in floorNumList)
                    {
                        if (f.Contains(i + 1))
                        {
                            hasNum = true;
                            break;
                        }
                    }
                    if (!hasNum)
                    {
                        notExistFloor.Add(i + 1);
                    }
                }
                return notExistFloor;
            }
            public static double[] CreatePipeOffsetX(int floorNumbers, List<int> lowestStorey, Point3d insertPt)
            {
                double PipeOffset_X = 10000;
                double PipeGap = -600;
                double[] PipeOffsetX = new double[floorNumbers];
                for (int i = 0; i < PipeOffsetX.Length; i++)
                {
                    PipeOffsetX[i] = PipeOffset_X + insertPt.X;
                    for (int j = 1; j < lowestStorey.Count; j++)
                    {
                        if (i + 1 >= lowestStorey[j])
                        {
                            PipeOffsetX[i] += PipeGap;
                        }
                    }
                }
                return PipeOffsetX;
            }
            public static List<int> GetFlushFaucet(ThFirePipeInfo setViewModel, out bool rstFlush)
            {
                rstFlush = true;
                var FlushFaucet = ThFireComputeService.ExtractData(setViewModel.FaucetFloor, "消火栓");
                if (setViewModel.FaucetFloor != "")
                {
                    var dataName = "消火栓";
                    FlushFaucet = ThFireComputeService.ExtractData(setViewModel.FaucetFloor, dataName);
                    if (FlushFaucet.Count == 0)
                    {
                        rstFlush = false;
                        return new List<int>();
                    }
                }
                return FlushFaucet;
            }
            public static List<int> GetNoPRValve(ThFirePipeInfo setViewModel, out bool rstNoPRValve)
            {
                rstNoPRValve = true;
                var NoPRValve = ThFireComputeService.ExtractData(setViewModel.NoCheckValve, "无减压阀");
                if (setViewModel.NoCheckValve != "")
                {
                    var dataName = "无减压阀";
                    NoPRValve = ThFireComputeService.ExtractData(setViewModel.NoCheckValve, dataName);
                    if (NoPRValve.Count == 0)
                    {
                        rstNoPRValve = false;
                        return new List<int>();
                    }
                }
                return NoPRValve;
            }
            public static int GetFloorNumbers(List<List<int>> floorNumList)
            {
                var floorNumbers = 1;
                foreach (var fn in floorNumList)
                {
                    if (fn.Max() > floorNumbers)
                    {
                        floorNumbers = fn.Max();
                    }
                }
                return floorNumbers;
            }
            public static bool GetLowHighStorey(ThFirePipeInfo setViewModel, int floorNumbers, out List<string> pipeNumber,
                out List<int> lowestStorey, out List<int> highestStorey)
            {
                pipeNumber = new List<string>();
                lowestStorey = new List<int>();
                highestStorey = new List<int>();
                for (int i = 0; i < setViewModel.PartitionDatas.Count; i++)
                {
                    if (Convert.ToInt32(setViewModel.PartitionDatas[i].MinimumFloorNumber) <= 0)
                    {
                        continue;
                    }
                    pipeNumber.Add(setViewModel.PartitionDatas[i].RiserNumber);
                    lowestStorey.Add(Convert.ToInt32(setViewModel.PartitionDatas[i].MinimumFloorNumber));
                    highestStorey.Add(Convert.ToInt32(setViewModel.PartitionDatas[i].HighestFloorNumber));
                    if (lowestStorey[i] > highestStorey[i])
                    {
                        MessageBox.Show("当前行最底层的值大于最高层");
                        return false;
                    }
                    if (lowestStorey[i] > floorNumbers || highestStorey[i] > floorNumbers)
                    {
                        MessageBox.Show("水管楼层超过最高楼层");
                        return false;
                    }
                }
                return true;
            }
            public static int GetMaxHouseholdNums(ThFireSysInfo sysIn, List<List<ThFireSystemService>> floorCleanToolList)
            {
                var maxHouseholdNums = 0;
                var areaIndex = sysIn.AreaIndex;
                for (int i = 0; i < sysIn.FloorNumbers; i++)
                {
                    if (floorCleanToolList[i][areaIndex].GetHouseholdNums() > maxHouseholdNums)
                    {
                        maxHouseholdNums = floorCleanToolList[i][areaIndex].GetHouseholdNums();
                    }
                }
                return maxHouseholdNums;
            }
            public static int GetHouseholdNum(int i, ThFireSysInfo sysIn, ThFireSysProcess sysProcess)
            {
                var floorCleanToolList = sysProcess.FloorCleanToolList;
                var areaIndex = sysIn.AreaIndex;
                var HouseholdNum = floorCleanToolList[i][areaIndex].GetHouseholdNums();
                if (HouseholdNum == 0)
                {
                    HouseholdNum = sysProcess.MaxHouseholdNums;
                }
                return HouseholdNum;
            }
            public static string GetDN(double U0i, double Ngi, int HouseholdNum)
            {
                var pipeCompute = new ThPipeComputeService(U0i, Ngi * HouseholdNum);
                var DN = "";
                if (Math.Abs(Ngi) > 1e-6)
                {
                    DN = pipeCompute.PipeDiameterCompute();
                }
                return DN;
            }
        }
}