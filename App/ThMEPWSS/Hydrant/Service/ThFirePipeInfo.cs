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
        public class ThFirePipeInfo
        {
            public ThFirePipeInfo()
            {
                FloorLineSpace = 1800;
                FaucetFloor = "1";
                NoCheckValve = "";
                MaxDayQuota = 250;
                MaxDayHourCoefficient = 2.5;
                NumberOfHouseholds = 3.5;
                MeterType = WaterMeterLocation.HalfFloor;
            }
            public CommandTypeEnum CommandType { get; set; }
            public WaterMeterLocation MeterType { get; set; }
            public bool IsHalfFloor => MeterType == WaterMeterLocation.HalfFloor;
            public double FloorLineSpace { get; set; }
            public string FaucetFloor { get; set; }
            public string NoCheckValve { get; set; }
            public double MaxDayQuota { get; set; }
            public double MaxDayHourCoefficient { get; set; }
            private double numberOfHouseholds { get; set; }
            public double NumberOfHouseholds { get; set; }
            public List<PartitionData> PartitionDatas
            { get; set; }
            public PartitionData SelectPartition { get; set; }
            public List<DynamicRadioButton> PRValveStyleDynamicRadios
            { get; set; }
            public List<DynamicRadioButton> LayingDynamicRadios
            { get; set; }
            public List<DynamicRadioButton> CleanToolDynamicRadios
            { get; set; }
            private void DeletePartitionRowToData()
            {
                if (null == SelectPartition)
                    return;
                PartitionDatas.Remove(SelectPartition);
                SelectPartition = PartitionDatas.FirstOrDefault();
            }
            public HalfPlatformSetVM halfViewModel { get; set; } = new HalfPlatformSetVM();
        }
}