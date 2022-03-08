using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module;
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
        ////田工说直接以后直接传String，不用枚举了，等他改好再说
        //public string ConvertToString(CircuitFormInType @in)
        //{
        //    return @in switch
        //    {
        //        CircuitFormInType.一路进线 => "1路进线",
        //        CircuitFormInType.二路进线ATSE => "2路进线ATSE",
        //        CircuitFormInType.三路进线 => "3路进线",
        //        CircuitFormInType.集中电源 => "集中电源",
        //        CircuitFormInType.None => "设备自带控制箱",
        //        _ => null,
        //    };
        //}
        //public string ConvertToString(CircuitFormOutType @out)
        //{
        //    return @out switch
        //    {
        //        CircuitFormOutType.常规 => "常规",
        //        CircuitFormOutType.漏电 => "漏电",
        //        CircuitFormOutType.接触器控制 => "接触器控制",
        //        CircuitFormOutType.热继电器保护 => "热继电器保护",
        //        CircuitFormOutType.配电计量_上海CT => "配电计量（上海CT）",
        //        CircuitFormOutType.配电计量_上海直接表 => "配电计量（上海直接表）",
        //        CircuitFormOutType.配电计量_CT表在前 => "配电计量（CT表在前）",
        //        CircuitFormOutType.配电计量_直接表在前 => "配电计量（直接表在前）",
        //        CircuitFormOutType.配电计量_CT表在后 => "配电计量（CT表在后）",
        //        //配电计量（直接表在后）
        //        CircuitFormOutType.电动机_分立元件 => "电动机（分立元件）",
        //        CircuitFormOutType.电动机_CPS => "电动机（CPS）",
        //        CircuitFormOutType.电动机_分立元件星三角启动 => "电动机（分立元件星三角启动）",
        //        CircuitFormOutType.电动机_CPS星三角启动 => "电动机（CPS星三角启动）",
        //        CircuitFormOutType.双速电动机_分立元件detailYY => "双速电动机（分立元件 D-YY）",
        //        CircuitFormOutType.双速电动机_分立元件YY => "双速电动机（分立元件 Y-Y）",
        //        CircuitFormOutType.双速电动机_CPSdetailYY => "双速电动机（CPS D-YY）",
        //        CircuitFormOutType.双速电动机_CPSYY => "双速电动机（CPS Y-Y）",
        //        CircuitFormOutType.消防应急照明回路WFEL => "消防应急照明回路（WFEL）",
        //        CircuitFormOutType.SPD => null,
        //        //控制（从属CPS）
        //        //SPD附件
        //        CircuitFormOutType.小母排 => null,
        //        //分支小母排
        //        //小母排分支
        //        CircuitFormOutType.None => null,
        //        _ => null,
        //    };
        //}
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
