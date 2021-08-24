using System;
using System.Linq;
using ThMEPHVAC.Duct;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.CAD
{
    public class ThHolesAndValvesEngine
    {
        public List<ThValveGroup> InletValveGroups { get; set; }
        public List<ThValveGroup> OutletValveGroups { get; set; }
        private Matrix3d dis_mat;
        public ThHolesAndValvesEngine(ThDbModelFan fanmodel,
                                      DBObjectCollection wallobjects,
                                      DBObjectCollection bypassobjects,
                                      Duct_InParam param,
                                      HashSet<Line> in_lines,
                                      HashSet<Line> out_lines)
        {
            double teewidth = ThMEPHVACService.Get_width(param.bypass_size);
            double inletductwidth = ThMEPHVACService.Get_width(param.in_duct_size);
            double outletductwidth = ThMEPHVACService.Get_width(param.out_duct_size);
            dis_mat = Matrix3d.Displacement(fanmodel.FanInletBasePoint.GetAsVector());
            InletValveGroups = GetValveGroup(fanmodel, wallobjects, bypassobjects, inletductwidth, teewidth, ValveGroupPosionType.Inlet, in_lines);
            OutletValveGroups = GetValveGroup(fanmodel, wallobjects, bypassobjects, outletductwidth, teewidth, ValveGroupPosionType.Outlet, out_lines);

            if (OutletValveGroups.Any(g => g.ValvesInGroup.Any(v => v.ValveVisibility.Contains("止回阀"))))
            {
                foreach (var inletgroup in InletValveGroups)
                {
                    if (inletgroup.ValvesInGroup.Count > 0)
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
                                                 HashSet<Line> lines)
        {
            var valvegroups = new List<ThValveGroup>();
            var walllines = wallobjects.Cast<Line>();
            foreach (Line l in lines)
            {
                var dir_vec = ThMEPHVACService.Get_edge_direction(l);
                bool IsBypass = ThServiceTee.Is_bypass(l.StartPoint, l.EndPoint, bypassobjects);
                double width = IsBypass ? teewidth : ductwidth;
                foreach (var wallline in walllines)
                {
                    Point3dCollection IntersectPoints = new Point3dCollection();
                    l.IntersectWith(wallline, Intersect.OnBothOperands, IntersectPoints, new IntPtr(), new IntPtr());
                    if (IntersectPoints.Count > 0)
                    {
                        var vec = new Vector2d(dir_vec.X, dir_vec.Y);
                        var holeangle = vec.Angle >= 0.5 * Math.PI ? vec.Angle - 0.5 * Math.PI : vec.Angle + 1.5 * Math.PI;
                        var insert_p = IntersectPoints[0].TransformBy(dis_mat);
                        var groupparameters = new ThValveGroupParameters()
                        {
                            GroupInsertPoint = insert_p,
                            DuctWidth = width,
                            RotationAngle = holeangle,
                            FanScenario = fanmodel.scenario,
                            ValveGroupPosion = valvePosion,
                            ValveToFanSpacing = IntersectPoints[0].DistanceTo(l.StartPoint),
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
