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
        private List<ThSaniterayTerminal> Terminal { get; set; }
        //private List<ThValve> AngleValve { get; set; }
        private Dictionary<Point3d, List<Line>> PtDict { get; set; }
        private Dictionary<Point3d, bool> PtCoolHotDict { get; set; }
        public Dictionary<Point3d, ThSaniterayTerminal> PtTerminal { get; set; }
        public Dictionary<Point3d, Point3d> TerminalPairDict { get; set; }
        public List<ThDrainageTreeNode> OriRootList { get; set; }
        public List<ThDrainageTreeNode> MergedRootList { get; set; }
        public ThDraingeTreeEngine(ThDrainageADPDataPass dataPass, Dictionary<Point3d, List<Line>> ptDict, Dictionary<Point3d, bool> ptCoolHotDict)
        {
            Terminal = dataPass.Terminal;
            //AngleValve = dataPass.AngleValve;
            PtDict = ptDict;
            PtCoolHotDict = ptCoolHotDict;

            PtTerminal = new Dictionary<Point3d, ThSaniterayTerminal>();
            TerminalPairDict = new Dictionary<Point3d, Point3d>();
            OriRootList = new List<ThDrainageTreeNode>();
            MergedRootList = new List<ThDrainageTreeNode>();
        }

        public void BuildDraingeTree()
        {
            //找管线对应末端洁具和可能的起点
            Terminal.ForEach(x => x.Boundary = x.Boundary.Buffer(1).OfType<Polyline>().OrderByDescending(x => x.Area).First());
            ThDrainageADTreeService.GetEndTerminal(PtDict, Terminal, out var ptTerminalTemp, out var ptStart);
            this.PtTerminal = ptTerminalTemp;

            //确定冷热水末端点位组
            this.TerminalPairDict = ThDrainageADTreeService.GetTerminalPairDict(PtTerminal);




            //确定冷热水起点
            var ptCoolHotStartDict = ThDrainageADTreeService.CheckCoolHotStartPt(ptStart, PtTerminal, PtCoolHotDict);

            //造原始树:所有的冷水起点，多热水器每个热水器一个热水起点
            OriRootList = new List<ThDrainageTreeNode>();
            foreach (var startPt in ptCoolHotStartDict)
            {
                var root = ThDrainageADTreeService.BuildTree(startPt.Key, PtDict);
                OriRootList.Add(root);
            }

            //设置节点冷热属性
            ThDrainageADTreeService.SetCoolHot(OriRootList, ptCoolHotStartDict);
            //设置节点洁具
            ThDrainageADTreeService.SetTerminal(OriRootList, PtTerminal);

            //复制多情况冷热树
            MergedRootList = ThDrainageADTreeService.MergeCoolHotTree(OriRootList);

            //冷热成对洁具
            ThDrainageADTreeService.SetTerminalPairSingleTree(MergedRootList, TerminalPairDict);

        }

        //private void SetTerminalDir()
        //{
        //    var tol = new Tolerance(1, 1);
        //    for (int i = 0; i < PtTerminal.Count(); i++)
        //    {
        //        var item = PtTerminal.ElementAt(i);
        //        if (item.Value.Dir != default(Vector3d))
        //        {
        //            //冷热水同时有的已经做过一次的跳过
        //            continue;
        //        }
        //        var projPt = new Point3d(item.Key.X, item.Key.Y, 0);
        //        var angleValve = AngleValve.Where(x => x.InsertPt.IsEqualTo(projPt, tol)).FirstOrDefault();
        //        if (angleValve != null)
        //        {
        //            item.Value.Dir = angleValve.Dir;
        //        }
        //    }
        //}

    }
}
