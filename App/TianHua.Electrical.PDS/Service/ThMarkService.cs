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

        /// <summary>
        /// 获取负载或回路标注
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public List<string> GetMarks(Polyline frame)
        {
            var dbTexts = new List<DBText>();
            var result = GetMarks(frame, dbTexts);
            dbTexts.Distinct().ForEach(o => result.Add(o.TextString));
            return result;
        }

        public List<List<string>> GetMultiMarks(Polyline frame)
        {
            var multiList = new List<List<string>>();
            var dbTexts = new List<DBText>();
            var result = GetMarks(frame, dbTexts);
            if (result.Count > 0)
            {
                result.ForEach(o =>
                {
                    multiList.Add(new List<string> { o });
                });
            }

            multiList.AddRange(DBTextSort(dbTexts.Distinct().ToList()));
            return multiList;
        }

        private List<string> GetMarks(Polyline frame, List<DBText> dbTexts)
        {
            var result = new List<string>();
            var textLeads = new List<Line>();
            SearchMarkLine(frame, textLeads);
            var tolerence = 3.0 * Math.PI / 180.0;
            textLeads.ForEach(o =>
            {
                var newFrame = ThPDSBufferService.Buffer(o, 200.0);//（Buffer200）+文字
                var TextCollection = TextIndex.SelectCrossingPolygon(newFrame);
                if (TextCollection.Count > 0)
                {
                    TextCollection.OfType<DBText>().ForEach(t =>
                    {
                        // 只取与引线方向相同的文字
                        var rad = t.Rotation * Math.PI / 180.0;
                        var vector = new Vector3d(Math.Cos(rad), Math.Sin(rad), 0);
                        var lineAngle = o.Angle % Math.PI;
                        if (Math.Abs(lineAngle - rad) < tolerence || Math.Abs(lineAngle - rad) > Math.PI - tolerence)
                        {
                            dbTexts.Add(t);
                        }
                    });
                }
                else
                {
                    var pointCollection = PointIndex.SelectWindowPolygon(newFrame);
                    if (pointCollection.Count > 0)
                    {
                        result.AddRange(MarkDic[pointCollection[0] as DBPoint]);
                    }
                }
            });

            var points = PointIndex.SelectWindowPolygon(frame);
            if (points.Count > 0)
            {
                result.AddRange(MarkDic[points[0] as DBPoint]);
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

            return result.Distinct().Select(o => Filter(o)).ToList();
        }

        private void SearchMarkLine(Polyline frame, List<Line> textLeads)
        {
            var lineCollection = LineIndex.SelectFence(frame);
            if (lineCollection.Count == 0)
            {
                return;
            }
            lineCollection.OfType<Line>().ForEach(o =>
            {
                if (!textLeads.Contains(o))
                {
                    textLeads.Add(o);
                    var newFrame = ThPDSBufferService.Buffer(o);
                    SearchMarkLine(newFrame, textLeads);
                }
            });
        }

        private List<List<string>> DBTextSort(List<DBText> texts)
        {
            var results = new List<ThPDSDBText>();
            foreach (var text in texts)
            {
                var rad = text.Rotation * Math.PI / 180.0;
                var direction = new Vector3d(Math.Cos(rad), Math.Sin(rad), 0);
                if (results.Count == 0)
                {
                    var pdsDBText = new ThPDSDBText
                    {
                        FirstPosition = text.Position,
                        Direction = direction,
                        Texts = new List<string> { text.TextString },
                    };
                    results.Add(pdsDBText);
                }
                else
                {
                    var i = 0;
                    for (; i < results.Count; i++)
                    {
                        if (Math.Abs((results[i].FirstPosition - text.Position).GetNormal().DotProduct(results[i].Direction)) > 0.995)
                        {
                            results[i].Texts.Add(text.TextString);
                            break;
                        }
                    }
                    if (i == results.Count)
                    {
                        var pdsDBText = new ThPDSDBText
                        {
                            FirstPosition = text.Position,
                            Direction = direction,
                            Texts = new List<string> { text.TextString },
                        };
                        results.Add(pdsDBText);
                    }
                }
            }

            return results.Select(x => x.Texts).ToList();
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
