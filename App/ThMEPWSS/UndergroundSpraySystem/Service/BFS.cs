using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class BFS
    {
        public void BfsBranch(int i, Point3dEx pt, SprayIn sprayIn, SpraySystem spraySys)
        {
            var termPts = new List<Point3dEx>();
            var valvePts = new List<Point3dEx>();

            var q = new Queue<Point3dEx>();
            q.Enqueue(pt);
            var visited2 = new HashSet<Point3dEx>();
            while (q.Count > 0)
            {
                var curPt = q.Dequeue();
                if (sprayIn.PtTypeDic.ContainsKey(curPt))
                {
                    if (sprayIn.PtTypeDic[curPt].Contains("Valve"))
                    {
                        valvePts.Add(pt);
                    }
                }

                var adjs = sprayIn.PtDic[curPt];
                if (adjs.Count == 1)
                {
                    termPts.Add(curPt);
                    continue;
                }

                foreach (var adj in adjs)
                {
                    if (spraySys.BranchLoops[i].Contains(adj))
                        continue;

                    if (visited2.Contains(adj))
                    {
                        continue;
                    }

                    visited2.Add(adj);
                    q.Enqueue(adj);
                }
            }
            if (termPts.Count != 0)
            {
                if (spraySys.BranchDic.ContainsKey(pt))
                {
                    return;
                }
                spraySys.BranchDic.Add(pt, termPts);
                if (valvePts.Count != 0)
                {
                    ;//ValveDic.Add(pt, valvePts);
                }
            }

        }
    }
}
