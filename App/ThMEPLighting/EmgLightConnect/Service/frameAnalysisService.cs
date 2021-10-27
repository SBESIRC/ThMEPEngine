using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class frameAnalysisService
    {
        public static Dictionary<Polyline, List<Polyline>> analysisHoles(List<Polyline> frameList)
        {
            var frameListHoles = new Dictionary<Polyline, List<Polyline>>();
            var allHoles = new List<Polyline>();

            frameList = frameList.OrderByDescending(x => x.Area).ToList();

            for (int i = 0; i < frameList.Count; i++)
            {
                if (allHoles.Contains(frameList[i]) == false)
                {
                    frameList[i].Closed = true;
                    var holes = new List<Polyline>();
                    for (int j = i + 1; j < frameList.Count; j++)
                    {
                        frameList[j].Closed = true;
                        ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(frameList[i], frameList[j]);

                        if (relation.IsContains)
                        {
                            holes.Add(frameList[j]);
                            allHoles.Add(frameList[j]);
                        }
                        else if (relation.IsIntersects && relation.IsOverlaps)
                        {
                            var polyCollection = new DBObjectCollection() { frameList[i] };
                            var overlap = frameList[i].Intersection(polyCollection);

                            if (overlap.Count > 0)
                            {
                                var overlapPoly = overlap.Cast<Polyline>().OrderByDescending(x => x.Area).First();
                                if (overlapPoly.Area / frameList[j].Area > 0.6)
                                {
                                    holes.Add(frameList[j]);
                                    allHoles.Add(frameList[j]);
                                }
                            }
                        }
                    }
                    frameListHoles.Add(frameList[i], holes);
                }
            }

            return frameListHoles;

        }
    }
}
