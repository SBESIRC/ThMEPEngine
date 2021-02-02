using System;
using DotNetARX;
using System.Linq;
using ThMEPLighting.Common;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Engine
{
    public class ThSumNumberEngine:IDisposable
    {
        public Dictionary<Polyline, List<ThLightSumInfo>> SumInfos { get; set; }
        public ThSumNumberEngine()
        {
            SumInfos = new Dictionary<Polyline, List<ThLightSumInfo>>();
        }
        public void Sum(List<ThRegionLightEdge> regionLightEdges)
        {
            regionLightEdges.ForEach(o => Sum(o));
        }
        private void Sum(ThRegionLightEdge regionLightEdge)
        {
            var lightInfos = new List<Tuple<string, EdgePattern>>();
            regionLightEdge.Lights.ForEach(o =>
            {
                var lightInfo = XDataTools.GetXData(o.ObjectId, ThGarageLightCommon.ThGarageLightAppName);
                if(lightInfo.Count>2)
                {
                    lightInfos.Add(Tuple.Create(lightInfo[1].Value.ToString(), ThLightEdge.Trans(lightInfo[2].Value.ToString())));
                }
            });
            var sumInfos = new List<ThLightSumInfo>();
            var groups = lightInfos.GroupBy(o => o.Item1).OrderBy(o => o.Key);
            foreach(var group in groups)
            {
                sumInfos.Add( new ThLightSumInfo { Nubmer = group.Key ,Count= group.Count() });
            }
            SumInfos.Add(regionLightEdge.RegionBorder, sumInfos);
        }
        public void Dispose()
        {
        }
    }
}
