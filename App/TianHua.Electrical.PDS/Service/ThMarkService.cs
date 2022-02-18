using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using System;
using ThMEPEngineCore.CAD;

namespace TianHua.Electrical.PDS.Service
{
    public class ThMarkService
    {
        private ThCADCoreNTSSpatialIndex Lines { get; set; }
        private ThCADCoreNTSSpatialIndex Texts { get; set; }
        private ThCADCoreNTSSpatialIndex points { get; set; }

        private Dictionary<DBPoint,List<string>> marksDic { get; set; }
        public ThMarkService(List<ThRawIfcAnnotationElementData> markDatas, List<ThBlockReferenceData> markBlocks)
        {
            var lines = new DBObjectCollection();
            var texts = new DBObjectCollection();
            markDatas.ForEach(o =>
            {
                var entity = o.Data as Entity;
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
            marksDic = new Dictionary<DBPoint, List<string>>();
            Lines = new ThCADCoreNTSSpatialIndex(lines);
            Texts = new ThCADCoreNTSSpatialIndex(texts);

            var mLeader = markDatas.Select(o => o.Data).OfType<MLeader>();
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
                        break ;
                    }
                    catch
                    {
                        continue;
                    }
                }
                if(continueDo)
                {
                    marksDic.Add(ToDbPoint(vertex), GetTexts(e.MText));
                }
            });
            markBlocks.ForEach(o =>
            {
                if (o.EffectiveName.Equals("E-电力平面-负荷明细"))
                {

                }
                else
                {
                    marksDic.Add(ToDbPoint(o.Position), GetTexts(o));
                }
            });
            points = new ThCADCoreNTSSpatialIndex(marksDic.Keys.ToCollection());
        }

        public List<string> GetMarks(Polyline frame)
        {
            List<string> result = new List<string>();
            var lineCollection = Lines.SelectFence(frame);
            if (lineCollection.Count > 0)
            {
                Line line = lineCollection.Cast<Line>().OrderByDescending(o => o.Length).First();
                var bfLine = line.ExtendLine(10).Buffer(10);
                lineCollection = Lines.SelectCrossingPolygon(bfLine);
                lineCollection.Remove(line);
                if (lineCollection.Count > 0)
                {
                    line = lineCollection.Cast<Line>().OrderByDescending(o => o.Length).First();
                }
                bfLine = line.ExtendLine(200).Buffer(200);
                var TextCollection = Texts.SelectCrossingPolygon(bfLine);//（Buffer200）+文字
                TextCollection.OfType<DBText>().ForEach(o =>
                {
                    result.Add(o.TextString);
                });
            }
            else
            {
                var pointCollection = points.SelectWindowPolygon(frame);
                if(pointCollection.Count > 0)
                {
                    result = marksDic[pointCollection[0] as DBPoint];
                }
            }
            return result;
        }

        private DBPoint ToDbPoint(Point3d point)
        {
            return new DBPoint(point);
        }

        private List<string> GetTexts(MText mText)
        {
            return mText.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private List<string> GetTexts(ThBlockReferenceData Data)
        {
            var dic = Data.Attributes;
            return dic.Select(o => o.Key + ":" + o.Value).ToList();
        }
    }
}
