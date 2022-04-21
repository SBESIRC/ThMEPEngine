using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.DrainageSystemDiagram.Model;
using ThMEPWSS.DrainageADPrivate.Data;
using ThMEPWSS.DrainageADPrivate.Service;
using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Engine
{
    internal class ThDrainageADEngine
    {
        public static void DrainageTransADEngine(ThDrainageADPDataPass dataPass)
        {
            //树
            var treeEngine = new ThDraingeTreeEngine(dataPass);
            treeEngine.BuildDraingeTree();

            if (treeEngine.MergedRootList.Count() == 0)
            {
                return;
            }

            /////////////
            // treeEngine.MergedRootList.SelectMany(x => x.GetLeaf()).ForEach(l =>
            //{
            //    if (l.Terminal != null)
            //    {
            //        DrawUtils.ShowGeometry(l.Pt, l.Terminal.Type.ToString(), "l0leafterminal", 3, hight: 50);
            //    }
            //    if (l.TerminalPair != null)
            //    {
            //        DrawUtils.ShowGeometry(new Line(l.Pt, l.TerminalPair.Pt), "l0leafterminalPair", 3);
            //    }
            //});
            //treeEngine.MergedRootList.ForEach(x => PrintTree(x, String.Format("l0tree{0}", treeEngine.MergedRootList.IndexOf(x))));
            ///////////

            //--管径计算--
            //rootList 没有setTerminalPair
            var dimEngine = new ThCalculateDimEngine(dataPass, treeEngine);
            dimEngine.CalculateDim();
            ThDrainageADTreeService.SetTerminalPairMultipleTree(dimEngine.RootList, treeEngine.TerminalPairDict);

            if (dimEngine.RootList.Count() == 0)
            {
                return;
            }
            //treeEngine.MergedRootList.ForEach(x => PrintDiam(x, String.Format("l0dimTree{0}", treeEngine.MergedRootList.IndexOf(x))));
            //dimEngine.RootList.ForEach(x => PrintDiam(x, String.Format("l0dimMaxTree{0}", dimEngine.RootList.IndexOf(x))));
            //------

            //--转换--
            ThDrainageTransformEngine.TransformTopToAD(dimEngine.RootList);
            ThDrainageTransformEngine.TransTreeListToSelect(dimEngine.RootList, dataPass.PrintBasePt);
            //dimEngine.RootList.ForEach(x => PrintZ(x, string.Format("l0Zvalue{0}", dimEngine.RootList.IndexOf(x))));
            dimEngine.RootList.ForEach(x => PrintAD(x, string.Format("l1TransAD{0}", dimEngine.RootList.IndexOf(x))));

            //--摆放管径--
            var dim = ThLayoutDimService.LayoutDim(dimEngine.RootList);
            dim.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l1Dim", 191, l: 100));
            dim.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Visibility[ThDrainageADCommon.VisiName_valve], "l1Dim", 191, hight: 180));

            //--摆放终端阀门--
            var endValve = ThLayoutAngleValveService.LayoutAngleValve(dimEngine.RootList, treeEngine.PtTerminal);
            endValve.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l1EndValve", 40, l: 100));
            endValve.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Visibility[ThDrainageADCommon.VisiName_valve], "l1EndValve", 40, hight: 180));


            //--摆放其他阀门--
            var valveOutput = ThLayoutValveService.LayoutValve(dataPass, dimEngine.RootList);
            valveOutput.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l1otherValve", 201, l: 100));
            valveOutput.ForEach(x =>
            {
                if (x.Visibility.Count() > 0)
                {
                    DrawUtils.ShowGeometry(x.Position, x.Visibility[ThDrainageADCommon.VisiName_valve], "l1otherValve", 201, hight: 180);
                }
            });


            //-摆放热水器





            //--断线--






        }


        private static void PrintTree(ThDrainageTreeNode root, string layer)
        {
            int cs = root.GetLeafCount();
            int dp = root.GetDepth();
            DrawUtils.ShowGeometry(new Point3d(root.Pt.X + 20, root.Pt.Y, 0), string.Format("{0}_{1}_{2}", dp, cs, root.IsCool), layer, (short)(dp % 7), 25, 50);

            root.Child.ForEach(x => PrintTree(x, layer));
        }

        private static void PrintDiam(ThDrainageTreeNode root, string layer)
        {
            DrawUtils.ShowGeometry(new Point3d(root.Pt.X + 20, root.Pt.Y, 0), string.Format("dim:{0}", root.Dim), layer, 3, 25, 50);

            root.Child.ForEach(x => PrintDiam(x, layer));
        }

        private static void PrintZ(ThDrainageTreeNode root, string layer)
        {
            DrawUtils.ShowGeometry(new Point3d(root.Pt.X + 20, root.Pt.Y, 0), string.Format("transZ:{0}", root.TransPt.Z), layer, 3, 25, 50);

            root.Child.ForEach(x => PrintZ(x, layer));
        }

        private static void PrintAD(ThDrainageTreeNode root, string layer)
        {
            if (root.Parent != null)
            {
                var line = new Line(root.Parent.TransPt, root.TransPt);
                DrawUtils.ShowGeometry(line, layer, 151);
            }
            root.Child.ForEach(x => PrintAD(x, layer));
        }


    }
}
