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
            //--管道延长到立管--
            ThPipePreProcessService.ConnectPipeToNearVerticalPipe(dataPass, out var ptDict, out var ptCoolHotDict);
            //ptDict.ForEach(x => DrawUtils.ShowGeometry(x.Value, "l0finalline"));

            //--树--
            var treeEngine = new ThDraingeTreeEngine(dataPass, ptDict, ptCoolHotDict);
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
            // treeEngine.MergedRootList.ForEach(x => PrintTree(x, String.Format("l0tree{0}", treeEngine.MergedRootList.IndexOf(x))));
            ///////////

            //--管径计算--
            var dimEngine = new ThCalculateDimEngine(dataPass, treeEngine);
            dimEngine.CalculateDim();

            if (dimEngine.RootList.Count() == 0)
            {
                return;
            }
            /////////////
            //treeEngine.MergedRootList.ForEach(x => PrintDiam(x, String.Format("l0dimTree{0}", treeEngine.MergedRootList.IndexOf(x))));
            //dimEngine.RootList.ForEach(x => PrintDiam(x, String.Format("l0dimMaxTree{0}", dimEngine.RootList.IndexOf(x))));
            /////////////

            //--转换--
            ThDrainageTransformEngine.TransformTopToAD(dimEngine.RootList);
            ThDrainageTransformEngine.TransTreeListToSelect(dimEngine.RootList, dataPass.PrintBasePt);
            dimEngine.RootList.ForEach(x => PrintAD(x, string.Format("l1TransAD{0}", dimEngine.RootList.IndexOf(x))));

            //--摆放其他阀门--比管径先做，避让用
            var valveOutput = ThLayoutValveService.LayoutValve(dataPass, dimEngine.RootList);
            //valveOutput.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l1otherValve", 201, lineWeightNum: 30, l: 150));
            //valveOutput.ForEach(x =>
            //{
            //    if (x.Visibility.Count() > 0)
            //    {
            //        DrawUtils.ShowGeometry(x.Position, x.Visibility[ThDrainageADCommon.VisiName_valve], "l1otherValveVisi", 201, hight: 180);
            //    }
            //});


            //--摆放管径--
            var dim = ThLayoutDimService.LayoutDim(dimEngine.RootList);
            //dim.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l1Dim", 191, lineWeightNum: 30, l: 150));
            //dim.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Visibility[ThDrainageADCommon.VisiName_valve], "l1DimText", 191, hight: 180));

            //--摆放终端阀门--
            var endValve = ThLayoutAngleValveService.LayoutAngleValve(dimEngine.RootList, treeEngine.PtTerminal);
            //endValve.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l1EndValve", 40, lineWeightNum: 30, l: 150));
            //endValve.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Visibility[ThDrainageADCommon.VisiName_valve], "l1EndValveVisi", 40, hight: 180));

            //--断线--
            ThBreakPipeService.BreakPipe(dimEngine.RootList, out var coolPipe, out var hotPipe);

            dataPass.OutputDim.AddRange(dim);
            dataPass.OutputValve.AddRange(valveOutput);
            dataPass.OutputAngleValve.AddRange(endValve);
            dataPass.OutputCoolPipe.AddRange(coolPipe);
            dataPass.OutputHotPipe.AddRange(hotPipe);

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

        public static void PrintZ(ThDrainageTreeNode root, string layer)
        {
            DrawUtils.ShowGeometry(new Point3d(root.Pt.X + 20, root.Pt.Y, 0), string.Format("z:{0}", root.TransPt.Z), layer, 3, 25, 50);

            root.Child.ForEach(x => PrintZ(x, layer));
        }

        private static void PrintAD(ThDrainageTreeNode root, string layer)
        {
            if (root.Parent != null)
            {
                var line = new Line(root.Parent.TransPt, root.TransPt);
                var color = root.IsCool ? 140 : 221;
                DrawUtils.ShowGeometry(line, layer, color);
            }
            root.Child.ForEach(x => PrintAD(x, layer));
        }


    }
}
