using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.DrainageSystemDiagram.Model;
using ThMEPWSS.DrainageSystemDiagram.Service;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageADConvertEngine
    {
        public static void convertDiagram(ThDrainageSDADDataExchange dataset, out List<Line> convertedAllPipe, out List<ThDrainageSDADBlkOutput> valveOutputAll)
        {
            convertedAllPipe = new List<Line>();
            valveOutputAll = new List<ThDrainageSDADBlkOutput>();

            if (dataset.pipes == null || dataset.pipes.Count == 0)
            {
                ThDrainageSDMessageServie.WriteMessage(ThDrainageSDMessageCommon.noPipe);
                return;
            }

            turnToilet(dataset.toiletList, dataset.archiExtractor);

            //处理连阀管线：加入阀的中心线
            //处理连立管管线：管线延长到立管中点，返回只有在管线中间的立管
            ThDrainageADLineService.addValveToPipe(dataset.pipes, dataset.valveList);
            var stackNotInEnd = ThDrainageADLineService.connectPipeWhenStack(dataset.pipes, dataset.stackList);
            //DrawUtils.ShowGeometry(dataset.pipes, "l0pipe2", 15);

            //找起点建树
            //var startPt = ThDrainageADLineService.findStartPoint(dataset.pipes, dataset.valveList);
            var startPt = ThDrainageADLineService.findStartPoint(dataset.pipes, dataset.startPt);
            if (startPt == Point3d.Origin)
            {
                ThDrainageSDMessageServie.WriteMessage(ThDrainageSDMessageCommon.noStart);
                return;
            }

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

            //插入末尾立管。必须先做。里面会检查末尾和主线相交。插入水表的时候会删除主线。
            var endNode = root.getLeaf();
            var endToiDict = ThDrainageADConvertValveService.findToiletType(endNode, dataset.toiletList);
            var endStackPipe = ThDrainageADConvertLineService.addEndStackPipe(endToiDict, convertedPipe, convertNodeDict);
            DrawUtils.ShowGeometry(endStackPipe.SelectMany(x => x.Value).ToList(), "l2ADEndLines", 201, 25);

            //插入水表阀
            var valveOutput = ThDrainageADConvertValveService.convertValveInPipe(dataset.valveList, convertedPipe, convertNodeDict);
            DrawUtils.ShowGeometry(convertedPipe, "l2ADLines", 200, 30);
            valveOutput.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l2valve", 40, 30));

            //插入末端阀
            var endValveOutput = ThDrainageADConvertValveService.convertEndValve(endStackPipe, endToiDict, convertNodeDict);

            //插入管径标注
            var allNodeDiaDict = new Dictionary<ThDrainageSDTreeNode, int>();
            double alpha = dataset.alpha;
            ThDrainageADDiameterDim.calculateTreeDiameter(root, endToiDict, allNodeDiaDict, alpha);
            allNodeDiaDict.ForEach(x => DrawUtils.ShowGeometry(convertNodeDict[x.Key].Node, "DN" + x.Value, "l0diamDim", 3, 25, 50));

            var nodeDia = new Dictionary<ThDrainageSDTreeNode, int>();
            ThDrainageADDiameterDim.selectChangeNode(root, allNodeDiaDict, nodeDia);
            nodeDia.ForEach(x => DrawUtils.ShowGeometry(convertNodeDict[x.Key].Node, "DN" + x.Value, "l1diamDim", 191, 30, 50));

            //设置躲避的线
            var allIsolateLine = new List<Line>();
            allIsolateLine.AddRange(convertedPipe);
            allIsolateLine.AddRange(endStackPipe.SelectMany(x => x.Value).ToList());
            var endValveLine = endValveOutput.SelectMany(x => ThDrainageADConvertValveService.toAllIsolateLine(x)).ToList();
            allIsolateLine.AddRange(endValveLine);
     
            //计算管径标注位置，主管支管 
            var nodeDiaDimOutput = ThDrainageADDiameterDim.calculatePositionDiaDim(nodeDia, convertNodeDict, allIsolateLine);
            nodeDiaDimOutput.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l1dim", 191, 30, 500));

            //计算管径标注位置，末端立管 
            var nodeDiaDimEndOutput = ThDrainageADDiameterDim.calculatePositionDiaDimEnd(allNodeDiaDict, endStackPipe, allIsolateLine);
            nodeDiaDimEndOutput.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l1dimEnd", 191, 30, 500));
            DrawUtils.ShowGeometry(allIsolateLine, "l0isolate", 2);

            //for all final output data
            convertedAllPipe.AddRange(convertedPipe);
            convertedAllPipe.AddRange(endStackPipe.SelectMany(x => x.Value).ToList());

            valveOutputAll.AddRange(valveOutput);
            valveOutputAll.AddRange(endValveOutput);
            valveOutputAll.AddRange(nodeDiaDimOutput);
            valveOutputAll.AddRange(nodeDiaDimEndOutput);

        }

        private static void turnToilet(List<ThTerminalToilet> allToiletList, List<ThExtractorBase> archiExtractor)
        {
            var toiletList = allToiletList.Where(x => x.SupplyCool.Count > 0).ToList();

            //所有空间建model 包括没有厕所的空间（后续建图需要）
            var roomPolyList = ThDrainageSDRoomService.getRoomList(archiExtractor);
            var roomList = ThDrainageSDRoomService.buildRoomModel(roomPolyList, toiletList);
            var filteredRoom = ThDrainageSDRoomService.filtRoomList(roomList);

            if (filteredRoom == null || toiletList == null || filteredRoom.Count == 0 || toiletList.Count == 0)
            {
                ThDrainageSDMessageServie.WriteMessage(ThDrainageSDMessageCommon.noRoomToilet);
                return;
            }

            //确定每个厕所在墙上的给水点位,调整厕所方向
            ThDrainageSDCoolPtService.findCoolSupplyPt(roomList, toiletList, out var aloneToilet);
            //toiletList.ForEach(x => x.SupplyCoolOnWall.ForEach(pt => DrawUtils.ShowGeometry(pt, "l0SupplyOnWall", 50, 35, 20, "C")));
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
