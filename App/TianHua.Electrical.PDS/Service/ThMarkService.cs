using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using TianHua.Electrical.PDS.Model;
using ThMEPEngineCore.Algorithm;

namespace TianHua.Electrical.PDS.Service
{
    public class ThMarkService
    {
        private ThCADCoreNTSSpatialIndex LineIndex { get; set; }
        private ThCADCoreNTSSpatialIndex TextIndex { get; set; }
        private ThCADCoreNTSSpatialIndex PointIndex { get; set; }

        private Dictionary<DBPoint, List<string>> MarkDic { get; set; }

        public ThMarkService(List<Entity> markDatas, Dictionary<Entity, ThPDSBlockReferenceData> markBlocks,
            List<Entity> tchDimension)
        {
            var lines = new DBObjectCollection();
            var texts = new DBObjectCollection();
            MarkDic = new Dictionary<DBPoint, List<string>>();
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
                else if (entity is Polyline)
                {
                    lines.Add(entity);
                }
                else if (entity is Leader leader)
                {
                    var objs = new DBObjectCollection();
                    entity.Explode(objs);
                    objs.OfType<Polyline>().ForEach(p =>
                    {
                        var lineList = p.GetEdges();
                        for (var i = 0; i < lineList.Count; i++)
                        {
                            if (i == 0)
                            {
                                lines.Add(new Line(leader.StartPoint, lineList[i].EndPoint));
                            }
                            else
                            {
                                lines.Add(lineList[i]);
                            }
                        }
                    });
                }
                else if (entity is MLeader mLeader)
                {
                    var vertex = new Point3d();
                    var continueDo = false;
                    for (var i = 0; i < 5; i++)
                    {
                        try
                        {
                            vertex = mLeader.GetFirstVertex(i);
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
                            MarkDic.Add(point, GetTexts(mLeader.MText));
                        }
                    }
                }
                else if (entity is MText)
                {
                    var objs = new DBObjectCollection();
                    entity.Explode(objs);
                    objs.OfType<DBText>().ForEach(l => texts.Add(l));
                }
                else if (entity is Table table)
                {
                    var objs = new DBObjectCollection();
                    table.Explode(objs);
                    var marks = new List<string>();
                    objs.OfType<MText>().ForEach(t => marks.AddRange(GetTexts(t)));
                    var obb = objs.OfType<Line>().ToCollection().GetMinimumRectangle();
                    obb.Vertices().OfType<Point3d>().ForEach(pt => MarkDic.Add(ToDbPoint(pt), marks));
                }
            });
            LineIndex = new ThCADCoreNTSSpatialIndex(lines);
            TextIndex = new ThCADCoreNTSSpatialIndex(texts);

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
                else if (o.Value.EffectiveName.Contains("负载标注"))
                {
                    var value = GetTexts(o.Value);
                    if (o.Value.CustomProperties.Contains(ThPDSCommon.POWER_CATEGORY))
                    {
                        value.Add(o.Value.CustomProperties.GetValue(ThPDSCommon.POWER_CATEGORY) as string);
                    }
                    MarkDic.Add(ToDbPoint((o.Key as BlockReference).Position), value);
                }
                else
                {
                    MarkDic.Add(ToDbPoint((o.Key as BlockReference).Position), GetTexts(o.Value));
                }
            });

            tchDimension.ForEach(o =>
            {
                var objs = new DBObjectCollection();
                o.Explode(objs);
                var basePoint = objs.OfType<Line>().First().GetCenter();
                var textList = new List<string>();
                objs.OfType<Entity>().ForEach(e =>
                {
                    if (ThMEPTCHService.IsTCHText(e))
                    {
                        var text = new DBObjectCollection();
                        e.Explode(text);
                        textList.Add(text.OfType<DBText>().First().TextString);
                    }
                });
                MarkDic.Add(ToDbPoint(basePoint), textList);
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
                var newframe = line.ExtendLine(10.0).Buffer(10.0);
                lineCollection = LineIndex.SelectCrossingPolygon(newframe);
                lineCollection.Remove(line);

                if (lineCollection.OfType<Polyline>().Count() > 0)
                {
                    var polyCollection = lineCollection.OfType<Polyline>();
                    var polys = polyCollection.Select(p => ThPDSBufferService.Buffer(p)).ToCollection();
                    newframe = polys.ToNTSMultiPolygon().Union().ToDbCollection().OfType<Polyline>().FirstOrDefault();

                    var doSearch = true;
                    while (doSearch && newframe != null)
                    {
                        var newPolyCollection = LineIndex.SelectCrossingPolygon(newframe).OfType<Polyline>();
                        if (newPolyCollection.Count() <= polyCollection.Count())
                        {
                            doSearch = false;
                            break;
                        }
                        polys = newPolyCollection.Select(p => ThPDSBufferService.Buffer(p)).ToCollection();
                        newframe = polys.ToNTSMultiPolygon().Union().ToDbCollection().OfType<Polyline>().FirstOrDefault();
                        polyCollection = newPolyCollection;
                    }
                }
                else
                {
                    if (lineCollection.OfType<Line>().Count() > 0)
                    {
                        line = lineCollection.OfType<Line>().OrderByDescending(o => o.Length).First();
                    }
                    newframe = line.ExtendLine(200.0).Buffer(200.0);
                }

                var TextCollection = TextIndex.SelectCrossingPolygon(newframe);//（Buffer200）+文字
                if (TextCollection.Count > 0)
                {
                    TextCollection.OfType<DBText>().ForEach(o =>
                    {
                        result.Add(o.TextString);
                    });
                }
                else
                {
                    var pointCollection = PointIndex.SelectWindowPolygon(newframe);
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
                else
                {
                    frame = frame.Buffer(200.0).OfType<Polyline>().OrderByDescending(o => o.Length).First();
                    var TextCollection = TextIndex.SelectCrossingPolygon(frame);
                    if (TextCollection.Count > 0)
                    {
                        TextCollection.OfType<DBText>().ForEach(o =>
                        {
                            result.Add(o.TextString);
                        });
                    }
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
