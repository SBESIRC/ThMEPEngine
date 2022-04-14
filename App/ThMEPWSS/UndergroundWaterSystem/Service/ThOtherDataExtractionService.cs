using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.CADExtensionsNs;
using DotNetARX;
using ThCADCore.NTS;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThOtherDataExtractionService
    {
        private List<Entity> Entities = null;
        public ThOtherDataExtractionService()
        {
            if (Entities == null) CollectEntities();
        }
        private void CollectEntities()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                IEnumerable<Entity> GetEntities()
                {
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br)
                        {
                            if (br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 20000 && r.Width < 80000 && r.Height > 5000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else if (br.Layer == "块")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                        else
                        {
                            yield return ent;
                        }
                    }
                }
                var entities = GetEntities().ToList();
                Entities = entities;
            }
        }
        public List<ThValveModel> GetValveModelList(Point3dCollection pts)
        {
            var result = new List<ThValveModel>();
            string[] names_a = new string[] { "给水角阀平面", "截止阀", "闸阀", "蝶阀", "电动阀",
                "止回阀", "防污隔断网", "减压阀", "Y型过滤器", "水表1", "水表井","减压阀组" };
            string[] names_b = new string[] { "防污隔断阀组", "室内水表详图" };
            string[] names_c = new string[] { "295", "296","301", "315", "316", "333", "752", "743", "018", "021", "502" };
            double otherCorrespondingPipeLineLength = 1500;
            var bound = new Polyline()
            {
                Closed = true,
            };
            bound.CreatePolyline(pts);
            foreach (var e in Entities.OfType<BlockReference>().Where(e =>
            {
                bool cond_a = e.ObjectId.IsValid;
                bool cond_b = false;
                bool cond_c = false;
                bool cond_d = false;
                foreach (var name in names_a)
                    if (e.GetEffectiveName().Contains(name))
                        cond_b = true;
                foreach (var name in names_b)
                    if (e.GetEffectiveName().Contains(name))
                        cond_c = true;
                if (cond_a && (cond_b || cond_c))
                    cond_d = bound.Contains(e.Position);
                if (cond_a && (cond_b || cond_c) && cond_d) return true;
                else return false;
            }))
            {
                bool isDefaultCorrespondingPipeLineLength = true;
                foreach (var name in names_b)
                    if (e.GetEffectiveName().Contains(name))
                        isDefaultCorrespondingPipeLineLength = false;
                ThValveModel thValveModel = new ThValveModel(e);
                if (!isDefaultCorrespondingPipeLineLength) thValveModel.CorrespondingPipeLineLength = otherCorrespondingPipeLineLength;
                result.Add(thValveModel);
            }
            foreach (var e in Entities.OfType<Entity>()
                .Where(e => IsTianZhengElement(e))
                .Where(e =>
                {
                    try
                    {
                        return e.ExplodeToDBObjectCollection().OfType<BlockReference>().Any();
                    }
                    catch { return false; }
                })
                .Select(e =>
                {
                    var brs = e.ExplodeToDBObjectCollection().OfType<BlockReference>().ToList();
                    return brs;
                })
                .Where(e => e.Count>0)
                .Select(e => e[0])
                .Where(e =>
                {
                    var str = e.Name;
                    foreach (var name in names_c)
                        if (str.Contains(name)) return true;
                    return false;
                })
                .Where(e =>
                {
                    if (e.Bounds is Extents3d extent3d)
                    {
                        if (bound.Contains(extent3d.CenterPoint())) return true;
                        else return false;
                    }
                    return false;
                }))
            {
                ThValveModel thValveModel = new ThValveModel(e);            
                result.Add(thValveModel);
            }
            return result.Where(e => e.Valve!=null).ToList();
        }
    }
}
