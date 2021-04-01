using System;
using System.Linq;
using QuickGraph;
using ThMEPHVAC.Duct;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Service.Hvac;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.CAD
{
    public class ThHolesAndValvesEngine
    {
        public List<ThValveGroup> InletValveGroups { get; set; }
        public List<ThValveGroup> OutletValveGroups { get; set; }
        public ThHolesAndValvesEngine(ThDbModelFan fanmodel,
            DBObjectCollection wallobjects,
            DBObjectCollection bypassobjects,
            double inletductwidth,
            double outletductwidth,
            double teewidth,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> inletcenterlinegraph,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> outletcenterlinegraph)
        {
            InletValveGroups = GetValveGroup(fanmodel, wallobjects, bypassobjects, inletductwidth, teewidth, ValveGroupPosionType.Inlet, inletcenterlinegraph);
            OutletValveGroups = GetValveGroup(fanmodel, wallobjects, bypassobjects, outletductwidth, teewidth, ValveGroupPosionType.Outlet, outletcenterlinegraph);

            if (OutletValveGroups.Any(g=>g.ValvesInGroup.Any(v=>v.ValveVisibility.Contains("止回阀"))))
            {
                foreach (var inletgroup in InletValveGroups)
                {
                    if (inletgroup.ValvesInGroup.Count>0)
                    {
                        inletgroup.ValvesInGroup.RemoveAll(v => v.ValveVisibility.Contains("止回阀"));
                    }
                }
            }
        }

        public void RunInletValvesInsertEngine()
        {
            foreach (var valvegroup in InletValveGroups)
            {
                foreach (var model in valvegroup.ValvesInGroup)
                {
                    if (ThDuctUtils.IsHoleModel(model.ValveBlockName))
                    {
                        ThValvesAndHolesInsertEngine.InsertHole(model);
                    }
                    else
                    {
                        ThValvesAndHolesInsertEngine.InsertValve(model);
                    }
                    ThValvesAndHolesInsertEngine.EnableValveAndHoleLayer(model);
                }
            }
        }

        public void RunOutletValvesInsertEngine()
        {
            foreach (var valvegroup in OutletValveGroups)
            {
                foreach (var model in valvegroup.ValvesInGroup)
                {
                    if (ThDuctUtils.IsHoleModel(model.ValveBlockName))
                    {
                        ThValvesAndHolesInsertEngine.InsertHole(model);
                    }
                    else
                    {
                        ThValvesAndHolesInsertEngine.InsertValve(model);
                    }
                    ThValvesAndHolesInsertEngine.EnableValveAndHoleLayer(model);
                }
            }
        }

        private List<ThValveGroup> GetValveGroup(ThDbModelFan fanmodel,
            DBObjectCollection wallobjects,
            DBObjectCollection bypassobjects,
            double ductwidth,
            double teewidth,
            ValveGroupPosionType valvePosion,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> centerlinegraph)
        {
            List<ThValveGroup> valvegroups = new List<ThValveGroup>();

            var walllines = wallobjects.Cast<Line>();
            foreach (var centeredge in centerlinegraph.Edges)
            {
                var centerline = new Line(centeredge.Source.Position, centeredge.Target.Position);
                var centerlinevector = new Vector2d(centeredge.Target.Position.X - centeredge.Source.Position.X, centeredge.Target.Position.Y - centeredge.Source.Position.Y);
                bool IsBypass = ThServiceTee.is_bypass(centeredge.Source.Position, centeredge.Target.Position, bypassobjects);
                double width = IsBypass ? teewidth : ductwidth;
                foreach (var wallline in walllines)
                {
                    Point3dCollection IntersectPoints = new Point3dCollection();
                    centerline.IntersectWith(wallline, Intersect.OnBothOperands, IntersectPoints, new IntPtr(), new IntPtr());
                    if (IntersectPoints.Count > 0)
                    {
                        double holeangle = centerlinevector.Angle >= 0.5 * Math.PI ? centerlinevector.Angle - 0.5 * Math.PI : centerlinevector.Angle + 1.5 * Math.PI;
                        var groupparameters = new ThValveGroupParameters()
                        {
                            GroupInsertPoint = IntersectPoints[0],
                            DuctWidth = width,
                            RotationAngle = holeangle,
                            FanScenario = fanmodel.FanScenario,
                            ValveGroupPosion = valvePosion,
                            ValveToFanSpacing = IntersectPoints[0].DistanceTo(centerline.StartPoint) - centeredge.SourceShrink,
                        };
                        var valvegroup = new ThValveGroup(groupparameters, fanmodel.Data.BlockLayer);
                        valvegroups.Add(valvegroup);
                    }
                }
            }
            return valvegroups;
        }
    }
}
