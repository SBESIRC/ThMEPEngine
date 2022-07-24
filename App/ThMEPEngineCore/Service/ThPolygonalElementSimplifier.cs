using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.Service
{
    public class ThPolygonalElementSimplifier : ThBuildElementSimplifier
    {
        public ThPolygonalElementSimplifier()
        {
            AREATOLERANCE = 1.0;
            OFFSETDISTANCE = 20.0;
            DISTANCETOLERANCE = 1.0;
            TESSELLATEARCLENGTH = 100.0;
        }
        
        public override DBObjectCollection Simplify(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(o =>
            {
                if(o is AcPolygon polyline)
                {
                    results.Add(polyline.DPSimplify(DISTANCETOLERANCE));
                }
                else if(o is MPolygon mPolygon)
                {
                    var shell = mPolygon.Shell().DPSimplify(DISTANCETOLERANCE);
                    var holes = mPolygon.Holes().Select(h => h.DPSimplify(DISTANCETOLERANCE) as Curve);
                    results.Add(ThMPolygonTool.CreateMPolygon(shell, holes.Where(h => h.Area > AREATOLERANCE).ToList()));
                }
                else
                {
                    results.Add(o);
                }
            });
            return results;
        }

        public DBObjectCollection TPSimplify(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(o =>
            {
                if (o is AcPolygon polyline)
                {
                    results.Add(polyline.TPSimplify(DISTANCETOLERANCE));
                }
                else if (o is MPolygon mPolygon)
                {
                    var shell = mPolygon.Shell().TPSimplify(DISTANCETOLERANCE);
                    var holes = mPolygon.Holes().Select(h => h.TPSimplify(DISTANCETOLERANCE) as Curve);
                    results.Add(ThMPolygonTool.CreateMPolygon(shell, holes.Where(h => h.Area > AREATOLERANCE).ToList()));
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
                if(o is AcPolygon polyline)
                {
                    polyline
                    .Buffer(-OFFSETDISTANCE)
                    .OfType<AcPolygon>()
                    .ForEach(p =>
                    {
                        p.Buffer(OFFSETDISTANCE)
                        .OfType<AcPolygon>()
                        .ForEach(e => results.Add(e));
                    });
                }
                else if(o is MPolygon mPolygon)
                {
                    mPolygon
                    .Buffer(-OFFSETDISTANCE, true)
                    .OfType<MPolygon>()
                    .ForEach(m =>
                    {
                        m.Buffer(OFFSETDISTANCE, true)
                        .OfType<MPolygon>()
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
                if (o is AcPolygon polyline)
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
                    objs.Add(polygon.Tessellate(TESSELLATEARCLENGTH));
                }
                else if (c is Circle circle)
                {
                    objs.Add(circle.Tessellate(TESSELLATEARCLENGTH));
                }
                else if (c is MPolygon mPolygon)
                {
                    var shell = mPolygon.Shell();
                    var holes = mPolygon.Holes();
                    shell = shell.Tessellate(TESSELLATEARCLENGTH);
                    holes.ForEach(h => h = h.Tessellate(TESSELLATEARCLENGTH));
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
    public class ThWindowSimplifier : ThPolygonalElementSimplifier
    {
        public ThWindowSimplifier()
        {
            OFFSETDISTANCE = 20.0;
            DISTANCETOLERANCE = 1.0;
            TESSELLATEARCLENGTH = 100.0;
        }
    }
    public class ThSlabSimplifier : ThPolygonalElementSimplifier
    {
        public ThSlabSimplifier()
        {
            OFFSETDISTANCE = 20.0;
            DISTANCETOLERANCE = 1.0;
            TESSELLATEARCLENGTH = 100.0;
        }
        public override DBObjectCollection Tessellate(DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            foreach (Curve c in curves)
            {
                if (c is AcPolygon polygon)
                {
                    objs.Add(polygon.Tessellate(TESSELLATEARCLENGTH));
                }
                else if (c is Circle circle)
                {
                    objs.Add(circle.Tessellate(TESSELLATEARCLENGTH));
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
