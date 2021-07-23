using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageADConvertEngine
    {
        public static void convertDiagram(ThDrainageSDADDataExchange dataset)
        {
            //处理连阀管线：加入阀的中心线
            //处理连立管管线：管线延长到立管中点，返回只有在管线中间的立管
            ThDrainageADLineService.addValveToPipe(dataset.pipes, dataset.valveList);
            var stackNotInEnd = ThDrainageADLineService.connectPipeWhenStack(dataset.pipes, dataset.stackList);
            DrawUtils.ShowGeometry(dataset.pipes, "l0pipe2", 15);

            //找起点建树
            //var startPt = ThDrainageADLineService.findStartPoint(dataset.pipes, dataset.valveList);
            var startPt = dataset.startPt;
            var root = ThDrainageSDTreeService.buildPipeTree(dataset.pipes, startPt, false);

            //为后面处理，立管处的node插入相同的node
            var traversedStack = new List<int>();
            ThDrainageADLineService.insertStackNode(stackNotInEnd, root, traversedStack);

            //转轴测图
            var convertNodeDict = new Dictionary<ThDrainageSDTreeNode, ThDrainageSDTreeNode>();
            int stackDir = 0;
            convertNodeDict.Add(root, root);
            ThDrainageADConvertLineService.convertTree(root, convertNodeDict, stackNotInEnd, ref stackDir);

            //抽出轴侧管线
            var convertedPipe = new List<Line>();
            toADLine(root, convertNodeDict, convertedPipe);

            //插入末尾立管
            var endNode = root.getLeaf();
            var endStackPipe = ThDrainageADConvertLineService.addEndStackPipe(endNode, convertedPipe, convertNodeDict);
            DrawUtils.ShowGeometry(endStackPipe.SelectMany(x => x.Value).ToList(), "l2ADEndLines", 201, 25);

            //插入水表阀
            var valveOutput = ThDrainageADConvertValveService.convertValveInPipe(dataset.valveList, convertedPipe, convertNodeDict);
            DrawUtils.ShowGeometry(convertedPipe, "l2ADLines", 200, 30);
            valveOutput.ForEach(x => DrawUtils.ShowGeometry(x.position, x.dir, "l2valve", 40, 30));

            //插入末端阀
            var toiDict = ThDrainageADConvertValveService.findToiletType(endNode, dataset.toiletList);
            var endValveOutput = ThDrainageADConvertValveService.convertEndValve( endStackPipe, toiDict, convertNodeDict);

            //for all final output data
            var convertedAllPipe = new List<Line>();
            convertedAllPipe.AddRange(convertedPipe);
            convertedAllPipe.AddRange(endStackPipe.SelectMany(x => x.Value).ToList());

            var valveOutputAll = new List<ThDrainageSDADBlkOutput>();
            valveOutputAll.AddRange(valveOutput);
            valveOutputAll.AddRange(endValveOutput);

            dataset.convertedPipes = convertedAllPipe;
            dataset.convertedValve = valveOutputAll;
          
        }

        private static void toADLine(ThDrainageSDTreeNode root, Dictionary<ThDrainageSDTreeNode, ThDrainageSDTreeNode> convertDict, List<Line> newTreeLine)
        {
            if (root.Child.Count > 0)
            {
                foreach (var c in root.Child)
                {
                    var newLine = new Line(convertDict[root].Node, convertDict[c].Node);
                    newTreeLine.Add(newLine);
                }
                foreach (var c in root.Child)
                {
                    toADLine(c, convertDict, newTreeLine);
                }
            }
        }
    }
}
