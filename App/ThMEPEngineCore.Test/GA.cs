using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Overlay.Snap;
using Autodesk.AutoCAD.Colors;
using DotNetARX;

namespace ThCADCore.Test
{
    public class Point3dEx : IEquatable<Point3dEx>
    {
        public double X;
        public double Y;
        public double Z;

        //public Point3d Pt;
        double tolerance = 0.1;
        public Point3dEx(Point3d pt)
        {
            //Pt = pt;
            X = pt.X;
            Y = pt.Y;
            Z = pt.Z;
        }
        public Point3dEx(double x, double y, double z)
        {
            //Pt = new Point3d(x, y, z);
            X = x;
            Y = y;
            //Z = z;
        }
        public override int GetHashCode()
        {
            return (int)X ^ (int)Y;// ^ (int)Pt.Z;
        }
        public bool Equals(Point3dEx other)
        {
            return Math.Abs(X - other.X) < tolerance && Math.Abs(Y - other.Y) < tolerance;// && Math.Abs(Z - other.Z) < tolerance;
        }

        public Point3d ToPoint3d()
        {
            return new Point3d(X, Y, Z);
        }
    }

    public class GeneKey : IEquatable<GeneKey>
    {
        public int Dir = 0;
        public Point3dEx StartPt;
        public Point3dEx EndPt;
        public int FirstDir = 0;
        public override int GetHashCode()
        {
            return StartPt.GetHashCode() ^ EndPt.GetHashCode() ^ Dir.GetHashCode() ^ FirstDir.GetHashCode();
        }
        public bool Equals(GeneKey other)
        {
            return this.Dir.Equals(other.Dir)
                && this.StartPt.Equals(other.StartPt)
                && this.EndPt.Equals(other.EndPt)
                && this.FirstDir.Equals(other.FirstDir);
        }
    }

    public class Gene : IEquatable<Gene>
    {
        public int Dir = 0;
        public Point3dEx StartPt;
        public Point3dEx EndPt;
        public int FirstDir = 0;
        public List<Point3dEx> pts = new List<Point3dEx>();

        public GeneKey Key 
        { 
            get
            {
                return GetGeneKey(this.StartPt, this.EndPt, this.FirstDir, this.Dir);
            }
        }

        static public GeneKey GetGeneKey(Point3dEx startPt, Point3dEx endPt, int firstDir, int dir)
        {
            var key = new GeneKey();
            key.StartPt = startPt;
            key.EndPt = endPt;
            key.FirstDir = firstDir;
            key.Dir = dir;
            return key;
        }

        public override int GetHashCode()
        {
            return StartPt.GetHashCode() ^ EndPt.GetHashCode() ^ Dir.GetHashCode() ^ FirstDir.GetHashCode();
        }
        public bool Equals(Gene other)
        {
            return this.Dir.Equals(other.Dir)
                && this.StartPt.Equals(other.StartPt) 
                && this.EndPt.Equals(other.EndPt)
                && this.FirstDir.Equals(other.FirstDir);
        }

        public double GetLength()
        {
            return pts.Count;
        }
    }

    public class Chromosome
    {
        //Group of genes
        public List<Gene> Genome = new List<Gene>(); 
        
        //Fitness method
        public double GetTotalLength()
        {
            return Genome.Sum(c => c.GetLength());
        }

        private HashSet<Point3dEx> ptSet = new HashSet<Point3dEx>();
        public int GetDistinctPtLen()
        {
            if (ptSet.Count > 0) return ptSet.Count;
            foreach(var c in Genome)
            {
                ptSet = new HashSet<Point3dEx>(ptSet.Union(c.pts));
            }
            return ptSet.Count;
        }

        public void AddChromos(Gene c)
        {
            Genome.Add(c);
        }
    }

    public class GA
    {
        Random Rand = new Random();

        //Genetic Algorithm parameters
        int MaxTime;
        int PopulationSize;
        int SelectionSize = 6;
        int ChromoLen = 2;
        double CrossRate = 0.8;
        double MutationRate = 0.2;

