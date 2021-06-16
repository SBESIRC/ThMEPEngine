using System;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThTesslateService
    {
        public static Entity Tesslate(Entity ent, double length)
        {
            if (ent is Line line)
            {
                return line;
            }
            else if (ent is Arc arc)
            {
                return Tesslate(arc, length);
            }
            else if (ent is Polyline poly)
            {
                return Tesslate(poly, length);
            }
            else if (ent is Circle circle)
            {
                return Tesslate(circle, length);
            }
            else if (ent is MPolygon mPolygon)
            {
                // 暂不支持对洞的打散
                return mPolygon;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private static Polyline Tesslate(Polyline polyline, double length)
        {
            var simplifier = new ThElementSimplifier()
            {
                TESSELLATE_ARC_LENGTH = length,
            };
            var objs = simplifier.Tessellate(new DBObjectCollection() { polyline });
            return objs.Count > 0 ? objs[0] as Polyline : polyline.Clone() as Polyline;
        }
        private static Polyline Tesslate(Arc arc, double length)
        {
            var simplifier = new ThElementSimplifier()
            {
                TESSELLATE_ARC_LENGTH = length,
            };
            var objs = simplifier.Tessellate(new DBObjectCollection() { arc });
            return objs.Count > 0 ? objs[0] as Polyline : arc.TessellateArcWithArc(arc.Length / 10.0);
        }
        private static Polyline Tesslate(Circle circle, double length)
        {
            var simplifier = new ThElementSimplifier()
            {
                TESSELLATE_ARC_LENGTH = length,
            };
            var objs = simplifier.Tessellate(new DBObjectCollection() { circle });
            return objs.Count > 0 ? objs[0] as Polyline : circle.Tessellate(circle.Radius * Math.PI * 2 / 10.0);
        }
    }
}
