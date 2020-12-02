using System;
using QuickGraph;
using QuickGraph.Algorithms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHAVC.Duct
{
    public class DraughtDesignParameters
    {
        //总风量
        public double TotalVolume { get; set; }
        //风口数量
        public int DraughtCount { get; set; }
        //风口风速
        public double AirSpeed { get; set; }
        //风口形式
        public TypeOfThDraught DraughtType { get; set; }

    }
    public class ThDraughtDesignEngine
    {
        public List<List<ThDuctEdge<ThDuctVertex>>> DraughtEndEdges { get; set; }
        public DraughtDesignParameters DesignDraughtParameters { get; set; }

        public ThDraughtDesignEngine(List<List<ThDuctEdge<ThDuctVertex>>> draughtedges, DraughtDesignParameters draughtdesignparameters)
        {
            DraughtEndEdges = draughtedges;
            DesignDraughtParameters = draughtdesignparameters;
            DraughtCountAssignment();
            DraughtDesign();
        }

        public void DraughtDesign()
        {
            var singledraughtvolume = Math.Ceiling(DesignDraughtParameters.TotalVolume / DesignDraughtParameters.DraughtCount);
            var singledraughtarea = singledraughtvolume / DesignDraughtParameters.AirSpeed;
            var draughtwidth = Math.Floor(Math.Sqrt(singledraughtarea))-100;
            var draughtlength = Math.Floor(singledraughtarea / draughtwidth);
            foreach (var edges in DraughtEndEdges)
            {
                foreach (var edge in edges)
                {
                    if (edge.DraughtCount == 0)
                    {
                        continue;
                    }
                    List<ThDraught> draughts = new List<ThDraught>();
                    foreach (var dividepoint in edge.GetEdgeDividePoint(edge.DraughtCount))
                    {
                        ThDraughtParameters draughtparameters = new ThDraughtParameters()
                        {
                            DraughtVolume = singledraughtvolume,
                            XPosition = dividepoint.X,
                            YPosition = dividepoint.Y,
                            //CenterPosition = dividepoint,
                            DraughtLength = RoundUpToTen(draughtlength),
                            DraughtWidth = RoundDownToTen(draughtwidth),
                            DraughtType = DesignDraughtParameters.DraughtType
                        };
                        ThDraught draught = new ThDraught(draughtparameters);
                        draughts.Add(draught);
                    }
                    edge.DraughtInfomation = draughts;
                }
            }
        }

        public void DraughtCountAssignment()
        {
            if (DraughtEndEdges.Count == 0)
            {
                return;
            }

            var endedgestotallength = DraughtEndEdges.Sum(s=>s.Sum(e=>e.EdgeLength));
            int totaldraughtcount = DesignDraughtParameters.DraughtCount;

            foreach (var edges in DraughtEndEdges)
            {
                double edgeslength = edges.Sum(e => e.EdgeLength);
                int draughtcountinedgelink = Convert.ToInt32(Math.Floor(edgeslength / endedgestotallength * DesignDraughtParameters.DraughtCount));
                totaldraughtcount -= draughtcountinedgelink;
                edges.First().DraughtCount = draughtcountinedgelink;
            }
            var ordereddraughtedges = DraughtEndEdges.OrderByDescending(s=> s.Sum(e => e.EdgeLength) / endedgestotallength * DesignDraughtParameters.DraughtCount - Math.Floor(s.Sum(e => e.EdgeLength) / endedgestotallength * DesignDraughtParameters.DraughtCount)).ToList();
            if (totaldraughtcount > 0)
            {
                for (int i = 0; i < totaldraughtcount; i++)
                {
                    ordereddraughtedges[i].First().DraughtCount++;
                }
            }

            foreach (var edges in DraughtEndEdges)
            {
                double edgeslength = edges.Sum(e => e.EdgeLength);
                int draughtcountinedgelink = edges.First().DraughtCount;
                foreach (var edge in edges)
                {
                    edge.DraughtCount = Convert.ToInt32(Math.Floor(edge.EdgeLength / edgeslength * draughtcountinedgelink));
                }
                int restdraughtcount = draughtcountinedgelink - edges.Sum(e=>e.DraughtCount);
                var orderededges = edges.OrderByDescending(e=> e.EdgeLength / edgeslength * draughtcountinedgelink - e.DraughtCount).ToList();
                if (restdraughtcount > 0)
                {
                    for (int i = 0; i < restdraughtcount; i++)
                    {
                        orderededges[i].DraughtCount++;
                    }
                }
            }

        }

        private int RoundUpToTen(double roundnum)
        {
            var remainder = roundnum % 10;
            return remainder == 0 ? Convert.ToInt32(roundnum) : Convert.ToInt32(roundnum - remainder + 10);
        }

        private int RoundDownToTen(double roundnum)
        {
            var remainder = roundnum % 10;
            return remainder == 0 ? Convert.ToInt32(roundnum) : Convert.ToInt32(roundnum - remainder);
        }

    }
}
