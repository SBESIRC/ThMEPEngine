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
        public List<ThValveGroup> room_valves { get; set; }
        public List<ThValveGroup> not_room_valves { get; set; }
        private Matrix3d dis_mat;
        public ThHolesAndValvesEngine(ThDbModelFan fanmodel,
                                      DBObjectCollection wallobjects,
                                      DBObjectCollection bypassobjects,
                                      Duct_InParam param,
                                      HashSet<Line> room_lines,
                                      HashSet<Line> not_room_lines)
        {
            double teewidth = ThMEPHVACService.Get_width(param.bypass_size);
            double room_width = ThMEPHVACService.Get_width(param.room_duct_size);
            double not_room_width = ThMEPHVACService.Get_width(param.other_duct_size);
            var p = fanmodel.is_exhaust ? fanmodel.FanInletBasePoint : fanmodel.FanOutletBasePoint;
            dis_mat = Matrix3d.Displacement(p.GetAsVector());
            //非送风场景 room是入风口 (送，补) room是出风口
            if (fanmodel.is_exhaust)
            {
                room_valves = GetValveGroup(fanmodel, wallobjects, bypassobjects, room_width, teewidth, ValveGroupPosionType.Inlet, room_lines);
                not_room_valves = GetValveGroup(fanmodel, wallobjects, bypassobjects, not_room_width, teewidth, ValveGroupPosionType.Outlet, not_room_lines);
            }
            else
            {
                room_valves = GetValveGroup(fanmodel, wallobjects, bypassobjects, room_width, teewidth, ValveGroupPosionType.Outlet, room_lines);
                not_room_valves = GetValveGroup(fanmodel, wallobjects, bypassobjects, not_room_width, teewidth, ValveGroupPosionType.Inlet, not_room_lines);
            }
            //room_valves = GetValveGroup(fanmodel, wallobjects, bypassobjects, room_width, teewidth, ValveGroupPosionType.Inlet, room_lines);
            //not_room_valves = GetValveGroup(fanmodel, wallobjects, bypassobjects, not_room_width, teewidth, ValveGroupPosionType.Outlet, not_room_lines);
            //Exclude_redundant_check_valve(fanmodel.is_exhaust);
        }
        private void Exclude_redundant_check_valve(bool Is_io_reverse)
        {
            if (!Is_io_reverse)
            {
                if (room_valves.Any(g => g.ValvesInGroup.Any(v => v.ValveVisibility.Contains("止回阀"))))
                {
                    foreach (var group in not_room_valves)
                    {
                        if (group.ValvesInGroup.Count > 0)
                        {
                            group.ValvesInGroup.RemoveAll(v => v.ValveVisibility.Contains("止回阀"));
                        }
                    }
                }
            }
            else
            {
                if (not_room_valves.Any(g => g.ValvesInGroup.Any(v => v.ValveVisibility.Contains("止回阀"))))
                {
                    foreach (var group in room_valves)
                    {
                        if (group.ValvesInGroup.Count > 0)
                        {
                            group.ValvesInGroup.RemoveAll(v => v.ValveVisibility.Contains("止回阀"));
                        }
                    }
                }
            }
        }
        public void RunInletValvesInsertEngine()
        {
            foreach (var valvegroup in room_valves)
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
            foreach (var valvegroup in not_room_valves)
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
                        
                        var valvegroup = new ThValveGroup(groupparameters, fanmodel.Data.BlockLayer, fanmodel.is_exhaust);
                        valvegroups.Add(valvegroup);
                    }
                }
            }
            return valvegroups;
        }
    }
}
