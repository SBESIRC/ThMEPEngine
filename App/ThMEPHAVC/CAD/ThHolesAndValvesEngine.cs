using System;
using System.Linq;
using QuickGraph;
using ThMEPHVAC.Duct;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Service.Hvac;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.Entity;

namespace ThMEPHVAC.CAD
{
    public class ThHolesAndValvesEngine
    {
        public List<ThValveGroup> InletValveGroups { get; set; }
        public List<ThValveGroup> OutletValveGroups { get; set; }
        public ThHolesAndValvesEngine(ThDbModelFan fanmodel,
            DBObjectCollection wallobjects,
            double inletductwidth,
            double outletductwidth,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> inletcenterlinegraph,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> outletcenterlinegraph)
        {
            InletValveGroups = GetValveGroup(fanmodel, wallobjects, inletductwidth, ValveGroupPosionType.Inlet, inletcenterlinegraph);
            OutletValveGroups = GetValveGroup(fanmodel, wallobjects, outletductwidth, ValveGroupPosionType.Outlet, outletcenterlinegraph);
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
            double ductwidth,
            ValveGroupPosionType valvePosion,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> centerlinegraph)
        {
            List<ThValveGroup> valvegroups = new List<ThValveGroup>();
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
                            var groupparameters = new ThValveGroupParameters()
                            {
                                GroupInsertPoint = IntersectPoints[0],
                                DuctWidth = ductwidth,
                                RotationAngle = holeangle,
                                FanScenario = fanmodel.FanScenario,
                                ValveGroupPosion = valvePosion,
                            };
                            var valvegroup = new ThValveGroup(groupparameters, fanmodel.Data.BlockLayer);
                            valvegroups.Add(valvegroup);
                        }
                    }
                }
            }
            return valvegroups;
        }
    }
}
