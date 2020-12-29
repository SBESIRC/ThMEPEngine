using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThFilterLinkService
    {
        private List<Line> Links { get; set; }
        private List<Line> CenterLines { get; set; }
        private double OffsetDis { get; set; }
        private ThFilterLinkService(List<Line> links, List<Line> centerLines, double offsetDis)
        {
            Links = links;
            CenterLines = centerLines;
            OffsetDis = offsetDis;
        }
        public static List<Line> Filter(List<Line> links, List<Line> centerLines,double offsetDis)
        {
            var instance = new ThFilterLinkService(links, centerLines, offsetDis);
            return instance.Filter();
        }
        private List<Line> Filter()
        {
            //Links中的线与中心线要平行，间距要等于OffsetDis，要有共有部分
            return Links.Where(o => IsValid(o)).ToList();
        }
        private bool IsValid(Line line)
        {
            return CenterLines
                .Where(o => line.HasCommon(o))
                .Where(o => IsEqualOffsetDistance(line, o))
                .Any();
        }
        private bool IsEqualOffsetDistance(Line first ,Line second)
        {
            var dis = first.Distance(second);
            return Math.Abs(dis - OffsetDis) <= 1.0;
        }
    }
}
