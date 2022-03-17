using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Electrical.PDS.UI.Models;
using static TianHua.Electrical.PDS.UI.Models.DrawUtils;

namespace TianHua.Electrical.PDS.UI.Services
{
    public class PDSBlockInfo
    {
        public Point3d BasePoint;
        public GRect Boundary;
        public string BlockName;
        public List<GLineSegment> Lines;
        public List<GCircle> Circles;
        public List<GArc> Arcs;
        public List<CText> CTexts;
    }
    public class ThPDSCircuitGraphComponentGenerator
    {
        public static List<PDSBlockInfo> PDSBlockInfos;
        private void Init()
        {
            if (PDSBlockInfos != null) return;
            var file = ThCADExtension.ThCADCommon.PDSComponentPath();
            using var lck = DocLock;
            using var adb = Linq2Acad.AcadDatabase.Open(file, Linq2Acad.DwgOpenMode.ReadOnly);
            static bool canExplode(Entity entity)
            {
                var type = entity.GetType();
                return !(type == typeof(Line) || type == typeof(Circle) || type == typeof(DBPoint) || type == typeof(DBText));
            }
            static IEnumerable<Entity> _explode(Entity entity)
            {
                try
                {
                    return entity.ExplodeToDBObjectCollection().OfType<Entity>();
                }
                catch
                {
                    return Enumerable.Empty<Entity>();
                }
            }
            static IEnumerable<Entity> explode(Entity entity)
            {
                if (canExplode(entity))
                {
                    foreach (var ent in _explode(entity))
                    {
                        foreach (var e in explode(ent))
                        {
                            yield return e;
                        }
                    }
                }
                else
                {
                    yield return entity;
                }
            }
            var infos = new List<PDSBlockInfo>(4096);
            foreach (var br in adb.ModelSpace.OfType<BlockReference>())
            {
                if (br.BlockTableRecord.IsValid)
                {
                    var ents = explode(br);
                    var info = new PDSBlockInfo
                    {
                        BlockName = br.GetEffectiveName(),
                        BasePoint = br.Position,
                        Boundary = br.Bounds.ToGRect(),
                        Lines = ents.OfType<Line>().Where(x => x.Length > 0).Select(x => x.ToGLineSegment()).ToList(),
                        Circles = ents.OfType<Circle>().Where(x => x.Radius > 0).Select(x => x.ToGCircle()).ToList(),
                        CTexts = ents.OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)).Select(x => new CText() { Text = x.TextString, Boundary = x.Bounds.ToGRect() }).ToList(),
                        Arcs = ents.OfType<Arc>().Where(x => x.Length > 0).Select(x => x.ToGArc()).ToList()
                    };
                    infos.Add(info);
                }
            }
            PDSBlockInfos = infos;
        }
        public PDSBlockInfo IN(string type)
        {
            Init();
            return PDSBlockInfos.FirstOrDefault(x => x.BlockName == type);
        }
        public PDSBlockInfo OUT(string type)
        {
            Init();
            return PDSBlockInfos.FirstOrDefault(x => x.BlockName == type);
        }
    }
}
