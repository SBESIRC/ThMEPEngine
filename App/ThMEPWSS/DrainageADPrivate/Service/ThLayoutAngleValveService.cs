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

using ThMEPWSS.DrainageADPrivate.Data;
using ThMEPWSS.DrainageADPrivate.Service;
using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class ThLayoutAngleValveService
    {
        public static List<ThDrainageBlkOutput> LayoutAngleValve(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, ThSaniterayTerminal> ptTerminal)
        {
            var angleValve = new List<ThDrainageBlkOutput>();
            foreach (var root in rootList)
            {
                angleValve.AddRange(LayoutAngleValve(root, ptTerminal));
            }

            return angleValve;
        }

        private static List<ThDrainageBlkOutput> LayoutAngleValve(ThDrainageTreeNode root, Dictionary<Point3d, ThSaniterayTerminal> ptTerminal)
        {
            var angleValve = new List<ThDrainageBlkOutput>();
            var leafs = root.GetLeaf();
            leafs.Add(root);

            foreach (var leaf in leafs)
            {
                if (leaf.Terminal == null)
                {
                    continue;
                }

                ThDrainageADCommon.Terminal_end_name.TryGetValue((int)leaf.Terminal.Type, out var blk_name);
                if (blk_name == null || blk_name == "")
                {
                    blk_name = ThDrainageADCommon.BlkName_AngleValve_AD;
                }
                var visiDir = ThDrainageADCommon.EndValve_dir_name[blk_name][0];
                var scale = ThDrainageADCommon.Blk_scale_end;
                if ((blk_name == "淋浴器系统" || blk_name == "燃气热水器") && leaf.IsCool == true)
                {
                    //跳过冷水
                    continue;
                }
                else
                {
                    visiDir = CalculateVisibilityDir(leaf.Terminal.Dir, blk_name);
                }
                if (blk_name == "淋浴器系统" || blk_name == "燃气热水器")
                {
                    scale = scale * ThDrainageADCommon.TransEnlargeScale;
                }

                var thModel = new ThDrainageBlkOutput(leaf.TransPt);
                thModel.Name = blk_name;
                thModel.Dir = new Vector3d(1, 0, 0);
                thModel.Scale = scale;
                thModel.Layer = ThDrainageADCommon.Layer_EQPM;
                thModel.Visibility.Add(ThDrainageADCommon.VisiName_valve, visiDir);

                angleValve.Add(thModel);

            }

            return angleValve;
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
