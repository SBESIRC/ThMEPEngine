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
    }

    public class Chromokey : IEquatable<Chromokey>
    {
        public int Dir = 0;
        public Point3dEx StartPt;
        public Point3dEx EndPt;
        public int FirstDir = 0;
        public override int GetHashCode()
        {
            return StartPt.GetHashCode() ^ EndPt.GetHashCode() ^ Dir.GetHashCode() ^ FirstDir.GetHashCode();
        }
        public bool Equals(Chromokey other)
        {
            return this.Dir.Equals(other.Dir)
                && this.StartPt.Equals(other.StartPt)
                && this.EndPt.Equals(other.EndPt)
                && this.FirstDir.Equals(other.FirstDir);
        }


    }

    public class Chromosome : IEquatable<Chromosome>
    {
        public int Dir = 0;
        public Point3dEx StartPt;
        public Point3dEx EndPt;
        public int FirstDir = 0;
        public List<Point3dEx> pts = new List<Point3dEx>();

        public Chromokey Key 
        { 
            get
            {
                return GetChromoKey(this.StartPt, this.EndPt, this.FirstDir, this.Dir);
            }
        }

        static public Chromokey GetChromoKey(Point3dEx startPt, Point3dEx endPt, int firstDir, int dir)
        {
            var key = new Chromokey();
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
        public bool Equals(Chromosome other)
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

    public class Solution
    {
        public List<Chromosome> chromos = new List<Chromosome>();
        public double GetTotalLength()
        {
            return chromos.Sum(c => c.GetLength());
        }

        private HashSet<Point3dEx> ptSet = new HashSet<Point3dEx>();
        public int GetDistinctPtLen()
        {
            if (ptSet.Count > 0) return ptSet.Count;
            foreach(var c in chromos)
            {
                ptSet = new HashSet<Point3dEx>(ptSet.Union(c.pts));
            }
            return ptSet.Count;
        }

        //public Solution CrossOver(Solution other, Random rand)
        //{
        //    Solution newSolution = new Solution();
        //    for(int i = 0; i < chrom)
        //}

        public void AddChromos(Chromosome c)
        {
            chromos.Add(c);
        }
    }

    public class GA
    {
        Random rand = new Random();
        int MaxTime;
        int popsize;
        int selectionSize = 6;
        int chromoLen = 2;
        double crossRate = 0.8;
        double mutateRate = 0.2;

        Point3dEx startPt;
        List<Point3dEx> endPts = new List<Point3dEx>();
        List<Extents3d> Obstacles = new List<Extents3d>();

        Point3dEx low, high;
        public GA(List<Point3d> pts, Point3d rangeLowPt, Point3d rangeHighPt, List<Extents3d> obstacles, int popSize = 10)
        {
            //Guid.NewGuid().GetHashCode
            rand = new Random(System.DateTime.Now.Millisecond);
            popsize = popSize;
            MaxTime = 100;
            crossRate = 0.8;
            mutateRate = 0.2;
            startPt = new Point3dEx( pts.First());
            pts.RemoveAt(0);
            pts.ForEach(p => endPts.Add(new Point3dEx(p)));
            Obstacles = obstacles;

            low = new Point3dEx(rangeLowPt);
            high = new Point3dEx(rangeHighPt);
        }
         
        public List<Solution> Run()
        {
             List<Solution> selected = new List<Solution>();

            //var s = new Solution();
            //var chrom = FindAChromByBfs(startPt, endPts[0]);
            //var chrom1 = FindAChromByBfs(startPt, endPts[0], 1);
            //s.AddChromos(chrom);
            //s.AddChromos(chrom1);
            //selected.Add(s);

            //var chrom3 = FindAChromByBfs(startPt, endPts[1],2);
            //var chrom4 = FindAChromByBfs(startPt, endPts[1], 3);

            //var s2 = new Solution();
            //s2.AddChromos(chrom3);
            //s2.AddChromos(chrom4);
            //selected.Add(s2);

            //return selected;

            var pop = CreateFirstPop3();

            //return pop;

            Active.Editor.WriteMessage($"init pop cnt {pop.Count}");
            var cnt = 200;

            while (cnt-- > 0)
            {
                //Active.Editor.WriteMessage($"iteration cnt {cnt}");
                selected = Selection(pop);
                pop = CreateNextGeneration(selected);
                //Mutation(pop);
            }

            return selected;


        }

        public void Mutation(List<Solution> s)
        {
            var cnt = (int)s.Count * mutateRate;
            for(int i = 0; i < cnt; ++i )
            {
                int index = rand.Next(s.Count);
                var targetSolution = s[index];
                for(int j = 0; j < chromoLen; ++j)
                {
                    var rn = rand.Next(2);
                    if(rn == 0)
                    {
                        var chrom = new Chromosome();
                        chrom.pts.Add(startPt);
                        while (true)
                        {
                            int rd = rand.Next(3);
                            var lastPt = chrom.pts.Last();

                            if (lastPt.Equals(endPts[j]))
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

                        targetSolution.chromos[j] = chrom;
                    }
                }
            }
        }

        private bool IsPtInRange(Point3dEx pt)
        {
            return pt.X > low.X && pt.X < high.X && pt.Y > low.Y && pt.Y < high.X;
        }
        private List<Point3dEx> GetPtAdjs(Point3dEx pt, int dirPriority)
        {
            var pts = new List<Point3dEx>();
            var step = 1;
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


        Dictionary<Chromokey,Chromosome> GlobalChromdic = new Dictionary<Chromokey, Chromosome>();
        public Chromosome FindAChromByBfs(Point3dEx startPt, Point3dEx endPt, int dir = 0)
        {
            var startTime = DateTime.Now;
            var chrom = new Chromosome();
            chrom.Dir = dir;
            chrom.StartPt = startPt;
            chrom.EndPt = endPt;

            var chromKey = new Chromokey();
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

        public List<Chromosome> FindChromsByBFS(Point3dEx startPt, List<Point3dEx> endPts, int firstDir)
        {
            var localChromosDic = new Dictionary<Chromokey,Chromosome>();
            var startTime = DateTime.Now;

            var tmpEnds = new HashSet<Point3dEx>(endPts);

            Queue<Point3dEx> Q = new Queue<Point3dEx>();
            Q.Enqueue(startPt);
            Dictionary<Point3dEx, bool> visited = new Dictionary<Point3dEx, bool>();
            Dictionary<Point3dEx, Point3dEx> preDic = new Dictionary<Point3dEx, Point3dEx>();
            visited[startPt] = true;
            //int maxCnt = 1000000;
            // bool found = false;
            int cnt = 0;
            while (tmpEnds.Count > 0)
            {
                var pt = Q.Dequeue();
                if (tmpEnds.Contains(pt))
                {
                    tmpEnds.Remove(pt);

                    var chrome = new Chromosome();
                    //rst.Dir = dir;
                    chrome.StartPt = startPt;
                    chrome.EndPt = pt;
                    chrome.FirstDir = firstDir;
                    var chromKey = chrome.Key;
                    //var chromKey = new Chromokey();
                    //chromKey.Dir = chrome.Dir;
                    //chromKey.ClockWise = chrome.ClockWise;
                    //chromKey.StartPt = chrome.StartPt;
                    //chromKey.EndPt = chrome.EndPt;

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
                            curPt = preDic[curPt];
                            chrome.pts.Add(curPt);
                        }

                        chrome.pts.Add(startPt);

                        if (!GlobalChromdic.ContainsKey(chromKey))
                            GlobalChromdic.Add(chromKey, chrome);
                        localChromosDic.Add(chromKey,chrome);
                    }
                    Active.Editor.WriteMessageWithReturn($"found node: {cnt}");
                    //break;
                }
                var adjs = GetPtAdjs2(pt, firstDir);
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

            var chromos = new List<Chromosome>();
            foreach(var e in endPts)
            {
                //var chromKey = new Chromokey();
                ////chromKey.Dir = Dir;
                //chromKey.ClockWise = clockwise;
                //chromKey.StartPt = startPt;
                //chromKey.EndPt = e;
                var chromKey = Chromosome.GetChromoKey(startPt, e, firstDir, 0);

                if(localChromosDic.ContainsKey(chromKey))
                {
                    chromos.Add(localChromosDic[chromKey]);
                }
            }
            return chromos;
        }

        //public List< Chromosome> FindAChromsByBfs(Point3dEx startPt, Point3dEx endPt, int dir1, int dir2)
        //{
        //    var startTime = DateTime.Now;
        //    var rst = new Chromosome();

        //    var QsDic = new Dictionary<int, Queue<Point3dEx>>();
        //    var q1 = new Queue<Point3dEx>();
        //    q1.Enqueue(startPt);
        //    var q2 = new Queue<Point3dEx>();
        //    q2.Enqueue(startPt);

        //    var visitedDic = new Dictionary<int, Dictionary<Point3dEx, bool>>();
        //    var ptsPreDic = new Dictionary<int, Dictionary<Point3dEx, Point3dEx>>();
        //    var visited1 = new Dictionary<Point3dEx, bool>();
        //    var visited2 = new Dictionary<Point3dEx, bool>();
        //    visited1[startPt] = true;
        //    visited2[startPt] = true;
        //    visitedDic.Add(1, visited1);
        //    visitedDic.Add(2, visited2);

        //    int maxCnt = 1000000;
        //    bool found = false;
        //    int cnt = 0;
        //    while (maxCnt-- > 0)
        //    {
        //        var pt = Q.Dequeue();
        //        if (pt.Equals(endPt))
        //        {
        //            Active.Editor.WriteMessage($"found: {cnt}");
        //            found = true;
        //            break;
        //        }
        //        var adjs1 = GetPtAdjs(pt, dir1);
        //        var adjs2 = GetPtAdjs(pt, dir2);
        //        foreach (var adj in adjs1)
        //        {
        //            if (!visited.ContainsKey(adj) /*|| !visited[adj]*/)
        //            {
        //                visited.Add(adj, true);
        //                preDic.Add(adj, pt);
        //                Q.Enqueue(adj);
        //                cnt++;
        //            }
        //        }
        //    }

        //    if (found)
        //    {
        //        rst.pts.Add(endPt);
        //        var curPt = preDic[endPt];
        //        while (preDic.ContainsKey(curPt))
        //        {
        //            curPt = preDic[curPt];
        //            rst.pts.Add(curPt);
        //        }

        //        rst.pts.Add(startPt);
        //    }
        //    var endTime = DateTime.Now;
        //    var span = endTime - startTime;
        //    Active.Editor.WriteMessage($"Seconds: { span.TotalSeconds}");
        //    return rst;
        //}


        //public Dictionary<Point3dEx, Chromosome> FindChromsByBfs(Point3dEx startPt, List<Point3dEx> endPts, int dir = 0)
        //{
        //    var startTime = DateTime.Now;

        //    var chromosDic = new Dictionary<Point3dEx, Chromosome>();

        //    Dictionary<Point3dEx, Queue<Point3dEx>> QsDic = new Dictionary<Point3dEx, Queue<Point3dEx>>();
        //    foreach(var pt in endPts)
        //    {
        //        var q = new Queue<Point3dEx>();
        //        q.Enqueue(startPt);
        //        QsDic.Add(pt, q);
        //    }

        //    Dictionary<Point3dEx, bool> visited = new Dictionary<Point3dEx, bool>();
        //    Dictionary<Point3dEx, Point3dEx> preDic = new Dictionary<Point3dEx, Point3dEx>();
        //    visited[startPt] = true;
        //    int maxCnt = 1000000;
        //    bool found = false;
        //    int cnt = 0;
        //    while (maxCnt-- > 0)
        //    {
        //        var pt = Q.Dequeue();
        //        if (endPts.Contains(pt))
        //        {

        //            Active.Editor.WriteMessage($"found: {cnt}");
        //            found = true;
        //            break;
        //        }
        //        var adjs = GetPtAdjs(pt, dir);
        //        foreach (var adj in adjs)
        //        {
        //            if (!visited.ContainsKey(adj) /*|| !visited[adj]*/)
        //            {
        //                visited.Add(adj, true);
        //                preDic.Add(adj, pt);
        //                Q.Enqueue(adj);
        //                cnt++;
        //            }
        //        }
        //    }

        //    if (found)
        //    {
        //        foreach (var endPt in endPts)
        //        {
        //            var oneChrom = new Chromosome();
        //            oneChrom.pts.Add(endPt);
        //            var curPt = preDic[endPt];
        //            while (preDic.ContainsKey(curPt))
        //            {
        //                curPt = preDic[curPt];
        //                oneChrom.pts.Add(curPt);
        //            }

        //            oneChrom.pts.Add(startPt);

        //            chromosDic.Add(endPt, oneChrom);
        //        }
        //    }
        //    var endTime = DateTime.Now;
        //    var span = endTime - startTime;
        //    Active.Editor.WriteMessage($"Seconds: { span.TotalSeconds}");
        //    return chromosDic;
        //}

        private int RandInt(int range)
        {
            var guid = Guid.NewGuid();
            var rand = new Random(guid.GetHashCode());
            int i = rand.Next(range);
            return i;
        }

        public List<Solution> CreateFirstPop3()
        {
            List<Solution> solutions = new List<Solution>();

            for(int i = 0; i < popsize; ++i)
            {
                var s = new Solution();
                var firstDir = RandInt(4);
                //bool clockwise = tmp == 0 ? true : false;
                var chroms = FindChromsByBFS(startPt, endPts, firstDir);
                s.chromos = chroms;
                solutions.Add(s);
            }

            return solutions;
        }

        public List<Solution> CreateFirstPop2()
        {
            List<Solution> solutions = new List<Solution>();
            for(int i = 0; i < popsize; ++i)
            {
                var s = new Solution();
                int j = 0;
                foreach(var endPt in endPts)
                {
                    var rd = rand.Next(2);
                    if (j == 0)
                    {
                        var chrom = FindAChromByBfs(startPt, endPts[0], rd);
                        s.AddChromos(chrom);
                    }
                    else if (j == 1)
                    {
                        var chrom = FindAChromByBfs(startPt, endPts[1], rd + 2);
                        s.AddChromos(chrom);
                    }
                    else
                    {
                        //var chrom = FindAChromByBfs(startPt, endPts[1], rd + 2);
                        //s.AddChromos(chrom);
                    }
                    ++j;
                    //var rd = rand.Next(4);
                    //var chrom = FindAChromByBfs(startPt, endPt, rd);
                    //s.AddChromos(chrom);
                }
                solutions.Add(s);
            }
            return solutions;
        }

        public List<Solution> CreateFirstPop()
        {
            List<Solution> solutions = new List<Solution>();
            for(int i = 0; i < popsize; ++i)
            {
                var solution = new Solution();
                var visited = new HashSet<Point3dEx>();
                for (int j = 0; j < chromoLen; ++j)
                {
                    var chrom = new Chromosome();
                    chrom.pts.Add(startPt);
                    visited.Add(startPt);
                    while (true)
                    {
                        int rd = rand.Next(3);
                        var lastPt = chrom.pts.Last();

                        if (lastPt.Equals(endPts[j]))
                        {
                            break;
                        }

                        if (rd == 0)
                        {
                            var newPt = new Point3dEx(lastPt.X, lastPt.Y - 1, 0);
                            if (IsPtInRange(newPt) && !visited.Contains(newPt))
                            {
                                chrom.pts.Add(newPt);
                                visited.Add(newPt);
                            }
                        }
                        else if (rd == 1)
                        {
                            var newPt = new Point3dEx(lastPt.X + 1, lastPt.Y, 0);
                            if (IsPtInRange(newPt) && !visited.Contains(newPt))
                            {
                                chrom.pts.Add(newPt);
                                visited.Add(newPt);
                            }
                        }
                        else
                        {
                            var newPt = new Point3dEx(lastPt.X - 1, lastPt.Y, 0);
                            if (IsPtInRange(newPt) && !visited.Contains(newPt))
                            {
                                chrom.pts.Add(newPt);
                                visited.Add(newPt);
                            }
                        }
                    }
                    solution.AddChromos(chrom);
                }
                solutions.Add(solution);
            }
            return solutions;
        }

        public List<Solution> CreateNextGeneration(List<Solution> solutions)
        {
            //var popSize = popsize;

            List<Solution> rst = new List<Solution>();

            for(int i = 0; i < popsize; ++i)
            {
                int rd1 = RandInt (solutions.Count);
                int rd2 = RandInt (solutions.Count);
                var s = Crossover(solutions[rd1], solutions[rd2]);
                rst.Add(s);
            }

            //for(int i = 0; i < solutions.Count - 1; i ++)
            //{
            //    var s = Crossover(solutions[i], solutions[i + 1]);
            //    rst.Add(s);
            //}
            //int index  = rand.Next(0, solutions.Count);
            //rst.Add(solutions[index]);

            //todo : mutation
            return rst;
        }

        public Solution Crossover(Solution s1, Solution s2)
        {
            Solution newS = new Solution();
            var chromoLen = s1.chromos.Count;
            int[] covering_code = new int[chromoLen];
            for (int i = 0; i < chromoLen; ++i)
            {
                var cc = RandInt(2) ;//rand.Next(0, 2);
                if (cc == 0)
                {
                    newS.AddChromos(s1.chromos[i]);
                }
                else
                {
                    newS.AddChromos(s2.chromos[i]);
                }
            }
            //for (int i )

            return newS;
        }

        public List<Solution> Selection(List<Solution> inputSolution)
        {
            var sorted = inputSolution.OrderBy(s => s.GetDistinctPtLen()).ToList();
            var rst = new List<Solution>();
            for(int i = 0; i < selectionSize; ++i)
            {
                rst.Add(sorted[i]);
            }
            return rst;
        }
    }
}
