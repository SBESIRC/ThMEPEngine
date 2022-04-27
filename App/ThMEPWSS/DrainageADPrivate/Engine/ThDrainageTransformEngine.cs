using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.DrainageADPrivate.Model;
using ThMEPWSS.DrainageADPrivate.Service;

namespace ThMEPWSS.DrainageADPrivate.Engine
{
    internal class ThDrainageTransformEngine
    {
        public static void TransformTopToAD(List<ThDrainageTreeNode> rootList)
        {
            //压缩z值。
            TransPtNormalZ(rootList);

            //转换transPt
            TransTreeList(rootList);

        }

        private static void TransPtNormalZOri(List<ThDrainageTreeNode> rootList)
        {
            //rootlist已经根据冷热排序
            rootList = rootList.OrderByDescending(x => x.IsCool).ToList();
            var rootListDict = rootList.ToDictionary(x => x, x => x.GetLeaf());

            //change cool first
            foreach (var root in rootList)
            {
                if (root.IsCool == false)
                {
                    var pairCool = rootListDict.Where(x => x.Value.Where(o => o.Terminal == root.Terminal).Any());
                    if (pairCool.Count() > 0)
                    {
                        var coolPair = pairCool.First().Value.Where(x => x.Terminal == root.Terminal).First();
                        root.TransPt = new Point3d(root.TransPt.X, root.TransPt.Y, coolPair.TransPt.Z);
                    }
                }
                ThTransformTopToADService.TransPtNormalZ(root);
            }
        }

        private static void TransPtNormalZ(List<ThDrainageTreeNode> rootList)
        {
            //rootlist已经根据冷热排序
            rootList = rootList.OrderByDescending(x => x.IsCool).ToList();
            var rootListDict = rootList.ToDictionary(x => x, x => x.GetDescendant());

            var allNode = new List<ThDrainageTreeNode>();
            allNode.AddRange(rootList);
            allNode.AddRange(rootListDict.SelectMany(x => x.Value));

            var minZ = allNode.Select(x => x.Pt.Z).OrderBy(x => x).First();
            minZ = Math.Round(minZ / 1000, MidpointRounding.AwayFromZero);

            //change cool first
            foreach (var root in rootList)
            {
                if (root.IsCool == true)
                {
                    var zBase = FindZBaseValue(root, minZ);
                    root.TransPt = new Point3d(root.TransPt.X, root.TransPt.Y, zBase);
                }
                if (root.IsCool == false)
                {
                    var pairCool = rootListDict.Where(x => x.Value.Where(o => o.Terminal == root.Terminal).Any());
                    if (pairCool.Count() > 0)
                    {
                        var coolPair = pairCool.First().Value.Where(x => x.Terminal == root.Terminal).First();
                        root.TransPt = new Point3d(root.TransPt.X, root.TransPt.Y, coolPair.TransPt.Z);
                    }
                    else
                    {
                        var zBase = FindZBaseValue(root, minZ);
                        root.TransPt = new Point3d(root.TransPt.X, root.TransPt.Y, zBase);
                    }
                }
                ThTransformTopToADService.TransPtNormalZ(root);
            }
        }

        private static double FindZBaseValue(ThDrainageTreeNode root, double minZ)
        {
            var rootZ = Math.Round(root.Pt.Z / 1000, MidpointRounding.AwayFromZero);
            var zBase = 0.0;
            if ((rootZ - minZ) > 1)
            {
                zBase = 1000;
            }

            return zBase;
        }