        //Inputs
        Point3dEx StartPt;
        List<Point3dEx> EndPts = new List<Point3dEx>();
        List<Extents3d> Obstacles = new List<Extents3d>();

        //Range
        Point3dEx Low, High;

        private List<double> XPositionList = new List<double>();
        private List<double> YPositionList = new List<double>();
        private Dictionary<Point3dEx, Tuple<int, int>> ptExToIndexDic = new Dictionary<Point3dEx, Tuple<int, int>>();

        public GA(List<Point3d> pts, Point3d rangeLowPt, Point3d rangeHighPt, List<Extents3d> obstacles, int popSize = 10)
        {
            Rand = new Random(System.DateTime.Now.Millisecond);
            PopulationSize = popSize;
            MaxTime = 100;
            CrossRate = 0.8;
            MutationRate = 0.2;
            StartPt = new Point3dEx(pts.First());
            //pts.RemoveAt(0);
            pts.GetRange(1,pts.Count-1).ForEach(p => EndPts.Add(new Point3dEx(p)));
            
            Obstacles = obstacles.Select(e=>e.Expand(1.2)).ToList();

            //init coordinates
            var obstaclesPts = new List<Point3d>();
            var minPts = obstacles.Select(e=>e.MinPoint).ToList();
            var maxPts = obstacles.Select(e=>e.MaxPoint).ToList();
            obstaclesPts.AddRange(minPts);
            obstaclesPts.AddRange(maxPts);
            
            var orderedXPositionList = pts.Select(pt => pt.X).Distinct().ToList();
            orderedXPositionList.AddRange(obstaclesPts.Select(pt => pt.X).Distinct());

            orderedXPositionList.Sort();
            XPositionList.AddRange(orderedXPositionList);
            
            var orderedYPositionList = pts.Select(pt => pt.Y).Distinct().ToList();
            orderedXPositionList = pts.Select(pt => pt.Y).Distinct().ToList();
            orderedYPositionList.Sort();
            YPositionList.AddRange(orderedYPositionList);
            
            for(int i = 0; i < XPositionList.Count; i++)
            {
                for(int j = 0; j < YPositionList.Count; j++)
                {
                    var pt = new Point3d(XPositionList[i], YPositionList[j], 0);
                    NoDraw.Circle(pt, 0.8).AddToCurrentSpace();
                    var ptEx = new Point3dEx(XPositionList[i], YPositionList[j],0);
                    ptExToIndexDic.Add(ptEx, Tuple.Create(i, j));
                }
            }

            Low = new Point3dEx(rangeLowPt);
            High = new Point3dEx(rangeHighPt);
        }
         
        public List<Chromosome> Run()
        {
            List<Chromosome> selected = new List<Chromosome>();

            var pop = CreateFirstPopulation();

            Active.Editor.WriteMessage($"init pop cnt {pop.Count}");
            var cnt = 200;

            while (cnt-- > 0)
            {
                //Active.Editor.WriteMessage($"iteration cnt： {cnt}");
                selected = Selection(pop);
                pop = CreateNextGeneration(selected);
                //Mutation(pop);
            }

            return selected;
        }

        public void Mutation(List<Chromosome> s)
        {
            var cnt = (int)s.Count * MutationRate;
            for(int i = 0; i < cnt; ++i )
            {
                int index = Rand.Next(s.Count);
                var targetSolution = s[index];
                for(int j = 0; j < ChromoLen; ++j)
                {
                    var rn = Rand.Next(2);
                    if(rn == 0)
                    {
                        var chrom = new Gene();
                        chrom.pts.Add(StartPt);
                        while (true)
                        {
                            int rd = Rand.Next(3);
                            var lastPt = chrom.pts.Last();

                            if (lastPt.Equals(EndPts[j]))
                            {
                                break;
                            }

                            if (rd == 0)
                            {
                                var newPt = new Point3dEx(lastPt.X,lastPt.Y + 1,0);
                                if (IsPtInRange(newPt))
                                    chrom.pts.Add(newPt);
                            }
                            else if (rd == 1)
                            {
                                var newPt = new Point3dEx(lastPt.X+1, lastPt.Y + 1, 0);
                                if (IsPtInRange(newPt))
                                    chrom.pts.Add(newPt);
                            }
                            else
                            {
                                var newPt = new Point3dEx(lastPt.X-1, lastPt.Y + 1, 0);
                                if (IsPtInRange(newPt))
                                    chrom.pts.Add(newPt);
                            }
                        }

                        targetSolution.Genome[j] = chrom;
                    }
                }
            }
        }

