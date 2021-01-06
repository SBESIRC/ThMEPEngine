using ThMEPHVAC.Duct;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.IO;
using System.Linq;

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

        private List<ThValve> SetInnerValveGroup(string fanlayer)
        {
            List<ThValve> valves = new List<ThValve>();

            var hole = CreateHole();
            var firevalve = CreateFireValve(fanlayer);
            hole.ValveOffsetFromCenter = -hole.Length;
            firevalve.ValveOffsetFromCenter = 0;
            valves.AddRange(new List<ThValve> { firevalve, hole });

            return valves;
        }

        private List<ThValve> SetOuterValveGroup(string fanlayer)
        {
            List<ThValve> valves = new List<ThValve>();

            var silencer = CreateSilencer(fanlayer);
            var checkvalve = CreateCheckValve(fanlayer);
            var hole = CreateHole();
            var firevalve = CreateFireValve(fanlayer);

            //正常情况下，空间足够
            if (Parameters.ValveToFanSpacing > checkvalve.Length + firevalve.Length)
            {
                silencer.ValveOffsetFromCenter = -silencer.Length - hole.Length;
                hole.ValveOffsetFromCenter = -hole.Length;
                firevalve.ValveOffsetFromCenter = 0;
                checkvalve.ValveOffsetFromCenter = firevalve.Length;
            }
            //机房内空间放不下防火阀加止回阀
            else
            {
                //机房内空间连一个止回阀都放不下
                if (Parameters.ValveToFanSpacing < checkvalve.Length)
                {
                    silencer.ValveOffsetFromCenter = -silencer.Length - firevalve.Length - checkvalve.Length - hole.Length;
                    firevalve.ValveOffsetFromCenter = -firevalve.Length - checkvalve.Length - hole.Length;
                    checkvalve.ValveOffsetFromCenter = -checkvalve.Length - hole.Length;
                    hole.ValveOffsetFromCenter = -hole.Length;

                }
                //机房内空间可以放一个止回阀
                else
                {
                    silencer.ValveOffsetFromCenter = -silencer.Length - firevalve.Length - hole.Length;
                    firevalve.ValveOffsetFromCenter = -firevalve.Length - hole.Length;
                    hole.ValveOffsetFromCenter = -hole.Length;
                    checkvalve.ValveOffsetFromCenter = 0;
                }
            }

            switch (Parameters.FanScenario)
            {
                case "消防排烟":
                case "消防补风":
                case "消防加压送风":
                    valves.AddRange(new List<ThValve> { checkvalve, firevalve, hole });
                    break;
                default:
                    valves.AddRange(new List<ThValve> { checkvalve, firevalve, hole, silencer });
                    break;
            }

            return valves;
        }

        private List<ThValve> CreateValvesFromValveGroup(string fanlayer)
        {
            List<ThValve> valves = new List<ThValve>();

            var jsonReader = new ThDuctInOutMappingJsonReader();
            var innerRomDuctPosition = jsonReader.Mappings.First(d => d.WorkingScenario == Parameters.FanScenario).InnerRoomDuctType;
            
            //设置风机进风口段阀组
            if (Parameters.ValveGroupPosion == ValveGroupPosionType.Inlet)
            {
                //若当前工作场景中，风机进风口段对应机房内管段
                if (innerRomDuctPosition == "进风段")
                {
                    return SetInnerValveGroup(fanlayer);
                }
                //若当前工作场景中，风机出风口段对应机房内管段，即风机进风口段对应机房外管段
                else
                {
                    return SetOuterValveGroup(fanlayer);
                }
            }
            //出风段
            else
            {
                //若当前工作场景中，风机进风口段对应机房内管段
                if (innerRomDuctPosition == "进风段")
                {
                    return SetOuterValveGroup(fanlayer);
                }
                //若当前工作场景中，风机出风口段对应机房内管段，即风机进风口段对应机房外管段
                else
                {
                    return SetInnerValveGroup(fanlayer);
                }
            }
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
                ValveBlockLayer = ThDuctUtils.ValveLayerName(fanlayer),
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
                ValveBlockLayer = ThDuctUtils.ValveLayerName(fanlayer),
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
                ValveBlockLayer = ThDuctUtils.ValveLayerName(fanlayer),
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
        public double ValveToFanSpacing { get; set; }
        public Point3d GroupInsertPoint { get; set; }
        public ValveGroupPosionType ValveGroupPosion { get; set; }
    }
}