        private static void TransPtNormalZ2(List<ThDrainageTreeNode> rootList)
        {
            //rootlist根据冷热排序,子节点个数
            var rootListDict = rootList.ToDictionary(x => x, x => x.GetLeaf());
            rootList = rootList.OrderByDescending(x => x.IsCool).ThenByDescending(x => rootListDict[x].Count()).ToList();


            for (int i = 0; i < rootList.Count; i++)
            {
                var root = rootList[i];
                if (root.IsCool == true && i > 0)
                {
                    //找最近的校准
                    var nearestNode = rootListDict.Where(x => x.Key != root && x.Key.IsCool == true).SelectMany(x => x.Value).OrderBy(x => x.Pt.DistanceTo(root.Pt)).First();
                    root.TransPt = new Point3d(root.TransPt.X, root.TransPt.Y, nearestNode.TransPt.Z);
                }
                if (root.IsCool == false)
                {
                    //找到热水起点的对应冷水点位。热水不能直接校准，冷水最终点位已经变了
                    var pairCool = rootListDict.Where(x => x.Value.Where(o => o.Terminal == root.Terminal).Any());
                    if (pairCool.Count() > 0)
                    {
                        var coolPair = pairCool.First().Value.Where(x => x.Terminal == root.Terminal).First();
                        root.TransPt = new Point3d(root.TransPt.X, root.TransPt.Y, coolPair.TransPt.Z);
                    }
                    else
                    {
                        var nearestNode = rootListDict.Where(x => x.Key != root && x.Key.IsCool == false).SelectMany(x => x.Value).OrderBy(x => x.Pt.DistanceTo(root.Pt)).First();
                        root.TransPt = new Point3d(root.TransPt.X, root.TransPt.Y, nearestNode.TransPt.Z);
                    }
                }

                ThTransformTopToADService.TransPtNormalZ(root);

            }
        }

        private static void TransPtNormalZ3(List<ThDrainageTreeNode> rootList)
        {
            //rootlist根据冷热排序,子节点个数
            var allEnd = rootList.SelectMany(x => x.GetLeaf()).ToList();
            allEnd.AddRange(rootList);
            var rootListDict = rootList.ToDictionary(x => x, x => x.GetLeaf());
            rootList = rootList.OrderByDescending(x => x.IsCool).ThenByDescending(x => rootListDict[x].Count()).ToList();


            for (int i = 0; i < rootList.Count; i++)
            {
                var root = rootList[i];
                if (root.IsCool == true && i > 0)
                {
                    //找最近的校准
                    var nearestNode = rootListDict.Where(x => x.Key != root && x.Key.IsCool == true).SelectMany(x => x.Value).OrderBy(x => x.Pt.DistanceTo(root.Pt)).First();
                    root.TransPt = new Point3d(root.TransPt.X, root.TransPt.Y, nearestNode.TransPt.Z);
                }
                if (root.IsCool == false)
                {
                    //找到热水起点的对应冷水点位。热水不能直接校准，冷水最终点位已经变了
                    var pairCool = rootListDict.Where(x => x.Value.Where(o => o.Terminal == root.Terminal).Any());
                    if (pairCool.Count() > 0)
                    {
                        var coolPair = pairCool.First().Value.Where(x => x.Terminal == root.Terminal).First();
                        root.TransPt = new Point3d(root.TransPt.X, root.TransPt.Y, coolPair.TransPt.Z);
                    }
                    else
                    {
                        var nearestNode = rootListDict.Where(x => x.Key != root && x.Key.IsCool == false).SelectMany(x => x.Value).OrderBy(x => x.Pt.DistanceTo(root.Pt)).First();
                        root.TransPt = new Point3d(root.TransPt.X, root.TransPt.Y, nearestNode.TransPt.Z);
                    }
                }

                ThTransformTopToADService.TransPtNormalZ(root);
                ThDrainageADEngine.PrintZ(root, string.Format("l0TransZ{0}", i));
            }
        }

        private static void TransTreeList(List<ThDrainageTreeNode> rootList)
        {
            var transService = new ThTransformTopToADService();
            foreach (var root in rootList)
            {
                transService.TransformTree(root);
            }
        }

        public static void TransTreeListToSelect(List<ThDrainageTreeNode> rootList, Point3d printBasePt)
        {
            var moveDir = printBasePt - rootList.First().TransPt;
            var moveTrans = Matrix3d.Displacement(moveDir);

            var allNode = rootList.SelectMany(x => x.GetDescendant()).ToList();
            rootList.ForEach(x => allNode.Add(x));

            foreach (var node in allNode)
            {
                node.TransPt = node.TransPt.TransformBy(moveTrans);
            }
        }

    }
}
