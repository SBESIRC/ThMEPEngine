using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LineCleaner;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OTools;
using ThParkingStall.Core.Tools;

namespace ThMEPArchitecture.FireZone
{
    public class CarWallLineCreator
    {
        private List<InfoCar> Cars;
        public List<LineSegment> WallLines;
        public CarWallLineCreator(List<InfoCar> cars)
        {
            Cars = cars;
        }
        public List<LineSegment> GetFireWalls()
        {
            var sideLines = new List<LineSegment>();
            var tailLines = new List<LineSegment>();
            var fireLines = new List<LineSegment>();
            foreach(var car in Cars)
            {
                var linesegs = car.Polyline.Shell.ToLineSegments();
                sideLines.Add(linesegs[1]);
                tailLines.Add(linesegs[2]);
                sideLines.Add(linesegs[3]);
            }
            var cleaner = new LineService(sideLines);
            var idxs_group = cleaner.GroupByParalle(sideLines);
            //对于一个group个数》=2：如果存在两根线互相投影长度较短则丢弃(>1m),否则保留
            foreach(var group in idxs_group)
            {
                bool keepgroup = true;
                if(group.Count() >= 2)
                {
                    for (int i = 0; i < group.Count()-1; i++)
                    {
                        var l1 = sideLines[group[i]];
                        for(int j = i+1;j < group.Count(); j++)
                        {
                            var l2 = sideLines[group[j]];
                            var projection = l1.Project(l2);
                            if(projection != null && projection.Length > 1000)
                            {
                                keepgroup = false;
                                break;
                            }
                        }
                        if (!keepgroup) break;
                    }
                }
                if (keepgroup) fireLines.AddRange(sideLines.Slice(group));
            }
            fireLines.AddRange(tailLines);
            cleaner = new LineService(fireLines,1000);
            fireLines = cleaner.MergeParalle(fireLines);
            fireLines = ExtendToOthers(fireLines, 500);
            //fireLines = cleaner.Clean();
            return fireLines;
        }
        public List<LineSegment> ExtendToOthers(List<LineSegment> fireLines,double distance)
        {
            var extended = fireLines.Select(l =>l.OExtend(distance)).ToList();
            var engine = new STRtree<int>();
            for(int i = 0; i < extended.Count; i++)
            {
                engine.Insert(new Envelope(extended[i].P0, extended[i].P1), i);
            }
            var results = new List<LineSegment>();
            for(int i = 0; i < fireLines.Count; i++)
            {
                var coors = new List<Coordinate> { fireLines[i].P0, fireLines[i].P1 };
                var extendedLine = extended[i];
                var envelop = new Envelope(extendedLine.P0, extendedLine.P1);
                var queried = engine.Query(envelop);
                queried.Remove(i);
                var intsetcions = extended.Slice(queried).Select(l => l.Intersection(extendedLine)).Where(c => c != null);
                coors.AddRange(intsetcions);
                coors = coors.PositiveOrder();
                results.Add(new LineSegment( coors.First(), coors.Last()));
            }
            return results;
        }

    }
}
