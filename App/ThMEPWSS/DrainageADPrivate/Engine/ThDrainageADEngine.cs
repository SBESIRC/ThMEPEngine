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
    internal class ThDrainageADEngine
    {
        internal static void DrainageTransADEngine(ThDrainageADPDataPass dataPass)
        {
            //--成树--
            //找管线点位对应的线
            var pipes = new List<Line>();
            pipes.AddRange(dataPass.CoolPipeTopView);
            pipes.AddRange(dataPass.HotPipeTopView);
            pipes.AddRange(dataPass.VerticalPipe);
            var ptDict = ThDrainageADTreeService.GetPtDict(pipes);

            //找管线对应末端和可能的起点
            ThDrainageADTreeService.GetEndTerminal(ptDict, dataPass.Terminal, out var ptTerminal, out var ptStart);

            //几何起点树
            var rootList = ThDrainageADTreeService.BuildTree(ptStart, ptDict);

            //找热水起点
            var ptCoolHotDict = ThDrainageADTreeService.CheckCoolHotPt(ptStart, ptTerminal, ptDict, dataPass);
            var hotStartPt = ptTerminal.Where(x => x.Value.Type == ThDrainageADCommon.TerminalType.WaterHeater && ptCoolHotDict[x.Key] == false).Select(x => x.Key).ToList();

            //热水起点树.不能放到上面一起。有可能有两个热水器 line的次数是两次分别计算
            var hotStartTree = new Dictionary<Point3d, ThDrainageTreeNode>();
            foreach (var hotStart in hotStartPt)
            {
                var newHotStartList = new List<Point3d>() { hotStart };
                var hotRootList = ThDrainageADTreeService.BuildTree(newHotStartList, ptDict);
                hotStartTree.Add(hotStart, hotRootList.First());

                rootList.Add(hotRootList.First());
            }

            //对应洁具
            ThDrainageADTreeService.SetTerminal(rootList, ptTerminal);
            ThDrainageADTreeService.SetTerminalPair(rootList);

            //对应冷热
            ThDrainageADTreeService.SetCoolHot(rootList, ptCoolHotDict);


            rootList.SelectMany(x => x.GetLeaf()).ForEach(l =>
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

            rootList.ForEach(x => ThDrainageADTreeService.PrintTree(x, String.Format("l0tree{0}", rootList.IndexOf(x))));
            //--管径计算--



            //------

            //--转换--
            //ThDrainageADEngine.TransformTopToAD(dataPass);

            //

            //
        }


        internal static void TransformTopToAD(ThDrainageADPDataPass dataPass)
        {
            var coolPipeTopView = dataPass.CoolPipeTopView;
            var vierticalPipe = dataPass.VerticalPipe;

            var transService = new TransformTopToADService();
            var transLine = coolPipeTopView.Select(x => transService.TransformLine(x)).ToList();
            var transVLine = vierticalPipe.Select(x => transService.TransformLine(x)).ToList();

            DrawUtils.ShowGeometry(transLine, "l0TranslineCool");
            DrawUtils.ShowGeometry(transVLine, "l0TVierticalPipe");
        }


    }
}