        private bool IsPtInRange(Point3dEx pt)
        {
            return true;
            //return pt.X > Low.X && pt.X < High.X && pt.Y > Low.Y && pt.Y < High.X;
        }
        private List<Point3dEx> GetPtAdjs(Point3dEx pt, int dirPriority)
        {
            var pts = new List<Point3dEx>();
            var step = 0.1;
            var leftPt = new Point3dEx(pt.X - step, pt.Y, pt.Z);
            var downPt = new Point3dEx(pt.X, pt.Y - step, pt.Z);
            var rightPt = new Point3dEx(pt.X + step, pt.Y, pt.Z);
            var topPt = new Point3dEx(pt.X, pt.Y + step, pt.Z);
            if (dirPriority == 0) // left down first
            {
                pts.Add(leftPt);
                pts.Add(downPt);
            }
            else if(dirPriority == 1) //low left first
            {
                pts.Add(downPt);
                pts.Add(leftPt);
            }
            else if (dirPriority == 2) //right  first
            {
                pts.Add(rightPt);
                pts.Add(downPt);
            }
            else if (dirPriority == 3) //top first
            {
                pts.Add(downPt);
                pts.Add(rightPt);
            }
            return pts;
        }

        private List<Point3dEx> GetPtAdjs2(Point3dEx pt, int  firstDir)
        {
            var pts = new List<Point3dEx>();
            var step = 1;
            var leftPt = new Point3dEx(pt.X - step, pt.Y, pt.Z);
            var downPt = new Point3dEx(pt.X, pt.Y - step, pt.Z);
            var rightPt = new Point3dEx(pt.X + step, pt.Y, pt.Z);
            var topPt = new Point3dEx(pt.X, pt.Y + step, pt.Z);

            switch (firstDir)
            {
                case 0:
                    pts.Add(leftPt);
                    pts.Add(downPt);
                    pts.Add(rightPt);
                    pts.Add(topPt);
                    break;
                case 1:
                    pts.Add(downPt);
                    pts.Add(rightPt);
                    pts.Add(topPt);
                    pts.Add(leftPt);
                    break;
                case 2:
                    pts.Add(rightPt);
                    pts.Add(topPt);
                    pts.Add(leftPt);
                    pts.Add(downPt);
                    break;
                case 3:
                    pts.Add(topPt);
                    pts.Add(leftPt);
                    pts.Add(downPt);
                    pts.Add(rightPt);

                    break;
                default:
                    break;
            }

            return pts;
        }

        private List<Point3dEx> GetPtAdjs3(Point3dEx pt, int firstDir)
        {
            var pts = new List<Point3dEx>();
            var step = 1;

            var ijTuple = ptExToIndexDic[pt];
            var i = ijTuple.Item1;
            var j = ijTuple.Item2;

            Point3dEx leftPt = null;
            Point3dEx downPt = null;
            Point3dEx rightPt = null;
            Point3dEx topPt = null;
            if(i - 1 >=0) 
                leftPt = new Point3dEx(XPositionList[i-1], YPositionList[j], 0);

            if(j+1 < YPositionList.Count)
                downPt = new Point3dEx(XPositionList[i], YPositionList[j+1], 0);

            if(i+1 < XPositionList.Count)
                rightPt = new Point3dEx(XPositionList[i+1], YPositionList[j], 0);

            if(j-1 >=0)
                topPt = new Point3dEx(XPositionList[i], YPositionList[j-1], 0);

            switch (firstDir)
            {
                case 0:
                    if(leftPt != null)
                        pts.Add(leftPt);
                    if(downPt != null)
                        pts.Add(downPt);
                    if (rightPt != null)
                        pts.Add(rightPt);
                    if(topPt != null)
                        pts.Add(topPt);
                    break;
                case 1:
                    if(downPt != null)
                        pts.Add(downPt);
                    if(rightPt != null)
                        pts.Add(rightPt);
                    if(topPt != null)
                        pts.Add(topPt);
                    if(leftPt != null)
                        pts.Add(leftPt);
                    break;
                case 2:
                    if(rightPt != null)
                        pts.Add(rightPt);
                    if(topPt!= null)
                        pts.Add(topPt);
                    if(leftPt!= null)
                        pts.Add(leftPt);
                    if(downPt!= null)
                        pts.Add(downPt);
                    break;
                case 3:
                    if(topPt != null)
                        pts.Add(topPt);
                    if(leftPt != null)
                        pts.Add(leftPt);
                    if(downPt !=null)
                        pts.Add(downPt);
                    if(rightPt!= null)
                        pts.Add(rightPt);
                    break;
                default:
                    break;
            }

            return pts;
        }

