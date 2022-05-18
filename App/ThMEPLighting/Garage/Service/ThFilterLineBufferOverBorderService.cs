using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service
{
    internal class ThFilterLineBufferOverBorderService
    {
        public ThFilterLineBufferOverBorderService()
        {
        }
        public List<Line> Filter(List<Line> lines, Entity polygon, double length)
        {
            var results = new List<Line>();
            var garbages = new DBObjectCollection();
            lines.Where(l=>l.Length>0.0).ForEach(l =>
            {
                // 往l的PerpendVector方向Buffer
                var left = Buffer(l, length,false);
                garbages.Add(left);

                var leftInLines = Trim(left, polygon);
                leftInLines.ForEach(o => garbages.Add(o));

                // 往l的PerpendVector.Negate方向Buffer
                var rightLines = new List<Line>();
                leftInLines.ForEach(o =>
                {
                    rightLines.Add(Buffer(o, 2 * length, true));
                });
                rightLines.ForEach(o => garbages.Add(o));

                // 计算右边在框里的线
                var rightInLines = rightLines.Trim(polygon);
                rightInLines.ForEach(o => garbages.Add(o));

                // 把右边在框里的线，再往PerpendVector方向Buffer，回到l位置
                rightInLines.ForEach(o => results.Add(Buffer(o, length,false)));
            });

            // 释放
            garbages = garbages.Difference(results.ToCollection());
            garbages.MDispose();

            return results;
        }

        private List<Line> Trim(Line line,Entity polygon)
        {            
            return new List<Line> { line }
            .Trim(polygon)
            .Where(o=>o.Length>0.0)
            .ToList();
        }

        private Line Buffer(Line line,double length,bool isNegate=false)
        {
            var dir = line.LineDirection();
            var perpend = dir.GetPerpendicularVector();
            if(isNegate)
            {
                perpend = perpend.Negate();
            }
            var mt = Matrix3d.Displacement(perpend.MultiplyBy(length));
            return line.GetTransformedCopy(mt) as Line;
        }
    }
}
