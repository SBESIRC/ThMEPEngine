using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPElectrical.ChargerDistribution.Model;

namespace ThMEPElectrical.ChargerDistribution.Service
{
    public class ThChargerCalculateService
    {
        public void BufferSearch(List<ThParkingStallInfo> infos, List<Polyline> searchedStalls, double extendWidth)
        {
            var cycle = true;
            while(cycle)
            {
                cycle = false;
                var stallIndex = new ThCADCoreNTSSpatialIndex(searchedStalls.ToCollection());
                infos.ForEach(info =>
                {
                    if (!info.Searched)
                    {
                        var buffer = info.Outline.Buffer(extendWidth).OfType<Polyline>().OrderByDescending(o => o.Area).FirstOrDefault();
                        var filter = stallIndex.SelectCrossingPolygon(buffer).OfType<Polyline>().FirstOrDefault();
                        if (filter.IsNull())
                        {
                            return;
                        }

                        var filterInfo = infos.Where(o => o.Outline.Equals(filter)).FirstOrDefault();
                        info.Searched = true;
                        info.Direction = filterInfo.Direction;
                        searchedStalls.Add(info.Outline);
                        cycle = true;
                    }
                });
            }
        }
    }
}
