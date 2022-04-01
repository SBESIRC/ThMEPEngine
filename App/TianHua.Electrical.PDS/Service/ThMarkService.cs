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
        private Dictionary<Entity, ObjectId> TextDic { get; set; }
        private Dictionary<DBPoint, Tuple<List<string>, ObjectId>> PointDic { get; set; }

        public ThMarkService(List<ThPDSEntityInfo> markDatas, Dictionary<Entity, ThPDSBlockReferenceData> markBlocks,
            List<ThPDSEntityInfo> tchDimension)
        {
            var lines = new DBObjectCollection();
            TextDic = new Dictionary<Entity, ObjectId>();
            PointDic = new Dictionary<DBPoint, Tuple<List<string>, ObjectId>>();
            markDatas.ForEach(data =>
            {
                var entity = data.Entity;
                if (entity is DBText)
                {
                    TextDic.Add(entity, data.SourceObjectId);
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
                        if (!PointDic.ContainsKey(point))
                        {
                            PointDic.Add(point, Tuple.Create(GetTexts(mLeader.MText), data.SourceObjectId));
                        }
                    }
                }
                else if (entity is MText)
                {
                    var objs = new DBObjectCollection();
                    entity.Explode(objs);
                    objs.OfType<DBText>().ForEach(l => TextDic.Add(l, data.SourceObjectId));
                }
                else if (entity is Table table)
                {
                    var objs = new DBObjectCollection();
                    table.Explode(objs);
                    var marks = new List<string>();
                    objs.OfType<MText>().ForEach(t => marks.AddRange(GetTexts(t)));
                    var obb = objs.OfType<Line>().ToCollection().GetMinimumRectangle();
                    obb.Vertices().OfType<Point3d>()
                        .ForEach(pt => PointDic.Add(ToDbPoint(pt), Tuple.Create(marks, data.SourceObjectId)));
                }
            });
            LineIndex = new ThCADCoreNTSSpatialIndex(lines);
            TextIndex = new ThCADCoreNTSSpatialIndex(TextDic.Keys.ToCollection());

            markBlocks.ForEach(o =>
            {
                if (o.Value.EffectiveName.Equals("E-电力平面-负荷明细"))
                {
                    var marks = GetTexts(o.Value);
                    var obb = ThPDSBufferService.Buffer(o.Key, o.Key.Database);
                    obb.Vertices().OfType<Point3d>().ForEach(pt =>
                    {
                        PointDic.Add(ToDbPoint(pt), Tuple.Create(marks, o.Value.ObjId));
                    });
                }
                else if (o.Value.EffectiveName.Contains(ThPDSCommon.LOAD_LABELS))
                {
                    var value = GetTexts(o.Value);
                    if (o.Value.CustomProperties.Contains(ThPDSCommon.POWER_CATEGORY))
                    {
                        value.Add(o.Value.CustomProperties.GetValue(ThPDSCommon.POWER_CATEGORY) as string);
                    }
                    PointDic.Add(ToDbPoint((o.Key as BlockReference).Position), Tuple.Create(value, o.Value.ObjId));
                }
                else
                {
                    PointDic.Add(ToDbPoint((o.Key as BlockReference).Position), Tuple.Create(GetTexts(o.Value), o.Value.ObjId));
                }
            });

            tchDimension.ForEach(o =>
            {
                var objs = new DBObjectCollection();
                o.Entity.Explode(objs);
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
                PointDic.Add(ToDbPoint(basePoint), Tuple.Create(textList, o.SourceObjectId));
            });

            PointIndex = new ThCADCoreNTSSpatialIndex(PointDic.Keys.ToCollection());
        }

        /// <summary>
        /// 获取负载或回路标注
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public ThPDSTextInfo GetMarks(Polyline frame)
        {
            var dbTexts = new List<Tuple<DBText, ObjectId>>();
            var result = GetMarks(frame, dbTexts);
            dbTexts.Distinct().ForEach(o =>
            {
                result.Texts.Add(o.Item1.TextString);
                result.ObjectIds.Add(o.Item2);
            });
            return result;
        }

        public List<ThPDSTextInfo> GetMultiMarks(Polyline frame)
        {
            var multiList = new List<ThPDSTextInfo>();
            var dbTexts = new List<Tuple<DBText, ObjectId>>();
            var result = GetMarks(frame, dbTexts);
            if (result.Texts.Count > 0)
            {
                result.Texts.ForEach(o =>
                {
                    multiList.Add(new ThPDSTextInfo
                    {
                        Texts = new List<string> { o },
                        ObjectIds = result.ObjectIds,
                    });
                });
            }

            multiList.AddRange(DBTextSort(dbTexts));
            return multiList;
        }

        private ThPDSTextInfo GetMarks(Polyline frame, List<Tuple<DBText, ObjectId>> dbTexts)
        {
            var result = new ThPDSTextInfo();
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
                            dbTexts.Add(Tuple.Create(t, TextDic[t]));
                        }
                    });
                }
                else
                {
                    var pointCollection = PointIndex.SelectWindowPolygon(newFrame);
                    if (pointCollection.Count > 0)
                    {
                        result.Texts.AddRange((PointDic[pointCollection[0] as DBPoint]).Item1);
                        result.ObjectIds.Add((PointDic[pointCollection[0] as DBPoint]).Item2);
                    }
                }
            });

            var points = PointIndex.SelectWindowPolygon(frame);
            if (points.Count > 0)
            {
                points.OfType<DBPoint>().ForEach(p =>
                {
                    result.Texts.AddRange(PointDic[p].Item1);
                    result.ObjectIds.Add(PointDic[p].Item2);
                });
            }
            else
            {
                frame = frame.Buffer(200.0).OfType<Polyline>().OrderByDescending(o => o.Length).First();
                var TextCollection = TextIndex.SelectCrossingPolygon(frame);
                if (TextCollection.Count > 0)
                {
                    TextCollection.OfType<DBText>().ForEach(o =>
                    {
                        result.Texts.Add(o.TextString);
                        result.ObjectIds.Add(TextDic[o]);
                    });
                }
            }

            result.Texts = Filter(result.Texts);
            return result;
        }

        private void SearchMarkLine(Polyline frame, List<Line> textLeads)
        {
            var lineCollection = LineIndex.SelectFence(frame);
            lineCollection.OfType<Line>().ForEach(o =>
            {
                if (!textLeads.Contains(o) && (frame.Contains(o.StartPoint) || frame.Contains(o.EndPoint)))
                {
                    textLeads.Add(o);
                    var newFrame = ThPDSBufferService.Buffer(o);
                    SearchMarkLine(newFrame, textLeads);
                }
            });
        }

        /// <summary>
        /// 将同行的文字归到一起
        /// </summary>
        /// <param name="texts"></param>
        /// <returns></returns>
        private List<ThPDSTextInfo> DBTextSort(List<Tuple<DBText, ObjectId>> texts)
        {
            var textCollection = new List<ThPDSDBTextCollection>();
            foreach (var text in texts)
            {
                var rad = text.Item1.Rotation * Math.PI / 180.0;
                var direction = new Vector3d(Math.Cos(rad), Math.Sin(rad), 0);
                if (textCollection.Count == 0)
                {
                    var pdsDBText = new ThPDSDBTextCollection
                    {
                        FirstPosition = text.Item1.Position,
                        Direction = direction,
                        Texts = new List<Tuple<List<string>, ObjectId>>
                        {
                            Tuple.Create(new List<string>{ text.Item1.TextString },text.Item2 ),
                        },
                    };
                    textCollection.Add(pdsDBText);
                }
                else
                {
                    var i = 0;
                    for (; i < textCollection.Count; i++)
                    {
                        if (Math.Abs((textCollection[i].FirstPosition - text.Item1.Position).GetNormal().DotProduct(textCollection[i].Direction)) > 0.995)
                        {
                            textCollection[i].Texts.Add(Tuple.Create(new List<string> { text.Item1.TextString }, text.Item2));
                            break;
                        }
                    }
                    if (i == textCollection.Count)
                    {
                        var pdsDBText = new ThPDSDBTextCollection
                        {
                            FirstPosition = text.Item1.Position,
                            Direction = direction,
                            Texts = new List<Tuple<List<string>, ObjectId>>
                            {
                                Tuple.Create(new List<string>{ text.Item1.TextString },text.Item2 ),
                            },
                        };
                        textCollection.Add(pdsDBText);
                    }
                }
            }

            var result = new List<ThPDSTextInfo>();
            textCollection.ForEach(text =>
            {
                var info = new ThPDSTextInfo();
                text.Texts.ForEach(o =>
                {
                    info.Texts.AddRange(o.Item1);
                    info.ObjectIds.Add(o.Item2);
                });
                result.Add(info);
            });
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

        private List<string> GetTexts(ThPDSBlockReferenceData Data)
        {
            var dic = Data.Attributes;
            return dic.Select(o => o.Value).ToList();
        }

        private List<string> Filter(List<string> info)
        {
            return info.Select(o => o.Replace(" ", "")).ToList();
        }
    }
}
