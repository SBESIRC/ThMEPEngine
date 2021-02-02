using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThFindLinkService
    {
        private List<Line> Lines { get; set; }
        private List<Line> Adds { get; set; }
        private ThQueryLineService QueryLineService { get; set; }
        private ThFindLinkService(List<Line> lines, ThQueryLineService queryLineService)
        {
            Lines = lines;
            QueryLineService = queryLineService;
            Adds = new List<Line>();
        }
        public static List<Line> Find(List<Line> lines, ThQueryLineService queryLineService)
        {
            var instance = new ThFindLinkService(lines, queryLineService);
            instance.Find();
            return instance.Adds;
        }
        private void Find()
        {
            for (int i = 0; i < Lines.Count - 1; i++)
            {
                var first = Lines[i];
                for (int j = i + 1; j < Lines.Count; j++)
                {
                    var second = Lines[j];
                    var links = new List<Line>();
                    links.Add(Find(first, first.StartPoint, second, second.StartPoint));
                    links.Add(Find(first, first.StartPoint, second, second.EndPoint));
                    links.Add(Find(first, first.EndPoint, second, second.StartPoint));
                    links.Add(Find(first, first.EndPoint, second, second.EndPoint));
                    links.Where(o => o.Length > 0)
                        .Where(o => !Lines.IsContains(o))
                        .ForEach(o =>
                        {
                            if(!Adds.IsContains(o))
                            {
                                Adds.Add(o);
                            }
                        });
                }
            }
        }
        private Line Find(Line first, Point3d firstPt, Line second, Point3d secondPt)
        {
            if (firstPt.DistanceTo(secondPt) <= ThGarageLightCommon.RepeatedPointDistance)
            {
                return new Line();
            }
            var firstLinks = QueryLineService.Query(firstPt);
            var secondLinks = QueryLineService.Query(secondPt);
            firstLinks.Remove(first);
            secondLinks.Remove(first);
            var commons = firstLinks.Where(o => secondLinks.IsContains(o));
            return commons.Count() == 1 ? commons.First() : new Line();
        }
    }
}
