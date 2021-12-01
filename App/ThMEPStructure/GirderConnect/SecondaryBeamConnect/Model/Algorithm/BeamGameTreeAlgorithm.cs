using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model.Algorithm
{
    /// <summary>
    /// 博弈树算法
    /// </summary>
    public class BeamGameTreeAlgorithm
    {
        private int PlayerCount { get; set; }
        private double AverageArea { get; set; }
        private double AverageCount { get; set; }

        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public Dictionary<int[],int> CheckerboardCache { get; set; }
        List<List<ThBeamTopologyNode>> Space { get; set; }
        List<ThBeamTopologyNode> Nodes { get; set; }

        Dictionary<ThBeamTopologyNode,int > NodeCoordinate { get; set; }
        Dictionary<int,Polyline> UnionPolygonDic { get; set; }

        private Random random = new Random();

        public int[] ChessGameResult { 
            get 
            { 
                return CheckerboardCache.Where(o => Array.IndexOf(o.Key, 0) < 0).OrderByDescending(o => o.Value).FirstOrDefault().Key; 
            } 
        }

        public BeamGameTreeAlgorithm(List<List<ThBeamTopologyNode>> space, List<ThBeamTopologyNode> nodes)
        {
            Space = space;
            Nodes = nodes;
            PlayerCount=space.Count;
            CheckerboardCache = new Dictionary<int[], int>();
            NodeCoordinate = new Dictionary<ThBeamTopologyNode, int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeCoordinate.Add(nodes[i], i);
            }
            UnionPolygonDic = new Dictionary<int, Polyline>();
            for (int i = 0; i < PlayerCount; i++)
            {
                var player = Space[i];
                UnionPolygonDic.Add(i + 1, player.UnionPolygon());
            }
            var AreaSum = space.Sum(o => o.Sum(x => x.Boundary.Area));
            AverageArea = AreaSum / space.Count;
            var CountSum = space.Sum(o => o.Count);
            AverageCount = CountSum / space.Count;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(nodes.Select(o => o.Boundary).ToCollection());
        }

        public void Start()
        {
            int[] CurrentBoard = new int[Nodes.Count];
            var scoreCache = new int[PlayerCount,4]; // Row:PlayerCount Cell:4
            PlayChess(CurrentBoard, scoreCache);
        }

        private void PlayChess(int[] currentBoard, int[,] scoreCache)
        {
            if (Array.IndexOf(currentBoard, 0) >= 0)
            {
                //有棋子还没有落子，游戏继续
                for (int i = 0; i < PlayerCount; i++)
                {
                    if (scoreCache[i, 0] > scoreCache[i, 1] && scoreCache[i, 1] > scoreCache[i, 2] && scoreCache[i, 2] > scoreCache[i, 3])
                    {
                        //连续三步都是'差'的操作，则剪枝，不再考虑后续的走动
                        return;
                    }
                    //if (scoreCache[i, 0] > scoreCache[i, 1] && scoreCache[i, 1] > scoreCache[i, 2] && scoreCache[i, 2] > scoreCache[i, 3] && scoreCache[i, 3] > scoreCache[i, 4])
                    //{
                    //    //连续四步都是'差'的操作，则剪枝，不再考虑后续的走动
                    //    return;
                    //}
                }
                for (int i = 0; i < PlayerCount; i++)
                {
                    var player = Space[i];
                    if (player.Count >= AverageCount / 2)
                    {
                        var places = Nodes.Where(o => currentBoard[NodeCoordinate[o]] == 0 && o.Neighbor.Any(x => player.Contains(x.Item2) || (Nodes.Contains(x.Item2) && currentBoard[NodeCoordinate[x.Item2]] == i + 1)));
                        foreach (var place in places)
                        {
                            var NewcurrentBoard = new int[Nodes.Count];
                            currentBoard.CopyTo(NewcurrentBoard, 0);
                            NewcurrentBoard[NodeCoordinate[place]] = i + 1;
                            if (CheckerboardCache.Keys.Any(o => o.SequenceEqual(NewcurrentBoard)))
                            {
                                //此棋盘已存在
                                continue;
                            }
                            CheckerboardCache.Add(NewcurrentBoard, 0);

                            var NewcurrentBoardClone = new int[Nodes.Count];
                            NewcurrentBoard.CopyTo(NewcurrentBoardClone, 0);
                            NewcurrentBoardClone = EliminateDents(i + 1, NewcurrentBoardClone);
                            NewcurrentBoardClone = AdjustCurrentBoard(NewcurrentBoardClone);
                            if (CheckerboardCache.Keys.Any(o => o.SequenceEqual(NewcurrentBoardClone)))
                            {
                                //此棋盘已存在
                                continue;
                            }
                            var Fraction = Evaluation(NewcurrentBoardClone);
                            CheckerboardCache.Add(NewcurrentBoardClone, Fraction);

                            var NewScore = scoreCache.Clone() as int[,];
                            NewScore[i, 0] = NewScore[i, 1];
                            NewScore[i, 1] = NewScore[i, 2];
                            NewScore[i, 2] = NewScore[i, 3];
                            //NewScore[i, 3] = NewScore[i, 4];
                            //NewScore[i, 4] = Fraction;
                            NewScore[i, 3] = Fraction;
                            PlayChess(NewcurrentBoardClone, NewScore);
                        }
                    }
                }
            }
            else
            {
                // GAME OVER
            }
        }

        /// <summary>
        /// 调整棋盘
        /// </summary>
        /// <param name="newcurrentBoard"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private int[] AdjustCurrentBoard(int[] currentBoard)
        {
            var newSpace = new List<List<ThBeamTopologyNode>>();
            for (int i = 0; i < Space.Count; i++)
            {
                var NewNodes = Space[i].ToArray().ToList();
                for (int j = 0; j < currentBoard.Length; j++)
                {
                    if (currentBoard[j] == i + 1)
                    {
                        NewNodes.Add(Nodes[j]);
                    }
                }
                newSpace.Add(NewNodes);
            }
            for (int i = 0; i < newSpace.Count; i++)
            {
                for (int j = i + 1; j < newSpace.Count; j++)
                {
                    if (newSpace[i].IsNeighbor(newSpace[j], true))
                    {
                        for (int k = 0; k < currentBoard.Length; k++)
                        {
                            if (currentBoard[k] == j + 1)
                            {
                                currentBoard[k] = i + 1;
                            }
                        }
                    }
                }
            }
            return currentBoard;
        }

        /// <summary>
        /// 评估分数
        /// </summary>
        /// <param name="currentBoard"></param>
        /// <returns></returns>
        private int Evaluation(int[] currentBoard)
        {
            var newSpace = new List<List<ThBeamTopologyNode>>();
            for (int i = 0; i < Space.Count; i++)
            {
                var NewNodes = Space[i].ToArray().ToList();
                for (int j = 0; j < currentBoard.Length; j++)
                {
                    if (currentBoard[j] == i + 1)
                    {
                        NewNodes.Add(Nodes[j]);
                    }
                }
                var neighbor = newSpace.FirstOrDefault(o => o.IsNeighbor(NewNodes, true));
                if (neighbor != null)
                {
                    neighbor.AddRange(NewNodes);
                }
                else
                {
                    newSpace.Add(NewNodes);
                }
            }
            int BaseScore = 500;
            newSpace.ForEach(o =>
            {
                var UnionPolygon = o.UnionPolygon();
                var ConvexPolyline = UnionPolygon.ConvexHullPL();
                var weights = UnionPolygon.Area > AverageArea ? UnionPolygon.Area / AverageArea : 1;//为大面积附加权重，使其'脱颖而出'
                var score = (int)Math.Ceiling(UnionPolygon.Area / ConvexPolyline.Area * 100 * weights);
                BaseScore += score;
            });
            BaseScore -= newSpace.Count * 100;
            return BaseScore;
        }

        /// <summary>
        /// 消除凹包
        /// </summary>
        private int[] EliminateDents(int index , int[] currentBoard)
        {
            bool Signal = true;
            while (Signal)
            {
                Signal = false;
                var Pieces = new List<ThBeamTopologyNode>();
                for (int i = 0; i < currentBoard.Length; i++)
                {
                    if (currentBoard[i] == index)
                    {
                        Pieces.Add(Nodes[i]);
                    }
                }
                var unionPolygon = Pieces.UnionPolygon(UnionPolygonDic[index]);
                var ConvexPolyline = unionPolygon.ConvexHullPL();
                var polyline = ConvexPolyline.Buffer(-1000)[0] as Polyline;
                var objs = SpatialIndex.SelectFence(polyline);
                foreach (Polyline obj in objs)
                {
                    var nodeindex = Nodes.FindIndex(o => o.Boundary.Equals(obj));
                    if (nodeindex > -1 && currentBoard[nodeindex] == 0)
                    {
                        Signal = true;
                        currentBoard[nodeindex] = index;
                    }
                }
            }
            return currentBoard;
        }
    }

    public class BeamGameTree
    {
        public BeamGameTree Parent { get; set; }
        public List<BeamGameTree> Childs { get; set; }
        
    }
}
