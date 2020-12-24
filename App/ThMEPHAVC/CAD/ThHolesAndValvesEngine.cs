using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using QuickGraph;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Duct;
using TianHua.Publics.BaseCode;

namespace ThMEPHVAC.CAD
{
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

    public class ValveGroupParameters
    {
        public Point3d GroupInsertPoint { get; set; }
        public double RotationAngle { get; set; }
        public string FanScenario { get; set; }
        public double DuctWidth { get; set; }
        public ValveGroupPosionType ValveGroupPosion { get; set; }
    }

    public class ValveGroup
    {
        ValveGroupParameters Parameters { get; set; }
        public List<ThValve> ValvesInGroup { get; set; }

        public ValveGroup(ValveGroupParameters parameters, string fanlayer)
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
                var silencer = CreateSilencer();
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
                var silencer = CreateSilencer();
                var checkvalve = CreateCheckValve();
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
                        break;
                }

            }
            return valves;
        }

        private ThValve CreateSilencer()
        {
            return new ThValve()
            {
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.SILENCER_BLOCK_NAME,
                ValveBlockLayer = ThHvacCommon.SILENCER_LAYER,
                ValveVisibility = "",
                Width = Parameters.DuctWidth,
                Length = 1600,
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTH,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_LENGTH,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
                RotationAngle = Parameters.RotationAngle,
            };
        }

        private ThValve CreateHole()
        {
            return new ThValve()
            {
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.WALLHOLE_BLOCK_NAME,
                ValveBlockLayer = ThHvacCommon.WALLHOLE_LAYER,
                ValveVisibility = "",
                Width = Parameters.DuctWidth + 100,
                Length = 200,
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_LENGTH,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
                RotationAngle = Parameters.RotationAngle
            };
        }

        private ThValve CreateFireValve(string fanlayer)
        {
            string layer;
            switch (fanlayer)
            {
                case "H-DUAL-FBOX":
                    layer = "H-DAPP-DDAMP";
                    break;
                case "H-FIRE-FBOX":
                    layer = "H-DAPP-FDAMP";
                    break;
                case "H-EQUP-FBOX":
                    layer = "H-DAPP-EDAMP";
                    break;
                default:
                    layer = "0";
                    break;
            }

            string visibility;
            switch (Parameters.FanScenario)
            {
                case "消防排烟":
                case "消防排烟兼平时排风":
                    visibility = "280度排烟阀（带输出信号）FDHS";
                    break;
                case "厨房排油烟":
                    visibility = "150度防火阀";
                    break;
                default:
                    visibility = "70度排烟阀（带输出信号）FDS";
                    break;
            }
            return new ThValve()
            {
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.FILEVALVE_BLOCK_NAME,
                ValveBlockLayer = layer,
                ValveVisibility = visibility,
                Width = Parameters.DuctWidth,
                Length = 320,
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
                RotationAngle = Parameters.RotationAngle
            };
        }

        private ThValve CreateCheckValve()
        {
            return new ThValve()
            {
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = ThHvacCommon.AIRVALVE_LAYER,
                ValveVisibility = "风管止回阀",
                Width = Parameters.DuctWidth,
                Length = 200,
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
                RotationAngle = Parameters.RotationAngle
            };
        }

        private ThValve CreateElectricValve()
        {
            return new ThValve()
            {
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = ThHvacCommon.AIRVALVE_LAYER,
                ValveVisibility = "电动多叶调节阀",
                Width = Parameters.DuctWidth,
                Length = 200,
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
                RotationAngle = Parameters.RotationAngle
            };
        }

    }

    public enum ValveGroupPosionType
    {
        Inlet = 1,
        Outlet = 2
    }

    public class ThHolesAndValvesEngine
    {
        public List<ValveGroup> InletValveGroups { get; set; }
        public List<ValveGroup> OutletValveGroups { get; set; }
        public ThHolesAndValvesEngine(ThDbModelFan fanmodel,
            DBObjectCollection wallobjects,
            string inletductinfo,
            string outletductinfo,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> inletcenterlinegraph,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> outletcenterlinegraph)
        {
            double inletDuctWidth = inletductinfo.Split('x').First().NullToDouble();
            double outletDuctWidth = outletductinfo.Split('x').First().NullToDouble();

            InletValveGroups = GetValveGroup(fanmodel, wallobjects, inletDuctWidth, ValveGroupPosionType.Inlet, inletcenterlinegraph);
            foreach (var valvegroup in InletValveGroups)
            {
                foreach (var valve in valvegroup.ValvesInGroup)
                {
                    ThValvesAndHolesInsertEngine.InsertWallHole(valve, valve.LengthPropertyName, valve.WidthPropertyName);
                }
            }
            OutletValveGroups = GetValveGroup(fanmodel, wallobjects, outletDuctWidth, ValveGroupPosionType.Outlet, outletcenterlinegraph);
            foreach (var valvegroup in OutletValveGroups)
            {
                foreach (var valve in valvegroup.ValvesInGroup)
                {
                    ThValvesAndHolesInsertEngine.InsertWallHole(valve, valve.LengthPropertyName, valve.WidthPropertyName);
                }
            }

        }

        private List<ValveGroup> GetValveGroup(ThDbModelFan fanmodel,
            DBObjectCollection wallobjects,
            double ductwidth,
            ValveGroupPosionType valvePosion,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> centerlinegraph)
        {
            List<ValveGroup> valvegroups = new List<ValveGroup>();
            using (DBObjectCollection lineobjs = new DBObjectCollection())
            {
                centerlinegraph.Edges.ToList().ForEach(e => lineobjs.Add(new Line(e.Source.Position, e.Target.Position)));
                var walllines = wallobjects.Cast<Line>();
                foreach (var centerline in lineobjs.Cast<Line>())
                {
                    foreach (var wallline in walllines)
                    {
                        Point3dCollection IntersectPoints = new Point3dCollection();
                        centerline.IntersectWith(wallline, Intersect.OnBothOperands, IntersectPoints, new IntPtr(), new IntPtr());
                        if (IntersectPoints.Count > 0)
                        {
                            double holeangle = centerline.Angle >= 0.5 * Math.PI ? centerline.Angle - 0.5 * Math.PI : centerline.Angle + 1.5 * Math.PI;
                            var groupparameters = new ValveGroupParameters()
                            {
                                GroupInsertPoint = IntersectPoints[0],
                                DuctWidth = ductwidth,
                                RotationAngle = holeangle,
                                FanScenario = fanmodel.FanScenario,
                                ValveGroupPosion = valvePosion,
                            };
                            var valvegroup = new ValveGroup(groupparameters, fanmodel.Data.BlockLayer);
                            valvegroups.Add(valvegroup);
                        }
                    }
                }
            }
            return valvegroups;
        }
    }
}
