using System;
using System.Linq;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public class ThElementSimplifier : ThBuildElementSimplifier
    {
        public double OFFSET_DISTANCE { get; set; }
        public double DISTANCE_TOLERANCE { get; set; }
        public double TESSELLATE_ARC_LENGTH { get; set; }        

        public ThElementSimplifier()
        {
            OFFSET_DISTANCE = 20.0;
            DISTANCE_TOLERANCE = 1.0;
            TESSELLATE_ARC_LENGTH = 10.0;
        }
        
        public override DBObjectCollection Simplify(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(o =>
            {
                // 由于投影误差，DB3切出来的墙线中有非常短的线段（长度<1mm)
                // 这里使用简化算法，剔除掉这些非常短的线段
                if(o is Polyline polyline)
                {
                    results.Add(polyline.DPSimplify(DISTANCE_TOLERANCE));
                }
                else if(o is MPolygon mPolygon)
                {
                    var shell = mPolygon.Shell().DPSimplify(DISTANCE_TOLERANCE);
                    var holes = mPolygon.Holes()
                    .Select(h=>h.DPSimplify(DISTANCE_TOLERANCE))
                    .Where(h=>h.Area>1e-6).OfType<Curve>().ToList();
                    results.Add(ThMPolygonTool.CreateMPolygon(shell, holes));
                }
                else
                {
                    results.Add(o);
                }
            });
            return results;
        }

        public override DBObjectCollection Normalize(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(o =>
            {
                if(o is Polyline polyline)
                {
                    polyline
                    .Buffer(-OFFSET_DISTANCE)
                    .OfType<AcPolygon>()
                    .ForEach(p =>
                    {
                        p.Buffer(OFFSET_DISTANCE)
                        .Cast<AcPolygon>()
                        .ForEach(e => results.Add(e));
                    });
                }
                else if(o is MPolygon mPolygon)
                {
                    mPolygon
                    .Buffer(-OFFSET_DISTANCE,true)
                    .OfType<Entity>()
                    .Where(m=> m is MPolygon && m.GetArea()>1e-6)
                    .OfType<MPolygon>()
                    .ForEach(m=>
                    {
                        m.Buffer(OFFSET_DISTANCE,true)
                       .OfType<Entity>()
                       .Where(n=>n.GetArea()>1e-6)
                       .ForEach(e => results.Add(e));
                    });
                }
                else
                {
                    results.Add(o);
                }
            });
            return results;
        }

        public override DBObjectCollection MakeValid(DBObjectCollection curves)
        {
            var results = new DBObjectCollection();
            curves.OfType<Entity>().ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    var res = polyline.MakeValid();
                    res.Cast<Entity>().ForEach(e => results.Add(e));
                }
                else if (o is MPolygon mPolygon)
                {
                    var res = mPolygon.MakeValid(true);
                    res.Cast<Entity>().ForEach(e => results.Add(e));
                }
                else
                {
                    results.Add(o);
                }
            });
            return results;
        }

        public override DBObjectCollection Tessellate(DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            foreach (Entity c in curves)
            {
                if (c is AcPolygon polygon)
                {
                    objs.Add(polygon.Tessellate(TESSELLATE_ARC_LENGTH));
                }
                else if (c is Circle circle)
                {
                    objs.Add(circle.Tessellate(TESSELLATE_ARC_LENGTH));
                }
                else if (c is Arc arc)
                {
                    objs.Add(arc.TessellateArcWithArc(TESSELLATE_ARC_LENGTH));
                }
                else if (c is MPolygon mPolygon)
                {
                    //不支持多个MuiltPolygon
                    var shell = mPolygon.Shell();
                    var holes = mPolygon.Holes();
                    shell = shell.Tessellate(TESSELLATE_ARC_LENGTH);
                    holes.ForEach(h => h = h.Tessellate(TESSELLATE_ARC_LENGTH));
                    objs.Add(ThMPolygonTool.CreateMPolygon(shell, holes.Cast<Curve>().ToList()));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return objs;
        }
    }
    public class ThWindowSimplifier : ThElementSimplifier
    {
        public ThWindowSimplifier()
        {
            OFFSET_DISTANCE = 20.0;
            DISTANCE_TOLERANCE = 1.0;
            TESSELLATE_ARC_LENGTH = 10.0;
        }
    }
    public class ThSlabSimplifier : ThElementSimplifier
    {
        public ThSlabSimplifier()
        {
            OFFSET_DISTANCE = 20.0;
            DISTANCE_TOLERANCE = 1.0;
            TESSELLATE_ARC_LENGTH = 100.0;
        }
        public override DBObjectCollection Tessellate(DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            foreach (Curve c in curves)
            {
                if (c is AcPolygon polygon)
                {
                    objs.Add(polygon.Tessellate(TESSELLATE_ARC_LENGTH));
                }
                else if (c is Circle circle)
                {
                    objs.Add(circle.Tessellate(TESSELLATE_ARC_LENGTH));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return objs;
        }
    }
}
