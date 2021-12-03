using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.TowerSeparation.DBScan
{
    public class DBScanCluster
    {
        public Point3dCollection sampleSet { get; set; } = new Point3dCollection();
        public double epsilon { get; set; }
        public int MinPts { get; set; }
        private int clusterNum { get; set; }
        private Point3dCollection centerPoints = new Point3dCollection();
        private Point3dCollection notVisited = new Point3dCollection();
        private Dictionary<Point3d, Point3dCollection> neighbors = new Dictionary<Point3d, Point3dCollection>();
        private void FindCenterPoints()
        {
            FindNeighbor();
            foreach(Point3d pt in sampleSet)
            {
                if(neighbors.ContainsKey(pt) && neighbors[pt].Count >= MinPts)
                {
                    centerPoints.Add(pt);
                }
            }
        }

        private void FindNeighbor()
        {
            for(int i = 0; i< sampleSet.Count; ++i)
            {
                for(int j = i + 1; j < sampleSet.Count; ++j)
                {
                    Point3d currentPt1 = sampleSet[i];
                    Point3d currentPt2 = sampleSet[j];
                    if(currentPt1.DistanceTo(currentPt2) <= epsilon)
                    {
                        if (neighbors.ContainsKey(currentPt1))
                        {
                            neighbors[currentPt1].Add(currentPt2);
                        }
                        else
                        {
                            neighbors.Add(currentPt1, new Point3dCollection { currentPt2 });
                        }
                        if (neighbors.ContainsKey(currentPt2))
                        {
                            neighbors[currentPt2].Add(currentPt1);
                        }
                        else
                        {
                            neighbors.Add(currentPt2, new Point3dCollection { currentPt1 });
                        }
                    }
                }
            }
        }
        private void Initialize(Point3dCollection pointSet)
        {
            sampleSet = pointSet;
            FindCenterPoints();
            clusterNum = 0;
            foreach(Point3d pt in sampleSet)
            {
                notVisited.Add(pt);
            }
        }

        public List<Point3dCollection> getClusters(Point3dCollection pointSet, double Epsilon, int minPts)
        {
            epsilon = Epsilon;
            MinPts = minPts;
            List<Point3dCollection> Clusters = new List<Point3dCollection>();
            Initialize(pointSet);
            while(centerPoints.Count > 0)
            {
                Point3dCollection currentNotVisit = new Point3dCollection();
                foreach(Point3d pt in notVisited)
                {
                    currentNotVisit.Add(pt);
                }
                Point3d firstCorePt = centerPoints[0];
                List<Point3d> queue = new List<Point3d> { firstCorePt };
                notVisited.Remove(firstCorePt);
                while (queue.Count > 0)
                {
                    Point3d q = queue[0];
                    queue.RemoveAt(0);
                    if(neighbors[q].Count >= MinPts)
                    {
                        Point3dCollection temp = neighbors[q];
                        foreach(Point3d pt in temp)
                        {
                            if(notVisited.Contains(pt))
                            {
                                queue.Add(pt);
                                notVisited.Remove(pt);
                            }
                        }
                    }
                }
                Point3dCollection Ck = new Point3dCollection();
                foreach(Point3d pt in currentNotVisit)
                {
                    if (!notVisited.Contains(pt))
                    {
                        Ck.Add(pt);
                        if (centerPoints.Contains(pt))
                        {
                            centerPoints.Remove(pt);
                        }
                    }
                }
                if(Ck.Count > 0)
                {
                    ++clusterNum;
                    Clusters.Add(Ck);
                }
                
            }
            return Clusters;
        }

    }
}