        Dictionary<GeneKey,Gene> GlobalChromdic = new Dictionary<GeneKey, Gene>();
        public Gene FindAChromByBfs(Point3dEx startPt, Point3dEx endPt, int dir = 0)
        {
            var startTime = DateTime.Now;
            var chrom = new Gene();
            chrom.Dir = dir;
            chrom.StartPt = startPt;
            chrom.EndPt = endPt;

            var chromKey = new GeneKey();
            chromKey.Dir = chrom.Dir;
            chromKey.StartPt = chrom.StartPt;
            chromKey.EndPt = chrom.EndPt;

            if(GlobalChromdic.ContainsKey(chromKey))
            {
                return GlobalChromdic[chromKey];
            }

            Queue<Point3dEx> Q = new Queue<Point3dEx>();
            Q.Enqueue(startPt);
            Dictionary<Point3dEx, bool > visited = new Dictionary<Point3dEx, bool>();
            Dictionary<Point3dEx, Point3dEx> preDic = new Dictionary<Point3dEx, Point3dEx>();
            visited[startPt] = true;
            int maxCnt = 1000000;
            bool found = false;
            int cnt = 0;
            while(maxCnt -- > 0)
            {
                var pt = Q.Dequeue();
                if (pt.Equals(endPt))
                {
                    Active.Editor.WriteMessageWithReturn($"found node: {cnt}");
                    found = true;
                    break;
                }
                var adjs = GetPtAdjs(pt, dir);
                foreach(var adj in adjs)
                {
                    cnt++;
                    if (!visited.ContainsKey(adj) /*|| !visited[adj]*/)
                    {
                        visited.Add(adj,true);
                        preDic.Add(adj, pt);
                        Q.Enqueue(adj);
                    }
                }
            }

            if (found)
            {
                chrom.pts.Add(endPt);
                var curPt = preDic[endPt];
                while (preDic.ContainsKey(curPt))
                {
                    curPt = preDic[curPt];
                    chrom.pts.Add(curPt);
                }

                chrom.pts.Add(startPt);

                GlobalChromdic.Add(chromKey, chrom);
            }
            var endTime = DateTime.Now;
            var span = endTime - startTime;
            Active.Editor.WriteMessageWithReturn($"Chrom Seconds: {span.TotalSeconds}");
            return chrom;
        }

        private bool IsPtInObstacles(Point3d pt)
        {
            foreach(var o in Obstacles)
            {
                if (o.IsPointIn(pt))
                    return true;
            }
            return false;
        }

