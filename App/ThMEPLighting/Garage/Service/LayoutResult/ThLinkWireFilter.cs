using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThLinkWireFilter
    {
        public ThLinkWireFilter()
        {
        }

        public DBObjectCollection Filter(DBObjectCollection linkWires)
        {
            var garbages = FindFilterLines(linkWires);
            var results = Remove(linkWires, garbages);
            return results;
        }

        private DBObjectCollection Remove(DBObjectCollection linkWires,DBObjectCollection garbages)
        {
            return linkWires
                .OfType<Entity>()
                .Where(o => !garbages.Contains(o))
                .ToCollection();
        }

        private DBObjectCollection FindFilterLines(DBObjectCollection linkWires)
        {
            var garbages = new DBObjectCollection();
            var threeWays = linkWires.OfType<Line>().ToList().GetThreeWays();
            threeWays
                .Where(o => o.Count == 3)
                .ForEach(o =>
                {
                    var branch = Filter(o);
                    if (branch != null)
                    {
                        garbages.Add(branch);
                    }
                });
            return garbages;
        }

        private Line Filter(List<Line> lines)
        {
            var pairs = lines.GetLinePairs();
            var mainPair = pairs.OrderBy(k => k.Item1.GetLineOuterAngle(k.Item2)).First();
            if (mainPair.Item1.IsLessThan45Degree(mainPair.Item2))
            {
                return lines.FindBranch(mainPair.Item1, mainPair.Item2);
            }
            else
            {
                return null;
            }
        }
    }
}
