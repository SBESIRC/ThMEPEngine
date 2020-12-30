using ThMEPHVAC.Duct;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Service.Hvac;


namespace ThMEPHVAC.Entity
{
    public enum ValveGroupPosionType
    {
        Inlet = 1,
        Outlet = 2
    }

    public class ThValve
    {
        public Point3d ValvePosition { get; set; }
        public string ValveBlockName { get; set; }
        public string ValveBlockLayer { get; set; }
        public string ValveVisibility { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public string VisibilityPropertyName { get; set; }
        public string WidthPropertyName { get; set; }
        public string LengthPropertyName { get; set; }
        public double RotationAngle { get; set; }
        public double ValveOffsetFromCenter { get; set; }
    }

    public class ThValveGroup
    {
        public List<ThValve> ValvesInGroup { get; set; }
        public ThValveGroupParameters Parameters { get; set; }

        public ThValveGroup(ThValveGroupParameters parameters, string fanlayer)
        {
            Parameters = parameters;
            ValvesInGroup = CreateValvesFromValveGroup(fanlayer);
        }

        private List<ThValve> CreateValvesFromValveGroup(string fanlayer)
        {
            List<ThValve> valves = new List<ThValve>();
            //进风段
            if (Parameters.ValveGroupPosion == ValveGroupPosionType.Inlet)
            {
                var silencer = CreateSilencer(fanlayer);
                var hole = CreateHole();
                var firevalve = CreateFireValve(fanlayer);
                silencer.ValveOffsetFromCenter = -silencer.Length - hole.Length;
                hole.ValveOffsetFromCenter = -hole.Length;
                firevalve.ValveOffsetFromCenter = 0;
                switch (Parameters.FanScenario)
                {
                    case "消防排烟兼平时排风":
                        valves.AddRange(new List<ThValve> { silencer, hole, firevalve });
                        break;
                    case "消防补风兼平时送风":
                    case "消防排烟":
                    case "消防补风":
                    case "消防正压送风":
                        valves.AddRange(new List<ThValve> { hole, firevalve });
                        break;
                    default:
                        valves.AddRange(new List<ThValve> { hole, firevalve });
                        break;
                }
            }
            //出风段
            else
            {
                var silencer = CreateSilencer(fanlayer);
                var checkvalve = CreateCheckValve(fanlayer);
                var hole = CreateHole();
                var firevalve = CreateFireValve(fanlayer);

                switch (Parameters.FanScenario)
                {
                    case "消防排烟兼平时排风":
                        hole.ValveOffsetFromCenter = -hole.Length;
                        firevalve.ValveOffsetFromCenter = 0;
                        checkvalve.ValveOffsetFromCenter = firevalve.Length;
                        valves.AddRange(new List<ThValve> { checkvalve, firevalve, hole });
                        break;
                    case "消防补风兼平时送风":
                        silencer.ValveOffsetFromCenter = -silencer.Length - hole.Length;
                        hole.ValveOffsetFromCenter = -hole.Length;
                        firevalve.ValveOffsetFromCenter = 0;
                        checkvalve.ValveOffsetFromCenter = firevalve.Length;
                        valves.AddRange(new List<ThValve> { checkvalve, firevalve, hole, silencer });
                        break;
                    case "消防排烟":
                        hole.ValveOffsetFromCenter = -hole.Length;
                        firevalve.ValveOffsetFromCenter = 0;
                        checkvalve.ValveOffsetFromCenter = firevalve.Length;
                        valves.AddRange(new List<ThValve> { checkvalve, firevalve, hole });
                        break;
                    case "消防补风":
                        hole.ValveOffsetFromCenter = -hole.Length;
                        firevalve.ValveOffsetFromCenter = 0;
                        checkvalve.ValveOffsetFromCenter = firevalve.Length;
                        valves.AddRange(new List<ThValve> { checkvalve, firevalve, hole });
                        break;
                    case "消防正压送风":
                        firevalve.ValveOffsetFromCenter = -firevalve.Length - hole.Length;
                        hole.ValveOffsetFromCenter = -hole.Length;
                        checkvalve.ValveOffsetFromCenter = 0;
                        valves.AddRange(new List<ThValve> { checkvalve, hole, firevalve });
                        break;
                    default:
                        firevalve.ValveOffsetFromCenter = -firevalve.Length - hole.Length;
                        hole.ValveOffsetFromCenter = -hole.Length;
                        checkvalve.ValveOffsetFromCenter = 0;
                        valves.AddRange(new List<ThValve> { checkvalve, hole, firevalve });
                        break;
                }

            }
            return valves;
        }

        private ThValve CreateSilencer(string fanlayer)
        {
            return new ThValve()
            {
                Length = 1600,
                Width = Parameters.DuctWidth,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.SILENCER_BLOCK_NAME,
                ValveBlockLayer = ThDuctUtils.SilencerLayerName(fanlayer),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTH,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_LENGTH,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }

        private ThValve CreateHole()
        {
            return new ThValve()
            {
                Length = 200,
                Width = Parameters.DuctWidth + 100,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.WALLHOLE_BLOCK_NAME,
                ValveBlockLayer = ThHvacCommon.WALLHOLE_LAYER,
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_LENGTH,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }

        private ThValve CreateFireValve(string fanlayer)
        {
            return new ThValve()
            {
                Length = 320,
                Width = Parameters.DuctWidth,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThDuctUtils.FireValveBlockName(),
                ValveBlockLayer = ThDuctUtils.FireValveLayerName(fanlayer),
                ValveVisibility = ThDuctUtils.FireValveModelName(Parameters.FanScenario),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }

        private ThValve CreateCheckValve(string fanlayer)
        {
            return new ThValve()
            {
                Length = 200,
                Width = Parameters.DuctWidth,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = ThDuctUtils.AirValveLayerName(fanlayer),
                ValveVisibility = ThDuctUtils.CheckValveModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }

        private ThValve CreateElectricValve(string fanlayer)
        {
            return new ThValve()
            {
                Length = 200,
                Width = Parameters.DuctWidth,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = ThDuctUtils.AirValveLayerName(fanlayer),
                ValveVisibility = ThDuctUtils.ElectricValveModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }
    }

    public class ThValveGroupParameters
    {
        public double RotationAngle { get; set; }
        public string FanScenario { get; set; }
        public double DuctWidth { get; set; }
        public Point3d GroupInsertPoint { get; set; }
        public ValveGroupPosionType ValveGroupPosion { get; set; }
    }
}
