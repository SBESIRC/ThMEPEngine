using System.Collections.Generic;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public static class PtSet
    {
        public static void AddVisit(this HashSet<Point3dEx> visited, List<List<Point3dEx>> PathList)
        {
            foreach (var ptls in PathList)
            {
                foreach (var pt in ptls)
                {
                    visited.Add(pt);
                }
            }
        }
    }
}
