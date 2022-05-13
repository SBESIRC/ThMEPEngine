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
        public static class CaseChooser
        {
            public static void Init(ThHalfFireBranchPipe halfBranchPipe, int halfType)
            {
                switch (halfType)
                {
                    case 1:
                        BaseFireCase.Init(halfBranchPipe);
                        break;
                    case 2:
                        HalfLowerCase.Init(halfBranchPipe);
                        break;
                    case 3:
                        HalfMiddleCase.Init(halfBranchPipe);
                        break;
                    case 4:
                        HalfHigherCase.Init(halfBranchPipe);
                        break;
                    case 5:
                        NormalLowerCase.Init(halfBranchPipe);
                        break;
                    case 6:
                        NormalMiddleCase.Init(halfBranchPipe);
                        break;
                    case 7:
                        NormalHigherCase.Init(halfBranchPipe);
                        break;
                    case 8:
                        HalfDoubleCase.Init(halfBranchPipe);
                        break;
                    case 9:
                        HalfLowerMiddleCase.Init(halfBranchPipe);
                        break;
                    case 10:
                        HalfLowerHigherCase.Init(halfBranchPipe);
                        break;
                    case 11:
                        NormalLowerMiddleCase.Init(halfBranchPipe);
                        break;
                    case 12:
                        NormalLowerHigherCase.Init(halfBranchPipe);
                        break;
                    case 13:
                        NormalMiddleHigherCase.Init(halfBranchPipe);
                        break;
                    case 14:
                        HalfMiddleHigherCase.Init(halfBranchPipe);
                        break;
                }
            }
            public static void Init1Floor(ThHalfFireBranchPipe halfBranchPipe, int halfType, string firstFloorMeterLocation, bool firstFloor)
            {
                switch (halfType)
                {
                    case 1:
                        BaseFireCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 2:
                        HalfLowerCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 3:
                        HalfMiddleCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 4:
                        HalfHigherCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 5:
                        NormalLowerCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 6:
                        NormalMiddleCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 7:
                        NormalHigherCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 8:
                        HalfDoubleCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 9:
                        HalfLowerMiddleCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 10:
                        HalfLowerHigherCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 11:
                        NormalLowerMiddleCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 12:
                        NormalLowerHigherCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 13:
                        NormalMiddleHigherCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                    case 14:
                        HalfMiddleHigherCase.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                        break;
                }
            }
            public static void InitUpFloor(ThHalfFireBranchPipe halfBranchPipe, int halfType, string outRoofStairwell, bool upperFloor)
            {
                if (halfType > 6 && outRoofStairwell.Equals("0"))
                {
                    switch (halfType)
                    {
                        case 7:
                            NormalHigherCase.InitUpFloor(halfBranchPipe, upperFloor);
                            break;
                        case 8:
                            HalfDoubleCase.InitUpFloor(halfBranchPipe, upperFloor);
                            break;
                        case 9:
                            HalfLowerMiddleCase.InitUpFloor(halfBranchPipe, upperFloor);
                            break;
                        case 10:
                            HalfLowerHigherCase.InitUpFloor(halfBranchPipe, upperFloor);
                            break;
                        case 11:
                            NormalLowerMiddleCase.InitUpFloor(halfBranchPipe, upperFloor);
                            break;
                        case 12:
                            NormalLowerHigherCase.InitUpFloor(halfBranchPipe, upperFloor);
                            break;
                        case 13:
                            NormalMiddleHigherCase.InitUpFloor(halfBranchPipe, upperFloor);
                            break;
                        case 14:
                            HalfMiddleHigherCase.InitUpFloor(halfBranchPipe, upperFloor);
                            break;
                    }
                }
                else
                {
                    Init(halfBranchPipe, halfType);
                }
            }
        }
}