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
            //填入transPt并压缩z值。
            TransPtNormalZ(rootList);

            //转换transPt
            TransTreeList(rootList);

        }

        private static void TransPtNormalZ(List<ThDrainageTreeNode> rootList)
        {
            //rootlist已经根据冷热排序
            rootList = rootList.OrderByDescending(x => x.IsCool).ToList();
            var rootListDict = rootList.ToDictionary(x => x, x => x.GetLeaf());

            //change cool first
            foreach (var root in rootList)
            {
                root.TransPt = root.Pt;
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
