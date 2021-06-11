using System;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThCurveCollectionExtension
    {
        public static DBObjectCollection ExplodeCurves(this DBObjectCollection curves)
        {
            var results = new DBObjectCollection();
            curves.Cast<Curve>().ForEach(c =>
            {
                if (c is Line line)
                {
                    // 线不用“炸”
                    results.Add(line.Clone() as Line);
                }
                else if (c is Circle circle)
                {
                    // 圆不支持“炸”
                    results.Add(circle.Clone() as Circle);
                }
                else if (c is Polyline polyline)
                {
                    var items = new DBObjectCollection();
                    polyline.Explode(items);
                    items.Cast<Curve>().ForEach(o => results.Add(o));
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return results;
        }
    }
}
