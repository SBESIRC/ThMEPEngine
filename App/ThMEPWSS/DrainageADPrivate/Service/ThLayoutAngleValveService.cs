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
        public static List<ThDrainageBlkOutput> LayoutAngleValve(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, ThSaniterayTerminal> ptTerminal, Dictionary<Point3d, ThValve> ptAngleValve)
        {
            var angleValveOutput = new List<ThDrainageBlkOutput>();
            SetTerminalDir(rootList, ptTerminal, ptAngleValve);

            var allLeafs = rootList.SelectMany(x => x.GetLeaf()).ToList();
            allLeafs.AddRange(rootList);

            angleValveOutput.AddRange(LayoutAngleValve(allLeafs));


            return angleValveOutput;
        }

        private static List<ThDrainageBlkOutput> LayoutAngleValve(List<ThDrainageTreeNode> allLeafs)
        {
            var angleValve = new List<ThDrainageBlkOutput>();
            var didLeaf = new List<ThDrainageTreeNode>();

            foreach (var leaf in allLeafs)
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
                var insertPt = leaf.TransPt;

                if (blk_name == "淋浴器系统" || blk_name == "燃气热水器")
                {
                    //冷热一对的要看插入点在冷水还是热水
                    if (didLeaf.Contains(leaf) == true)
                    {
                        continue;
                    }
                    scale = scale * ThDrainageADCommon.TransEnlargeScale;
                    visiDir = CalculateVisibilityDir(leaf.Terminal.Dir, blk_name, out var dirIndx);
                    insertPt = AdjustInsertPt(leaf, dirIndx);
                    didLeaf.Add(leaf);
                    didLeaf.Add(leaf.TerminalPair);
                }
                else
                {
                    visiDir = CalculateVisibilityDir(leaf.Terminal.Dir, blk_name, out var dirIndx);
                    didLeaf.Add(leaf);
                }

                var thModel = new ThDrainageBlkOutput(insertPt);
                thModel.Name = blk_name;
                thModel.Dir = new Vector3d(1, 0, 0);
                thModel.Scale = scale;
                thModel.Layer = ThDrainageADCommon.Layer_EQPM;
                thModel.Visibility.Add(ThDrainageADCommon.VisiName_valve, visiDir);

                angleValve.Add(thModel);
            }

            return angleValve;
        }
        /// <summary>
        /// 调整冷热水一个阀的时候，是冷水点插入还是热水点插入
        /// </summary>
        /// <param name="leaf"></param>
        /// <param name="dirIndx"></param>
        /// <returns></returns>
        private static Point3d AdjustInsertPt(ThDrainageTreeNode leaf, int dirIndx)
        {
            var returnPt = leaf.TransPt;

            if (leaf.TerminalPair != null)
            {
                var endDir = new Vector3d(1, 0, 0);//管线理论方向
                if (dirIndx == 0)
                {
                    //向右
                    endDir = endDir.RotateBy(45 * Math.PI / 180, Vector3d.ZAxis);
                }
                if (dirIndx == 1)
                {
                    //向前
                    endDir = endDir;
                }
                if (dirIndx == 2)
                {
                    //向左
                    endDir = endDir.RotateBy(225 * Math.PI / 180, Vector3d.ZAxis);
                }
                if (dirIndx == 3)
                {
                    //向后
                    endDir = endDir;
                }


                var leafToPairDir = (leaf.TerminalPair.TransPt - leaf.TransPt).GetNormal();
                var angle = leafToPairDir.GetAngleTo(endDir);
                if (angle > (45 * Math.PI / 180))
                {
                    //冷热端点方向和图块端点方向相反需要调整
                    returnPt = leaf.TerminalPair.TransPt;
                }
            }


            return returnPt;
        }

        private static void SetTerminalDir(List<ThDrainageTreeNode> rootList, Dictionary<Point3d, ThSaniterayTerminal> ptTerminal, Dictionary<Point3d, ThValve> ptAngleValve)
        {
            var tol = new Tolerance(1, 1);
            for (int i = 0; i < ptTerminal.Count(); i++)
            {
                var item = ptTerminal.ElementAt(i);
                if (item.Value.Dir != default(Vector3d))
                {
                    //冷热水同时有的已经做过一次的跳过
                    continue;
                }

                ptAngleValve.TryGetValue(item.Key, out var endValve);
                if (endValve != null)
                {
                    //有角阀的根据角阀
                    item.Value.Dir = endValve.Dir;
                }
                else
                {
                    //没有角阀的根据管线
                    var node = rootList.SelectMany(x => x.GetLeaf()).Where(x => item.Value != null && x.Terminal == item.Value).FirstOrDefault();

                    if (node != null)
                    {
                        while (node.Parent != null && Math.Abs(node.Pt.Z - node.Parent.Pt.Z) > 1)
                        {
                            //立管
                            node = node.Parent;
                        }
                        var dir = node.Parent.Pt - node.Pt;
                        if (Math.Abs(dir.Z) < 0.1)
                        {
                            dir = (new Vector3d(dir.X, dir.Y, 0)).GetNormal();
                            item.Value.Dir = dir;
                        }
                    }
                }
            }
        }

        public static string CalculateVisibilityDir(Vector3d dir, string name, out int dirInx)
        {
            dirInx = 0;
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
