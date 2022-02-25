using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public class ThMarkService
    {
        private ThCADCoreNTSSpatialIndex LineIndex { get; set; }
        private ThCADCoreNTSSpatialIndex TextIndex { get; set; }
        private ThCADCoreNTSSpatialIndex PointIndex { get; set; }

        private Dictionary<DBPoint, List<string>> MarkDic { get; set; }
        public ThMarkService(List<Entity> markDatas, Dictionary<Entity, ThPDSBlockReferenceData> markBlocks)
        {
            var lines = new DBObjectCollection();
            var texts = new DBObjectCollection();
            markDatas.ForEach(entity =>
            {
                if (entity is DBText)
                {
                    texts.Add(entity);
                }
                else if (entity is Line)
                {
                    lines.Add(entity);
                }
                else if (entity is Leader)
                {
                    var objs = new DBObjectCollection();
                    entity.Explode(objs);
                    objs.OfType<Polyline>().ForEach(p => p.GetEdges().ForEach(l => lines.Add(l)));
                }
                else if (entity is MText)
                {
                    var objs = new DBObjectCollection();
                    entity.Explode(objs);
                    objs.OfType<DBText>().ForEach(l => texts.Add(l));
                }
            });
            LineIndex = new ThCADCoreNTSSpatialIndex(lines);
            TextIndex = new ThCADCoreNTSSpatialIndex(texts);

            MarkDic = new Dictionary<DBPoint, List<string>>();
            var mLeader = markDatas.OfType<MLeader>();
            mLeader.ForEach(e =>
            {
                var vertex = new Point3d();
                var continueDo = false;
                for (var i = 0; i < 5; i++)
                {
                    try
                    {
                        vertex = e.GetFirstVertex(i);
                        continueDo = true;
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
                if (continueDo)
                {
                    var point = ToDbPoint(vertex);
                    if (!MarkDic.ContainsKey(point))
                    {
                        MarkDic.Add(point, GetTexts(e.MText));
                    }
                }
            });
            markBlocks.ForEach(o =>
            {
                if (o.Value.EffectiveName.Equals("E-电力平面-负荷明细"))
                {
                    var marks = GetTexts(o.Value);
                    var obb = ThPDSBufferService.Buffer(o.Key, o.Key.Database);
                    obb.Vertices().OfType<Point3d>().ForEach(pt =>
                    {
                        MarkDic.Add(ToDbPoint(pt), marks);
                    });
                }
                else
                {
                    MarkDic.Add(ToDbPoint(o.Value.Position), GetTexts(o.Value));
                }
            });
            PointIndex = new ThCADCoreNTSSpatialIndex(MarkDic.Keys.ToCollection());
        }

        public List<string> GetMarks(Polyline frame)
        {
            var result = new List<string>();
            var lineCollection = LineIndex.SelectFence(frame);
            if (lineCollection.Count > 0)
            {
                var line = lineCollection.OfType<Line>().OrderByDescending(o => o.Length).First();
                var bfLine = line.ExtendLine(10.0).Buffer(10.0);
                lineCollection = LineIndex.SelectCrossingPolygon(bfLine);
                lineCollection.Remove(line);
                if (lineCollection.Count > 0)
                {
                    line = lineCollection.OfType<Line>().OrderByDescending(o => o.Length).First();
                }
                bfLine = line.ExtendLine(200.0).Buffer(200.0);
                var TextCollection = TextIndex.SelectCrossingPolygon(bfLine);//（Buffer200）+文字
                if (TextCollection.Count > 0)
                {
                    TextCollection.OfType<DBText>().ForEach(o =>
                    {
                        result.Add(o.TextString);
                    });
                }
                else
                {
                    var pointCollection = PointIndex.SelectWindowPolygon(bfLine);
                    if (pointCollection.Count > 0)
                    {
                        result = MarkDic[pointCollection[0] as DBPoint];
                    }
                }
            }
            else
            {
                var pointCollection = PointIndex.SelectWindowPolygon(frame);
                if (pointCollection.Count > 0)
                {
                    result = MarkDic[pointCollection[0] as DBPoint];
                }
            }
            return result.Select(o => Filter(o)).ToList();
        }

        private DBPoint ToDbPoint(Point3d point)
        {
            return new DBPoint(point);
        }

        private List<string> GetTexts(MText mText)
        {
            return mText.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private List<string> GetTexts(ThPDSBlockReferenceData Data)
        {
            var dic = Data.Attributes;
            return dic.Select(o => o.Value).ToList();
        }

        private string Filter(string info)
        {
            return info.Replace(" ", "");
        }
    }
}
