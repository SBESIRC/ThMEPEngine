using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model.Algorithm
{
    /// <summary>
    /// 博弈树算法
    /// </summary>
    public class BeamGameTreeAlgorithm_Clone
    {
        private class BeamGameNation
        {
            public BeamGameNation()
            {
                Players = new List<BeamGamePlayer>();
                Boundarys = new List<Polyline>();
            }
            public List<BeamGamePlayer> Players { get; set; }

            public List<Polyline> Boundarys { get; set; }
        }

        private class BeamGamePlayer
        {
            public List<ThBeamTopologyNode> Nodes { get; set; }
        }

        private class Scoreboard
        {
            public Scoreboard()
            {
                Boundarys = new List<Polyline>();
            }
            public int[] board { get; set; }
            public List<Polyline> Boundarys { get; set; }
        }

        private int PlayerCount { get; set; }
        private double AverageArea { get; set; }
        private double AverageCount { get; set; }
        private int Percentage { get; set; } = 20;

        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private Tuple<int[], int> CheckerboardCache { get; set; }

        private List<BeamGameNation> Nations { get; set; }
        List<List<ThBeamTopologyNode>> Space { get; set; }
        List<ThBeamTopologyNode> Nodes { get; set; }

        Dictionary<ThBeamTopologyNode, int> NodeCoordinate { get; set; }
        Dictionary<int, Polyline> UnionPolygonDic { get; set; }

        private Random random = new Random();

        //public int[] ChessGameResult {
        //    get
        //    {
        //        return CheckerboardCache.Where(o => Array.IndexOf(o.Key, 0) < 0).OrderByDescending(o => o.Value).FirstOrDefault().Key;
        //    }
        //}

        public BeamGameTreeAlgorithm_Clone(List<List<ThBeamTopologyNode>> space, List<ThBeamTopologyNode> nodes)
        {
            Space = space;
            Nodes = nodes;
            PlayerCount=space.Count;
            Nations = new List<BeamGameNation>();
            //CheckerboardCache = new Dictionary<int[], int>();
            CheckerboardCache = new Tuple<int[], int>(null, 0);
            NodeCoordinate = new Dictionary<ThBeamTopologyNode, int>();
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
            var secondCount = 0;
            if (space.Count > 1 && (secondCount = space.OrderBy(o => o.Count).ToList()[1].Count)< AverageCount / 2)
            {
                AverageCount = secondCount * 2;
            }
            SpatialIndex = new ThCADCoreNTSSpatialIndex(this.Nodes.Select(o => o.Boundary).ToCollection());
        }

        public void Start()
        {
            //int[] CurrentBoard = new int[Nodes.Count];
            //var scoreCache = new int[PlayerCount,4]; // Row:PlayerCount Cell:4
            //PlayChess(CurrentBoard, scoreCache);

            Pretreatment();
            var Scoreboards = new List<Scoreboard>(){
                new Scoreboard()
                {
                    board = new int[Nodes.Count]
                }
            };
            for (int i = 0; i < Nations.Count - 1; i++)
            {
                int Round = i;
                var boundarys = Nations[Round].Boundarys;
                for (int j = 0; j < Scoreboards.Count; j++)
                {
                    Scoreboards[j].Boundarys.AddRange(boundarys);
                }
                Scoreboards = GameRound(Round, Scoreboards);
                //GameStart(Round, CurrentBoard);
            }
            //DrawNationFirst(Scoreboards.Select(o=>o.board).ToList());
            StatisticalScore(Scoreboards);
        }

        private void DrawNationFirst(List<int[]> scoreboard)
        {
            using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            {
                Vector3d Allvector = new Vector3d(0, 100000, 0);
                Vector3d vector = new Vector3d(100000, 0, 0);
                Matrix3d matrix = Matrix3d.Displacement(vector);
                for (int i = 0; i < scoreboard.Count; i++)
                {
                    for (int j = 0; j < Nations[0].Players[0].Nodes.Count; j++)
                    {
                        var polyline = Nations[0].Players[0].Nodes[j].Boundary.Clone() as Polyline;
                        polyline.ColorIndex =90;
                        polyline.TransformBy(matrix);
                        acad.ModelSpace.Add(polyline);
                    }
                    for (int j = 0; j<Nodes.Count; j++)
                    {
                        if (scoreboard[i][j] == 1)
                        {
                            var polyline = Nodes[j].Boundary.Clone() as Polyline;
                            polyline.ColorIndex =130;
                            polyline.TransformBy(matrix);
                            acad.ModelSpace.Add(polyline);
                        }
                    }
                    vector = vector + Allvector;
                    matrix = Matrix3d.Displacement(vector);
                }
            }
        }

        public void Revise()
        {
            var Result = CheckerboardCache;
            if (!Result.IsNull())
            {
                for (int i = 0; i < Result.Item1.Length; i++)
                {
                    var node = Nodes[i];
                    if (!node.CheckCurrentPixel(Nations[Result.Item1[i] - 1].Players.First().Nodes.First()))
                    {
                        node.SwapLayout();
                    }
                }
            }
            else
            {
                //throw new NotImplementedException();
            }
        }

        private List<Scoreboard> GameRound(int round, List<Scoreboard> scoreboards)
        {
            List<Scoreboard> NewScoreboards = new List<Scoreboard>();
            var nation = Nations[round];
            var LowScore = Evaluation(nation.Players) - Percentage - nation.Players.Count * 5;
            foreach (var scoreboard in scoreboards)
            {
                List<Scoreboard> refscoreboard = new List<Scoreboard>();
                refscoreboard.Add(scoreboard);
                PlayChessNew(scoreboard, LowScore, round + 1, ref refscoreboard);
                NewScoreboards.AddRange(refscoreboard);
            }
            return NewScoreboards;
        }
        private List<int[]> GameRoundOld(int round, List<int[]> scoreboards)
        {
            List<int[]> NewScoreboards = new List<int[]>();
            //var nation = Nations[round];
            //var LowScore = Evaluation(nation.Players) - Percentage - nation.Players.Count * 10;
            //foreach (var scoreboard in scoreboards)
            //{
            //    List<int[]> refscoreboard = new List<int[]>();
            //    refscoreboard.Add(scoreboard);
            //    PlayChessNew(scoreboard, LowScore, round + 1, ref refscoreboard);
            //    NewScoreboards.AddRange(refscoreboard);
            //}
            return NewScoreboards;
        }

        private void StatisticalScore(List<Scoreboard> currentBoards)
        {
            int LastNationIndex = Nations.Count;
            currentBoards.ForEach(currentBoard =>
            {
                var score = Evaluation(currentBoard);
                if(CheckerboardCache.Item2 < score)
                {
                    var newcurrentBoard = currentBoard.board.Select(o =>
                    {
                        if (o == 0)
                        {
                            return LastNationIndex;
                        }
                        else
                            return o;
                    }).ToArray();
                    CheckerboardCache = new Tuple<int[], int>(newcurrentBoard, score);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentBoard">当前棋局</param>
        /// <param name="scoreCache"></param>
        /// <param name="stage"></param>
        /// <param name="permissionPlayer"></param>
        /// <param name="scoreboard"></param>
        private void PlayChessNew(Scoreboard currentBoard, int lowScore, int stage, ref List<Scoreboard> scoreboard)
        {
            List<BeamGamePlayer> permissionPlayer = Nations[stage - 1].Players;
            if (Array.IndexOf(currentBoard.board, 0) >= 0 && scoreboard.Count <= 500)
            {
                var places = Nodes.Where(o => currentBoard.board[NodeCoordinate[o]] == 0 && o.Neighbor.Any(x => permissionPlayer.Any(y=>y.Nodes.Contains(x.Item2)) || (Nodes.Contains(x.Item2) && currentBoard.board[NodeCoordinate[x.Item2]] == stage))).ToList();
                ThBeamTopologyNode place;
                while (!(place = places.FirstOrDefault()).IsNull())
                {
                    var NewBoard = new int[Nodes.Count];
                    currentBoard.board.CopyTo(NewBoard, 0);
                    NewBoard[NodeCoordinate[place]] = stage;
                    if (scoreboard.Any(o => o.board.SequenceEqual(NewBoard)))
                    {
                        //此棋盘已存在
                        places.Remove(place);
                        continue;
                    }
                    if (Array.IndexOf(NewBoard, 0) < 0)
                    {
                        //棋盘已被填充满
                        places.Remove(place);
                        continue;
                    }
                    //var NewcurrentBoard = EliminateDents(permissionPlayer, stage, NewBoard);
                    NewBoard = EliminateDents(stage, NewBoard);

                    if (scoreboard.Any(o => o.board.SequenceEqual(NewBoard)))
                    {
                        places.Remove(place);
                        continue;
                    }
                    var nodeSet = new List<ThBeamTopologyNode>();
                    for (int i = 0; i < NewBoard.Length; i++)
                    {
                        if (currentBoard.board[i] == 0 && NewBoard[i] ==stage)
                        {
                            nodeSet.Add(Nodes[i]);
                        }
                    }
                    if (nodeSet.All(o => places.Contains(o)))
                    {
                        places = places.Except(nodeSet).ToList();
                    }
                    else
                    {
                        places.Remove(place);
                    }

                    var NewcurrentBoard = AdsorbDiscreteRegions(stage, NewBoard);
                    if (Evaluation(NewcurrentBoard.board, stage) <= lowScore)
                    {
                        continue;
                    }
                    if (scoreboard.Any(o => o.board.SequenceEqual(NewcurrentBoard.board)))
                    {
                        continue;
                    }
                    scoreboard.Add(NewcurrentBoard);
                    PlayChessNew(NewcurrentBoard, lowScore, stage, ref scoreboard);
                }
                //旧代码
                {
                    //foreach (var place in places)
                    //{
                    //    var NewcurrentBoard = new int[Nodes.Count];
                    //    currentBoard.CopyTo(NewcurrentBoard, 0);
                    //    NewcurrentBoard[NodeCoordinate[place]] = stage;
                    //    if (scoreboard.Any(o => o.SequenceEqual(NewcurrentBoard)))
                    //    {
                    //        此棋盘已存在
                    //        continue;
                    //    }
                    //    if (Array.IndexOf(NewcurrentBoard, 0) < 0)
                    //    {
                    //        棋盘已被填充满
                    //        continue;
                    //    }
                    //    NewcurrentBoard = EliminateDents(permissionPlayer, stage, NewcurrentBoard);
                    //    if (scoreboard.Any(o => o.SequenceEqual(NewcurrentBoard)))
                    //    {
                    //        continue;
                    //    }
                    //    scoreboard.Add(NewcurrentBoard);
                    //    PlayChessNew(NewcurrentBoard, lowScore, stage, ref scoreboard);
                    //}
                }
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// 游戏开始前的预处理操作
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void Pretreatment()
        {
            //为玩家分组
            GroupingPlayer();
            SpatialIndex = new ThCADCoreNTSSpatialIndex(this.Nodes.Select(o => o.Boundary).ToCollection());
            for (int i = 0; i < Nodes.Count; i++)
            {
                NodeCoordinate.Add(Nodes[i], i);
            }
        }

        /// <summary>
        /// 玩家分组
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void GroupingPlayer()
        {
            for (int i = 0; i < this.Space.Count; i++)
            {
                if (this.Space[i].Count == 1)
                {
                    //this.Nodes.Add(this.Space[i][0]);
                }
                else
                {
                    BeamGamePlayer player = new BeamGamePlayer();
                    player.Nodes = this.Space[i];
                    var UnionPolygon = this.Space[i].UnionPolygon();
                    var ConvexPolyline = UnionPolygon.ConvexHullPL();
                    var polyline = ConvexPolyline.Buffer(-1000)[0] as Polyline;
                    var objs = SpatialIndex.SelectCrossingPolygon(polyline);
                    foreach (Polyline obj in objs)
                    {
                        var node = Nodes.FirstOrDefault(o => o.Boundary.Equals(obj));
                        if (!node.IsNull())
                        {
                            var NewUnionPolygon = node.UnionPolygon(UnionPolygon);
                            var NewConvexPolyline = NewUnionPolygon.ConvexHullPL();
                            if (NewUnionPolygon.Area / NewConvexPolyline.Area > UnionPolygon.Area / ConvexPolyline.Area)
                            {
                                if (!node.CheckCurrentPixel(this.Space[i].First()))
                                    node.SwapLayout();
                                player.Nodes.Add(node);
                                Nodes.Remove(node);
                            }
                        }
                    }

                    var nation = Nations.FirstOrDefault(o => o.Players.First().Nodes.First().CheckCurrentPixel(player.Nodes.First()));
                    if (nation.IsNull())
                    {
                        nation = new BeamGameNation();
                        nation.Players.Add(player);
                        Nations.Add(nation);
                    }
                    else
                    {
                        nation.Players.Add(player);
                    }
                }
            }
            bool Signal = true;
            while (Signal)
            {
                Signal = false;
                for (int i = 0; i < Nodes.Count; i++)
                {
                    var node = Nodes[i];
                    if (node.LayoutLines.edges.Count == 0 && node.Neighbor.Count(o => Nodes.Contains(o.Item2) && o.Item2.LayoutLines.edges.Count > 0) < 2)
                    {
                        Signal = true;
                        Nodes.Remove(node);
                        //using(Linq2Acad.AcadDatabase acad =Linq2Acad.AcadDatabase.Active())
                        //{
                        //    acad.ModelSpace.Add(node.Boundary.Buffer(-500)[0] as Polyline);
                        //}
                    }
                }
            }
            //var deleteNodes = Nodes.Where(node => node.LayoutLines.edges.Count == 0 && node.Neighbor.Count(o => Nodes.Contains(o.Item2) && o.Item2.LayoutLines.edges.Count > 0) < 2);
            for (int i = 0; i < Nations.Count; i++)
            {
                Nations[i].Boundarys = Nations[i].Players.SelectMany(o => o.Nodes).ToList().UnionPolygons();
            }
            Nations = Nations.OrderBy(o => o.Players.Count).ThenByDescending(o => o.Players.Sum(x => x.Nodes.Count)).ToList();
        }

        /// <summary>
        /// 评估分数
        /// </summary>
        /// <param name="currentBoard"></param>
        /// <returns></returns>
        private int Evaluation(Scoreboard scoreboard)
        {
            var polygons = scoreboard.Boundarys;
            var nodes = new List<ThBeamTopologyNode>();
            for (int j = 0; j < scoreboard.board.Length; j++)
            {
                if (scoreboard.board[j] == 0)
                    nodes.Add(Nodes[j]);
            }
            var UnionPolygons = nodes.UnionPolygons(Nations.Last().Boundarys);
            polygons.AddRange(UnionPolygons);
            int BaseScore = 500;
            polygons.ForEach(o =>
            {
                var ConvexPolyline = o.ConvexHullPL();
                var weights = o.Area > AverageArea ? o.Area / AverageArea : 1;//为大面积附加权重，使其'脱颖而出'
                var score = (int)Math.Ceiling(o.Area / ConvexPolyline.Area * 100 * weights);
                BaseScore += score;
            });
            BaseScore -= polygons.Count * 100;
            return BaseScore;
        }

        /// <summary>
        /// 评估分数
        /// </summary>
        /// <param name="currentBoard"></param>
        /// <returns></returns>
        private int Evaluation(int[] currentBoard, int stage)
        {
            var nodes = new List<ThBeamTopologyNode>();
            for (int i = 0; i < currentBoard.Length; i++)
            {
                if (currentBoard[i] == stage)
                    nodes.Add(Nodes[i]);
            }
            var UnionPolygons = nodes.UnionPolygons(this.Nations[stage -1].Boundarys);
            int BaseScore = 300;
            UnionPolygons.ForEach(o =>
            {
                var ConvexPolyline = o.ConvexHullPL();
                var score = (int)Math.Ceiling(o.Area / ConvexPolyline.Area * 100);
                BaseScore += score;
            });
            BaseScore -= UnionPolygons.Count * 100;
            return BaseScore;
        }

        /// <summary>
        /// 评估分数
        /// </summary>
        /// <param name="currentBoard"></param>
        /// <returns></returns>
        private int Evaluation(List<BeamGamePlayer> permissionPlayer)
        {
            var nodes = permissionPlayer.SelectMany(o => o.Nodes).ToList();
            var UnionPolygons = nodes.UnionPolygons();
            int BaseScore = 300;
            UnionPolygons.ForEach(o =>
            {
                var ConvexPolyline = o.ConvexHullPL();
                var score = (int)Math.Ceiling(o.Area / ConvexPolyline.Area * 100);
                BaseScore += score;
            });
            BaseScore -= UnionPolygons.Count * 100;
            return BaseScore;
        }

        /// <summary>
        /// 消除凹包
        /// </summary>
        private int[] EliminateDents(int index , int[] board)
        {
            var NewBoard = new int[Nodes.Count];
            var UnionPolygons = new List<Polyline>();
            while (!NewBoard.SequenceEqual(board))
            {
                var nodes = new List<ThBeamTopologyNode>();
                board.CopyTo(NewBoard, 0);
                for (int i = 0; i < board.Length; i++)
                {
                    if (board[i] == index)
                        nodes.Add(Nodes[i]);
                }
                UnionPolygons = nodes.UnionPolygons(this.Nations[index -1].Boundarys);
                UnionPolygons.ForEach((Polygon) =>
                {
                    bool Signal = true;
                    while (Signal)
                    {
                        Signal = false;
                        var ConvexPolyline = Polygon.ConvexHullPL();
                        var polyline = ConvexPolyline.Buffer(-1000)[0] as Polyline;
                        var objs = SpatialIndex.SelectCrossingPolygon(polyline);
                        foreach (Polyline obj in objs)
                        {
                            var nodeindex = Nodes.FindIndex(o => o.Boundary.Equals(obj));
                            if (nodeindex > -1 && board[nodeindex] == 0)
                            {
                                var NewUnionPolygon = Nodes[nodeindex].UnionPolygon(Polygon);
                                var NewConvexPolyline = NewUnionPolygon.ConvexHullPL();
                                if (NewUnionPolygon.Area / NewConvexPolyline.Area > Polygon.Area / ConvexPolyline.Area - 0.05)
                                {
                                    Signal = true;
                                    board[nodeindex] = index;
                                }
                            }
                        }
                    }
                });
                //using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
                //{
                //    var polyline = UnionPolygons[0];
                //    polyline.ColorIndex =2;
                //    acad.ModelSpace.Add(polyline);

                //    var polyline2 = polyline.ConvexHullPL();
                //    polyline2.ColorIndex =2;
                //    acad.ModelSpace.Add(polyline2);

                //    var polyline1 = polyline.ConvexHullPL().Buffer(-1000)[0] as Polyline;
                //    polyline1.ColorIndex =5;
                //    acad.ModelSpace.Add(polyline1);

                //}
            }
            return NewBoard;

            UnionPolygons.AddRange(Nations.Take(index - 1).SelectMany(o => o.Boundarys));
            Scoreboard NewScoreBoard = new Scoreboard();
            NewScoreBoard.board = NewBoard;
            NewScoreBoard.Boundarys = UnionPolygons;
            //return NewScoreBoard;
        }

        /// <summary>
        /// 吸附离散区域
        /// </summary>
        /// <param name="permissionPlayer"></param>
        /// <param name="index"></param>
        /// <param name="currentBoard"></param>
        /// <returns></returns>
        private Scoreboard AdsorbDiscreteRegions(int index, int[] board)
        {
            var nodeSet = new List<ThBeamTopologyNode>();
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == 0)
                {
                    nodeSet.Add(Nodes[i]);
                }
            }

            var RemainingSpace = Nations.Skip(index).SelectMany(o => o.Boundarys);
            var polylines = nodeSet.UnionPolygons();
            polylines = polylines.Where(polygon =>
            {
                var pts = polygon.GetPoints();
                if (RemainingSpace.Any(o => pts.Count(x => o.DistanceTo(x, false) < 10) > 1))
                {
                    return false;
                }
                return true;
            }).ToList();
            foreach (var polyline in polylines)
            {
                var objs = SpatialIndex.SelectWindowPolygon(polyline.Buffer(1000)[0] as Polyline);
                foreach (Polyline obj in objs)
                {
                    var nodeindex = Nodes.FindIndex(o => o.Boundary.Equals(obj));
                    if (nodeindex > -1 && board[nodeindex] == 0)
                    {
                        nodeSet.Add(Nodes[nodeindex]);
                        board[nodeindex] = index;
                    }
                }
            }

            var UnionPolygons = new List<Polyline>();
            UnionPolygons = nodeSet.UnionPolygons(this.Nations[index -1].Boundarys);
            UnionPolygons.AddRange(Nations.Take(index - 1).SelectMany(o => o.Boundarys));
            Scoreboard NewScoreBoard = new Scoreboard();
            NewScoreBoard.board = board;
            NewScoreBoard.Boundarys = UnionPolygons;
            return NewScoreBoard;
        }
    }
}
