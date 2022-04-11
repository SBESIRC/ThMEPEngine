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

using ThMEPWSS.DrainageADPrivate.Data;
using ThMEPWSS.DrainageADPrivate.Service;
using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Engine
{
    internal class ThDraingeTreeEngine
    {
        private ThDrainageADPDataPass DataPass { get; set; }
        public Dictionary<Point3d, ThSaniterayTerminal> PtTerminal { get; set; }
        public Dictionary<Point3d, Point3d> TerminalPairDict { get; set; }
        public List<ThDrainageTreeNode> OriRootList { get; set; }
        public List<ThDrainageTreeNode> MergedRootList { get; set; }
        public ThDraingeTreeEngine(ThDrainageADPDataPass dataPass)
        {
            this.DataPass = dataPass;
            PtTerminal = new Dictionary<Point3d, ThSaniterayTerminal>();
            TerminalPairDict = new Dictionary<Point3d, Point3d>();
            OriRootList = new List<ThDrainageTreeNode>();
            MergedRootList = new List<ThDrainageTreeNode>();
        }

        public void BuildDraingeTree()
        {
            //找管线点位对应的线
            var pipes = new List<Line>();
            pipes.AddRange(DataPass.CoolPipeTopView);
            pipes.AddRange(DataPass.HotPipeTopView);
            pipes.AddRange(DataPass.VerticalPipe);
            var ptDict = ThDrainageADTreeService.GetPtDict(pipes);

            //找管线对应末端和可能的起点
            DataPass.Terminal.ForEach(x => x.Boundary = x.Boundary.Buffer(1).OfType<Polyline>().OrderByDescending(x => x.Area).First());
            ThDrainageADTreeService.GetEndTerminal(ptDict, DataPass.Terminal, out var ptTerminalTemp, out var ptStart);
            this.PtTerminal = ptTerminalTemp;
            //确定末端点位组
            this.TerminalPairDict = ThDrainageADTreeService.GetTerminalPairDict(PtTerminal);
            //确定冷热水起点
            var ptCoolHotStartDict = ThDrainageADTreeService.CheckCoolHotStartPt(ptStart, PtTerminal, ptDict, DataPass);

            //造原始树:所有的冷水起点，多热水器每个热水器一个热水起点
            OriRootList = new List<ThDrainageTreeNode>();
            foreach (var startPt in ptCoolHotStartDict)
            {
                var root = ThDrainageADTreeService.BuildTree(startPt.Key, ptDict);
                OriRootList.Add(root);
            }

            //对应冷热
            ThDrainageADTreeService.SetCoolHot(OriRootList, ptCoolHotStartDict);
            //洁具
            ThDrainageADTreeService.SetTerminal(OriRootList, PtTerminal);

            //复制多情况冷热树
            MergedRootList = ThDrainageADTreeService.MergeCoolHotTree(OriRootList);

            //冷热成对洁具
            ThDrainageADTreeService.SetTerminalPair(MergedRootList, TerminalPairDict);

            MergedRootList.SelectMany(x => x.GetLeaf()).ForEach(l =>
            {
                if (l.Terminal != null)
                {
                    DrawUtils.ShowGeometry(l.Node, l.Terminal.Type.ToString(), "l0leafterminal", 3, hight: 50);
                }
                if (l.TerminalPair != null)
                {
                    DrawUtils.ShowGeometry(new Line(l.Node, l.TerminalPair.Node), "l0leafterminalPair", 3);
                }
            });

            MergedRootList.ForEach(x => ThDrainageADTreeService.PrintTree(x, String.Format("l0tree{0}", MergedRootList.IndexOf(x))));

        }



    }
}
