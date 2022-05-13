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
       public class ThFireSysProcess
        {
            public List<int[]> Households { get; set; }
            public int MaxHouseholds { get; set; }
            public List<List<ThFireSystemService>> FloorCleanToolList { get; set; }
            public List<ThFireStoreyInfo> StoreyList { get; set; }
            public int MaxHouseholdNums { get; set; }
            public double[] PipeOffsetX { get; set; }
            public List<double> BranchPipeX { get; set; }
            public List<double[]> NGLIST { get; set; }
            public List<double[]> U0LIST { get; set; }
            public ThHalfFireBranchPipe HalfBranchPipe { get; set; }
            public ThFireSysProcess()
            {
                NGLIST = new List<double[]>();
                U0LIST = new List<double[]>();
                BranchPipeX = new List<double>();
            }
            public void SetInfo(ThFireSysInfo sysIn)
            {
                HalfBranchPipe.AreaIndex = 0;
                Households = ThFireComputeService.CountKitchenNums(sysIn);
                MaxHouseholds = ThFireComputeService.GetMaxHouseholds(Households, sysIn.FlushFaucet);
                FloorCleanToolList = ThFireComputeService.CountCleanToolNums(sysIn, Households);
                StoreyList = ThFireComputeService.CreateStoreysList(sysIn, Households);
                MaxHouseholdNums = ThFireService.GetMaxHouseholdNums(sysIn, FloorCleanToolList);
                PipeOffsetX = ThFireService.CreatePipeOffsetX(sysIn.FloorNumbers, sysIn.LowestStorey, sysIn.InsertPt);
                GetBranchPipeX();
            }
            private void GetBranchPipeX()
            {
                double lastPipeOffsetX = PipeOffsetX[0];
                for (int i = 0; i < PipeOffsetX.Length; i++)
                {
                    if (i == 0)
                    {
                        BranchPipeX.Add(400);
                    }
                    else
                    {
                        if (PipeOffsetX[i] != lastPipeOffsetX)
                        {
                            BranchPipeX.Add(BranchPipeX.Last() + 600);
                        }
                        else
                        {
                            BranchPipeX.Add(BranchPipeX.Last());
                        }
                    }
                    lastPipeOffsetX = PipeOffsetX[i];
                }
            }
        }
}