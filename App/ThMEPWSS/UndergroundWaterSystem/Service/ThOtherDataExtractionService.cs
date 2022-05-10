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
using ThMEPEngineCore.Engine;
using ThMEPWSS.UndergroundFireHydrantSystem.Extract;
using NFox.Cad;
using System.IO;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;

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
        public List<ThValveModel> GetValveModelList(Point3dCollection pts=null)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var result = new List<ThValveModel>();
                string[] names_a = new string[] { /*"给水角阀平面", */"截止阀", "闸阀", "蝶阀", "电动阀",
                "止回阀", "防污隔断网","减压阀", "Y型过滤器", "水表1", "水表井","减压阀组" };
                string[] names_b = new string[] { "防污隔断阀组", "室内水表详图" };
                string[] names_c = new string[] { "295", "296", "301", "315", "316", "333", "656", "752", "742", "743", "018", "021", "502" };
                double otherCorrespondingPipeLineLength = 1500;
                var bound = new Polyline()
                {
                    Closed = true,
                };
                if (pts != null)
                    bound.CreatePolyline(pts);
                //识别普通块
                List<List<string>> names = new List<List<string>>();
                names.Add(names_a.ToList());
                names.Add(names_b.ToList());
                for (int i = 0; i < 2; i++)
                {
                    foreach (var name in names[i])
                    {
                        var bks = ExtractBlocks(adb.Database, name).Cast<BlockReference>().ToList();
                        foreach (var br in bks)
                        {
                            ThValveModel thValveModel = new ThValveModel(br,br.GeometricExtents.CenterPoint() );
                            if (i == 1) thValveModel.CorrespondingPipeLineLength = otherCorrespondingPipeLineLength;
                            result.Add(thValveModel);
                        }
                    }
                }
                //识别天正元素
                var elements = Entities.Where(e => IsTianZhengElement(e))
                    .Where(e =>
                    {
                        if (e.Bounds == null) return false;
                        return bound.Contains(e.GeometricExtents.CenterPoint());
                    });
                foreach (var element in elements)
                {
                    var brs = RecognizeTianZhengValve(element, names_c).Where(p => bound.Contains(p.Position));
                    foreach (var br in brs)
                    {
                        ThValveModel thValveModel = new ThValveModel(br, br.GeometricExtents.CenterPoint());
                        result.Add(thValveModel);
                    }
                }
                //识别块-天正-块
                var complexed_blocks = Entities.Where(e => e is BlockReference)
                    .Where(e =>
                    {
                        try { return bound.Contains(e.GeometricExtents.CenterPoint()); }
                        catch { return true; }
                    })
                    .Where(e => e.ExplodeToDBObjectCollection().OfType<Entity>().Any())
                    .Select(e => e as BlockReference)
                    .Where(e =>
                     {
                         try { return bound.Contains(e.GeometricExtents.CenterPoint()); }
                         catch { return true; }
                     });

                foreach (var blk in complexed_blocks)
                {
                    var entities = blk.ExplodeToDBObjectCollection().OfType<Entity>();
                    foreach (var ent in entities)
                    {
                        if (IsTianZhengElement(ent))
                        {
                            var element = ent;
                            var brs = RecognizeTianZhengValve(element, names_c).Where(p => bound.Contains(p.Position));
                            foreach (var br in brs)
                            {
                                ThValveModel thValveModel = new ThValveModel(br, br.GeometricExtents.CenterPoint());
                                result.Add(thValveModel);
                            }
                        }
                    }
                }             
                result = result.Where(e => e.Valve != null)
                    .Where(e => bound.Contains(e.Point)).ToList();
                return result;          
            }
        }
        private List<BlockReference> RecognizeTianZhengValve(Entity entity,string[]names)
        {
            var results = new List<BlockReference>();
            var brs = new List<BlockReference>();
            try
            {
                brs = entity.ExplodeToDBObjectCollection().OfType<BlockReference>().ToList();
            }
            catch
            {
                /*有的天正元素炸开报错*/
                return results;
            }
            foreach (var br in brs)
            {           
                string blkname = br.Database == null ? br.Name : br.GetEffectiveName();
                foreach (var name in names)
                {
                    if (blkname.Contains(name)) results.Add(br);
                }
            }
            return results;
        }
        public List<ThFlushPointModel> GetFlushPointList(Point3dCollection pts=null)
        {

            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var result = new List<ThFlushPointModel>();
                var bound = new Polyline()
                {
                    Closed = true,
                };
                if(pts!=null)
                    bound.CreatePolyline(pts);
                var blks = ExtractBlocks(adb.Database, "给水角阀平面").Cast<BlockReference>().ToList();
                if(pts!=null)
                    blks = blks.Where(br => bound.Contains(br.Position)).ToList();
                foreach (var br in blks)
                {
                    ThFlushPointModel thFlushPoint = new ThFlushPointModel(br);
                    result.Add(thFlushPoint);
                }
                return result;
            }
        }
        private DBObjectCollection ExtractBlocks(Database db, string blockName)
        {
            Func<Entity, bool> IsBlkNameQualified = (e) =>
            {
                if (e is BlockReference br)
                {
                    return br.GetEffectiveName().ToUpper().Contains(blockName.ToUpper());
                }
                return false;
            };
            var blkVisitor = new ThBlockReferenceExtractionVisitor();
            blkVisitor.CheckQualifiedLayer = (e) => true;
            blkVisitor.CheckQualifiedBlockName = IsBlkNameQualified;

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(blkVisitor);
            extractor.Extract(db); // 提取块中块(包括外参)
            extractor.ExtractFromMS(db); // 提取本地块

            return blkVisitor.Results.Select(o => o.Geometry).ToCollection();
        }
    }
}