        public List<Gene> FindGenomeByBFS(Point3dEx startPt, List<Point3dEx> endPts, int firstDir)
        {
            var localChromosDic = new Dictionary<GeneKey,Gene>();
            var startTime = DateTime.Now;

            var tmpEnds = new HashSet<Point3dEx>(endPts);

            Queue<Point3dEx> Q = new Queue<Point3dEx>();
            Q.Enqueue(startPt);
            Dictionary<Point3dEx, bool> visited = new Dictionary<Point3dEx, bool>();
            Dictionary<Point3dEx, Point3dEx> preDic = new Dictionary<Point3dEx, Point3dEx>();
            visited[startPt] = true;

            int cnt = 0;
            while (tmpEnds.Count > 0)
            {
                var pt = Q.Dequeue();
                if (tmpEnds.Contains(pt))
                {
                    tmpEnds.Remove(pt);

                    var chrome = new Gene();
                    chrome.StartPt = startPt;
                    chrome.EndPt = pt;
                    chrome.FirstDir = firstDir;
                    var chromKey = chrome.Key;

                    if (GlobalChromdic.ContainsKey(chromKey))
                    {
                        localChromosDic.Add(chromKey,GlobalChromdic[chromKey]);
                    }
                    else
                    {
                        chrome.pts.Add(pt);
                        var curPt = preDic[pt];
                        while (preDic.ContainsKey(curPt))
                        {
                            chrome.pts.Add(curPt);
                            curPt = preDic[curPt];
                        }

                        chrome.pts.Add(startPt);

                        if (!GlobalChromdic.ContainsKey(chromKey))
                            GlobalChromdic.Add(chromKey, chrome);
                        localChromosDic.Add(chromKey,chrome);
                    }
                    Active.Editor.WriteMessageWithReturn($"found node: {cnt}");
                }
                var adjs = GetPtAdjs3(pt, firstDir);
                foreach (var adj in adjs)
                {
                    if (IsPtInObstacles(new Point3d(adj.X,adj.Y ,adj.Z)))
                        continue;
                    cnt++;
                    if (!visited.ContainsKey(adj) /*|| !visited[adj]*/)
                    {
                        visited.Add(adj, true);
                        preDic.Add(adj, pt);
                        Q.Enqueue(adj);
                    }
                }
            }

            var endTime = DateTime.Now;
            var span = endTime - startTime;
            Active.Editor.WriteMessageWithReturn($"Chrom Seconds: {span.TotalSeconds}");

            var chromos = new List<Gene>();
            foreach(var e in endPts)
            {
                var chromKey = Gene.GetGeneKey(startPt, e, firstDir, 0);

                if(localChromosDic.ContainsKey(chromKey))
                {
                    chromos.Add(localChromosDic[chromKey]);
                }
            }
            return chromos;
        }

        private int RandInt(int range)
        {
            var guid = Guid.NewGuid();
            var rand = new Random(guid.GetHashCode());
            int i = rand.Next(range);
            return i;
        }

        public List<Chromosome> CreateFirstPopulation()
        {
            List<Chromosome> solutions = new List<Chromosome>();

            for(int i = 0; i < PopulationSize; ++i)
            {
                var solution = new Chromosome();
                var firstDir = RandInt(4);
                var genome = FindGenomeByBFS(StartPt, EndPts, firstDir);
                solution.Genome = genome;
                solutions.Add(solution);
            }

            return solutions;
        }

        public List<Chromosome> CreateNextGeneration(List<Chromosome> solutions)
        {
            List<Chromosome> rst = new List<Chromosome>();

            for(int i = 0; i < PopulationSize; ++i)
            {
                int rd1 = RandInt (solutions.Count);
                int rd2 = RandInt (solutions.Count);
                var s = Crossover(solutions[rd1], solutions[rd2]);
                rst.Add(s);
            }

            return rst;
        }

        public Chromosome Crossover(Chromosome s1, Chromosome s2)
        {
            Chromosome newS = new Chromosome();
            var chromoLen = s1.Genome.Count;
            int[] covering_code = new int[chromoLen];
            for (int i = 0; i < chromoLen; ++i)
            {
                var cc = RandInt(2) ;//rand.Next(0, 2);
                if (cc == 0)
                {
                    newS.AddChromos(s1.Genome[i]);
                }
                else
                {
                    newS.AddChromos(s2.Genome[i]);
                }
            }

            return newS;
        }

        public List<Chromosome> Selection(List<Chromosome> inputSolution)
        {
            var sorted = inputSolution.OrderBy(s => s.GetDistinctPtLen()).ToList();
            var rst = new List<Chromosome>();
            for(int i = 0; i < SelectionSize; ++i)
            {
                rst.Add(sorted[i]);
            }
            return rst;
        }
    }
}
