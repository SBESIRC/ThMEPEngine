using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Algorithm.ClusteringAlgorithm;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightConnect
{
    /// <summary>
    /// 根据灯的块，通过密度聚类的方式进行预分组
    /// </summary>
    class LightBeforehandGroup
    {
        List<LightBlockReference> _lightBlockReferences;
        List<Line> _notCrossLines;
        public LightBeforehandGroup(List<LightBlockReference> lightBlocks,List<Line> notCrossLines) 
        {
            _lightBlockReferences = new List<LightBlockReference>();
            _notCrossLines = new List<Line>();
            if (null != lightBlocks && lightBlocks.Count > 0) 
            {
                foreach (var item in lightBlocks) 
                {
                    if (item == null)
                        continue;
                    _lightBlockReferences.Add(item);
                }    
            }
            if (null != notCrossLines && notCrossLines.Count > 0) 
            {
                foreach (var item in notCrossLines) 
                {
                    if (null == item || item.Length < 10)
                        continue;
                    _notCrossLines.Add(item);
                }
            }
        }
        public List<List<LightBlockReference>> GetPreGroupBlocks(double eps = 12000.0,int minPts = 1,int maxCount=25) 
        {
            var retGroups = new List<List<LightBlockReference>>();
            if (null == _lightBlockReferences || _lightBlockReferences.Count < 1)
                return retGroups;
            var clusters = GetPreGroupPoints(eps, minPts, maxCount);
            if (clusters == null || clusters.Count < 1)
                return retGroups;
            foreach (var group in clusters) 
            {
                if (null == group || group.Count < 1)
                    continue;
                var blockReferences = new List<LightBlockReference>();
                foreach (var item in group) 
                {
                    foreach (var block in _lightBlockReferences) 
                    {
                        if (block.LightPosition2d.DistanceTo(item) < 1)
                            blockReferences.Add(block);
                    }
                }
                if (blockReferences.Count < 1)
                    continue;
                retGroups.Add(blockReferences);
            }
            return retGroups;
        }
        public List<List<Point3d>> GetPreGroupPoints(double eps = 12000.0, int minPts = 1, int maxCount = 25) 
        {
            var retGroups = new List<List<Point3d>>();
            if (null == _lightBlockReferences || _lightBlockReferences.Count < 1)
                return retGroups;
            var pointDBSacn = new PointDBScan(_lightBlockReferences.Select(c => c.LightPosition2d).ToList(), _notCrossLines);
            var clusters = pointDBSacn.ClusterResult(eps, minPts, maxCount);
            if (clusters == null || clusters.Count < 1)
                return retGroups;
            foreach (var group in clusters)
            {
                if (null == group || group.Count < 1)
                    continue;
                retGroups.Add(group);
            }
            return retGroups;
        }
    }
}
