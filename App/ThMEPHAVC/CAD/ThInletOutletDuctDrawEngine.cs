using System;
using Linq2Acad;
using DotNetARX;
using QuickGraph;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.Duct;
using TianHua.Publics.BaseCode;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.CAD
{
    public class FanOpeningInfo
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double NormalAngle { get; set; }
        public Point3d OpingBasePoint { get; set; }

    }
    public struct Duct_InParam
    {
        public string bypass_size;
        public string bypass_pattern;
        public string room_duct_size;
        public string other_duct_size;
        public string scale;
        public string room_elevation;
        public string other_elevation;
        public bool is_io_reverse;
    }
    public class ThInletOutletDuctDrawEngine
    {
        public FanOpeningInfo InletOpening { get; set; }
        public FanOpeningInfo OutletOpening { get; set; }
        public string FanInOutType { get; set; }
        public List<ThIfcDistributionElement> InletDuctHoses { get; set; }
        public List<ThIfcDistributionElement> OutletDuctHoses { get; set; }
        public ThInletOutletDuctDrawEngine(ThDbModelFan fan)
        {
            Init(fan);
            SetInOutHoses(fan.scenario);
            string modelLayer = fan.Data.BlockLayer;
            DrawHoseInDWG(InletDuctHoses, modelLayer); 
            DrawHoseInDWG(OutletDuctHoses, modelLayer);
        }
        private void Init(ThDbModelFan fanmodel)
        {
            FanInOutType = fanmodel.IntakeForm;
            InletOpening = new FanOpeningInfo()
            {
                Width = fanmodel.FanInlet.Width,
                Height = fanmodel.FanInlet.Height,
                NormalAngle = fanmodel.FanInlet.Angle,
                OpingBasePoint = fanmodel.FanInletBasePoint
            };
            OutletOpening = new FanOpeningInfo()
            {
                Width = fanmodel.FanOutlet.Width,
                Height = fanmodel.FanOutlet.Height,
                NormalAngle = fanmodel.FanOutlet.Angle,
                OpingBasePoint = fanmodel.FanOutletBasePoint
            };
            InletDuctHoses = new List<ThIfcDistributionElement>();
            OutletDuctHoses = new List<ThIfcDistributionElement>();
        }
        private void SetInOutHoses(string scenario)
        {
            if (scenario == "消防补风" || scenario == "消防排烟" || scenario == "消防加压送风")
            {
                return;
            }
            else
            {
                if (!FanInOutType.Contains("上进") && !FanInOutType.Contains("下进"))
                {
                    ThIfcDuctHose inlethose = CreateHose(InletOpening.Width, InletOpening.NormalAngle, scenario);
                    inlethose.Matrix = Matrix3d.Displacement(inlethose.Parameters.InsertPoint.GetVectorTo(InletOpening.OpingBasePoint)) * Matrix3d.Rotation(inlethose.Parameters.RotateAngle, Vector3d.ZAxis, inlethose.Parameters.InsertPoint);
                    InletDuctHoses.Add(inlethose);
                }
                if (!FanInOutType.Contains("下出") && !FanInOutType.Contains("上出"))
                {
                    ThIfcDuctHose outlethose = CreateHose(OutletOpening.Width, OutletOpening.NormalAngle, scenario);
                    outlethose.Matrix = Matrix3d.Displacement(outlethose.Parameters.InsertPoint.GetVectorTo(OutletOpening.OpingBasePoint)) * Matrix3d.Rotation(outlethose.Parameters.RotateAngle, Vector3d.ZAxis, outlethose.Parameters.InsertPoint);
                    OutletDuctHoses.Add(outlethose);
                }
            }
        }
        private ThIfcDuctHose CreateHose(double width, double openingnormalangle,string scenario)
        {
            double openingnormalradian = openingnormalangle * Math.PI / 180;
            ThIfcDuctHoseParameters hoseparameters = new ThIfcDuctHoseParameters()
            {
                Width = width,
                Length = ThDuctUtils.GetHoseLength(scenario),
                RotateAngle = openingnormalradian < 0.5 * Math.PI ? 0.5 * Math.PI + openingnormalradian : openingnormalradian + 0.5 * Math.PI,
            };
            var hose = new ThIfcDuctHose(hoseparameters);
            hose.SetHoseInsertPoint();
            return hose;
        }

        private void DrawHoseInDWG(List<ThIfcDistributionElement> hoses, string modellayer)
        {
            foreach (ThIfcDuctHose hose in hoses)
            {
                ThValvesAndHolesInsertEngine.InsertHose(hose, modellayer);
                ThValvesAndHolesInsertEngine.EnableHoseLayer(hose, modellayer);
            }
        }
    }
}
