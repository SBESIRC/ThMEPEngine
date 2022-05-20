using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
                else if(entity.IsTCHText())
                {
                    var text = new DBObjectCollection();
                    entity.Explode(text);
                    TextDic.Add(text.OfType<DBText>().First(), data.SourceObjectId);
                }
            });
            LineIndex = new ThCADCoreNTSSpatialIndex(lines);
            TextIndex = new ThCADCoreNTSSpatialIndex(TextDic.Keys.ToCollection());

            var removeLines = new List<Line>();
            // 多回路标注情形
            lines.OfType<Line>().ForEach(l =>
            {
                var lineFrame = ThPDSBufferService.Buffer(l);
                var filter = LineIndex.SelectCrossingPolygon(lineFrame).OfType<Line>().ToList();
                var obliqueLines = filter.Except(new List<Line> { l })
                    .Where(o => o.Length < 1000.0)
                    .Where(o => Math.Abs(o.LineDirection().DotProduct(l.LineDirection())) > 0.1
                        && Math.Abs(o.LineDirection().DotProduct(l.LineDirection())) < 0.9)
                    .ToList();
                if (obliqueLines.Count > 1)
                {
                    removeLines.AddRange(obliqueLines);
                    removeLines.Add(l);
                    var crossPoints = new List<Point3d>();
                    obliqueLines.ForEach(o =>
                    {
                        crossPoints.AddRange(l.Intersect(o, Intersect.OnBothOperands));
                    });
                    if (crossPoints.Count < 2)
                    {
                        return;
                    }

                    var textSearchFrame = filter.Except(obliqueLines).ToCollection()
                        .Buffer(10 * ThPDSCommon.ALLOWABLE_TOLERANCE)
                        .OfType<Polyline>()
                        .OrderByDescending(p => p.Area)
                        .First();
                    var texts = TextIndex.SelectCrossingPolygon(textSearchFrame)
                        .OfType<DBText>()
                        .OrderByDescending(o => o.Position.Y)
                        .ToList();
                    var circuitNumbers = new List<Tuple<string, ObjectId>>();
                    var startPoint = new Point3d();
                    var direction = new Vector3d(1, 0, 0);

                    texts.ForEach(o =>
                    {
                        var info = o.TextString;
                        var regex1 = new Regex(@"[左].+[右]");
                        var match1 = regex1.Match(info);
                        if (match1.Success)
                        {
                            startPoint = o.Position.DistanceTo(l.StartPoint) < o.Position.DistanceTo(l.EndPoint) ?
                                l.StartPoint : l.EndPoint;
                            direction = new Vector3d(1, 0, 0);
                            return;
                        }

                        var charRegex = new Regex(@"[W].{1,5}[/].{0,2}[W]");
                        var charMatch = charRegex.Match(info);
                        if (charMatch.Success)
                        {
                            info = ThPDSReplaceStringService.ReplaceLastChar(info, "/", "~");
                        }
                        if (info.Contains("~"))
                        {
                            var numberRegex = new Regex(@"[0-9]+~[A-Z]*[0-9]+");
                            var numberMatch = numberRegex.Match(info);
                            if (numberMatch.Success)
                            {
                                var loadId = info.Replace(numberMatch.Value, "");
                                var first = new Regex(@"[0-9]+");
                                var firstMatch = first.Match(numberMatch.Value);
                                var secondMatch = firstMatch.NextMatch();
                                if (firstMatch.Success && secondMatch.Success)
                                {
                                    var leftNum = Convert.ToInt32(firstMatch.Value);
                                    var rightNum = Convert.ToInt32(secondMatch.Value);
                                    for (var i = leftNum; i <= rightNum; i++)
                                    {
                                        if (i < 10)
                                        {
                                            circuitNumbers.Add(Tuple.Create(loadId + "0" + i.ToString(), TextDic[o]));
                                        }
                                        else
                                        {
                                            circuitNumbers.Add(Tuple.Create(loadId + i.ToString(), TextDic[o]));
                                        }
                                    }
                                    return;
                                }
                            }
                        }
                        circuitNumbers.Add(Tuple.Create(info, TextDic[o]));
                    });

                    if ((crossPoints.First() - crossPoints.Last()).GetNormal().DotProduct(direction) < 0.1)
                    {
                        crossPoints = crossPoints.OrderByDescending(x => x.Y).ToList();
                    }
                    else
                    {
                        crossPoints = crossPoints.OrderBy(x => (x - startPoint).DotProduct(direction)).ToList();
                    }
                    for (var j = 0; j < crossPoints.Count && j < circuitNumbers.Count; j++)
                    {
                        PointDic.Add(ToDbPoint(crossPoints[j]),
                            Tuple.Create(new List<string> { circuitNumbers[j].Item1 }, circuitNumbers[j].Item2));
                    }
                }
            });
            LineIndex.Update(new DBObjectCollection(), removeLines.ToCollection());

            markBlocks.ForEach(o =>
            {
                if (o.Value.EffectiveName.Equals(ThPDSCommon.LOAD_DETAILS))
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
                var basePoint = new Point3d();
                if (ThMEPTCHService.IsTCHWireDim2(o.Entity))
                {
                    basePoint = objs.OfType<Line>().First().GetCenter();
                }
                else if (ThMEPTCHService.IsTCHMULTILEADER(o.Entity))
                {
                    basePoint = objs.OfType<Polyline>().First().GetCenter();
                }
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
                result.Texts.Add(Filter(o.Item1.TextString));
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
                        Texts = Filter(new List<string> { o }),
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
            var points = PointIndex.SelectWindowPolygon(frame);
            if (points.Count > 0)
            {
                points.OfType<DBPoint>().ForEach(p =>
                {
                    result.Texts.AddRange(PointDic[p].Item1);
                    result.ObjectIds.Add(PointDic[p].Item2);
                });
            }
            var textLeads = new List<Line>();
            var doSearch = true;
            SearchMarkLine(frame, textLeads);
            var tolerence = 3.0 * Math.PI / 180.0;
            textLeads.ForEach(o =>
            {
                var newFrame = ThPDSBufferService.Buffer(o, 200.0);//（Buffer200）+文字
                var textCollection = TextIndex.SelectCrossingPolygon(newFrame);
                if (textCollection.Count > 0)
                {
                    textCollection.OfType<DBText>().ForEach(t =>
                    {
                        // 只取与引线方向相同的文字
                        var rad = t.Rotation % Math.PI;
                        var lineAngle = o.Angle % Math.PI;
                        if (Math.Abs(lineAngle - rad) < tolerence || Math.Abs(lineAngle - rad) > Math.PI - tolerence)
                        {
                            dbTexts.Add(Tuple.Create(t, TextDic[t]));
                            doSearch = false;
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
                        doSearch = false;
                    }
                }
            });

            if (doSearch)
            {
                var newframe = frame.Buffer(200.0).OfType<Polyline>().OrderByDescending(o => o.Length).First();
                var TextCollection = TextIndex.SelectCrossingPolygon(newframe);
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
            Filter(dbTexts);
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
                        if (Math.Abs((textCollection[i].FirstPosition - text.Item1.Position).GetNormal().DotProduct(textCollection[i].Direction)) > 0.9995)
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
                    info.Texts.AddRange(Filter(o.Item1));
                    info.ObjectIds.Add(o.Item2);
                });
                result.Add(info);
            });
            return result;
        }

        public void InfosClean(List<ThPDSTextInfo> markList)
        {
            for (var i = 0; i < markList.Count; i++)
            {
                for (var j = 0; j < markList[i].Texts.Count; j++)
                {
                    if (markList[i].Texts[j].Contains("~"))
                    {
                        var numberRegex = new Regex(@"[0-9]+~[A-Z]*[0-9]+");
                        var numberMatch = numberRegex.Match(markList[i].Texts[j]);
                        if (numberMatch.Success)
                        {
                            var loadId = markList[i].Texts[j].Replace(numberMatch.Value, "");
                            var first = new Regex(@"[0-9]+");
                            var firstMatch = first.Match(numberMatch.Value);
                            var secondMatch = firstMatch.NextMatch();
                            if (firstMatch.Success && secondMatch.Success)
                            {
                                var start = Convert.ToInt32(firstMatch.Value);
                                var end = Convert.ToInt32(secondMatch.Value);
                                markList[i].Texts[j] = JointString(loadId, start);
                                for (var k = start + 1; k <= end; k++)
                                {
                                    markList.Add(new ThPDSTextInfo(new List<string> { JointString(loadId, k) }, markList[i].ObjectIds));
                                }
                            }
                        }
                    }
                }
            }
        }

        private string JointString(string str, int num)
        {
            if (num > 9)
            {
                return str + num.ToString();
            }
            else
            {
                return str + "0" + num.ToString();
            }
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

        private string Filter(string info)
        {
            return info.Replace(" ", "");
        }

        private void Filter(List<Tuple<DBText, ObjectId>> dbTexts)
        {
            var results = new List<Tuple<DBText, ObjectId>>();
            dbTexts.ForEach(o =>
            {
                var objectIds = results.Select(t => t.Item2).ToList();
                if (!objectIds.Contains(o.Item2))
                {
                    results.Add(o);
                }
            });
            dbTexts.Clear();
            results.ForEach(o => dbTexts.Add(o));
        }
    }
}
