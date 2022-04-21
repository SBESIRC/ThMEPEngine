using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.DrainageSystemDiagram.Model;
using ThMEPWSS.DrainageSystemDiagram.Service;

using ThMEPWSS.DrainageADPrivate.Data;
using ThMEPWSS.DrainageADPrivate.Service;
using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class ThLayoutAngleValveService
    {
        public static List<ThDrainageSDADBlkOutput> LayoutAngleValve(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, ThSaniterayTerminal> ptTerminal)
        {
            var ptTerminalDir = GetTerminalDir(ptTerminal);
            var angleValve = LayoutAngleValveTree(rootList, ptTerminalDir);
            return angleValve;
        }

        private static Dictionary<Point3d, Vector3d> GetTerminalDir(Dictionary<Point3d, ThSaniterayTerminal> ptTerminal)
        {
            var ptTerminalDir = new Dictionary<Point3d, Vector3d>();
            foreach (var ptTerm in ptTerminal)
            {
                var terminal = ptTerm.Value;
                var pt = ptTerm.Key;

                if (terminal.Dir != default(Vector3d))
                {
                    ptTerminalDir.Add(pt, terminal.Dir);
                    continue;
                }

                if (terminal.Boundary.IsCCW() == false)
                {
                    terminal.Boundary.ReverseCurve();
                }

                var ptDist = new List<KeyValuePair<Line, double>>();
                var ptProj = new Point3d(pt.X, pt.Y, 0);
                for (int i = 0; i < terminal.Boundary.NumberOfVertices - 1; i++)
                {
                    var line = new Line(terminal.Boundary.GetPoint3dAt(i), terminal.Boundary.GetPoint3dAt(i + 1));
                    var dist = line.GetDistToPoint(ptProj, false);
                    ptDist.Add(new KeyValuePair<Line, double>(line, dist));
                }

                ptDist = ptDist.OrderBy(x => x.Value).ToList();
                var minDist = ptDist.First().Value;
                ptDist = ptDist.Where(x => (minDist - 10) <= x.Value && x.Value <= (minDist + 10)).ToList();
                ptDist = ptDist.OrderBy(x => x.Key.Length).ToList();
                var dir = (ptDist.First().Key.EndPoint - ptDist.First().Key.StartPoint).GetNormal();
                dir = dir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis).GetNormal();
                terminal.Dir = dir;
                ptTerminalDir.Add(pt, dir);

            }
            return ptTerminalDir;
        }



        private static List<ThDrainageSDADBlkOutput> LayoutAngleValveTree(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, Vector3d> ptTerminalDir)
        {
            var angleValve = new List<ThDrainageSDADBlkOutput>();
            foreach (var root in rootList)
            {
                angleValve.AddRange(LayoutAngleValve(root, ptTerminalDir));
            }
            return angleValve;
        }

        private static List<ThDrainageSDADBlkOutput> LayoutAngleValve(ThDrainageTreeNode root, Dictionary<Point3d, Vector3d> ptTerminalDir)
        {
            var angleValve = new List<ThDrainageSDADBlkOutput>();
            var leafs = root.GetLeaf();

            foreach (var leaf in leafs)
            {
                if (leaf.Terminal == null)
                {
                    continue;
                }
                if (leaf.TerminalPair != null && leaf.IsCool == false)
                {
                    continue;
                }

                var thModel = new ThDrainageSDADBlkOutput(leaf.TransPt);

                ThDrainageADCommon.Terminal_end_name.TryGetValue((int)leaf.Terminal.Type, out var blk_name);
                if (blk_name == null || blk_name == "")
                {
                    blk_name = ThDrainageADCommon.BlkName_AngleValve_AD;
                }

                thModel.Name = blk_name;
                thModel.Dir = new Vector3d(1, 0, 0);
                thModel.Scale = ThDrainageADCommon.Blk_scale_end;

                var visiDir = GetVisibilityDir(leaf, thModel.Name, ptTerminalDir);
                thModel.Visibility.Add(ThDrainageADCommon.VisiName_valve, visiDir);

                angleValve.Add(thModel);

            }

            return angleValve;
        }

        private static string GetVisibilityDir(ThDrainageTreeNode node, string name, Dictionary<Point3d, Vector3d> ptTerminalDir)
        {
            var tol = new Tolerance(10, 10);

            var dir = new Vector3d(1, 0, 0);
            var endTerminalDir = ptTerminalDir.Where(x => x.Key.IsEqualTo(node.Pt, tol));
            if (endTerminalDir.Count() > 0)
            {
                dir = endTerminalDir.First().Value;
            }

            var visiDir = CalculateVisibilityDir(dir, name);
            return visiDir;
        }

        public static string CalculateVisibilityDir(Vector3d dir, string name)
        {
            var dirInx = 0;
            var angle = dir.GetAngleTo(Vector3d.XAxis, -Vector3d.ZAxis);

            if ((0 * Math.PI / 180 <= angle && angle < 44 * Math.PI / 180) ||
               (316 * Math.PI / 180 < angle && angle <= 360 * Math.PI / 180))
            {
                dirInx = 0;
            }
            else if (44 * Math.PI / 180 <= angle && angle <= 136 * Math.PI / 180)
            {
                dirInx = 1;
            }
            else if (136 * Math.PI / 180 < angle && angle < 224 * Math.PI / 180)
            {
                dirInx = 2;
            }
            else if (224 * Math.PI / 180 <= angle && angle <= 316 * Math.PI / 180)
            {
                dirInx = 3;
            }

            var visibility = ThDrainageADCommon.EndValve_dir_name[name][dirInx];

            return visibility;
        }

    }
}
