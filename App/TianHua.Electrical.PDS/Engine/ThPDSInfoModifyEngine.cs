using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using AcHelper;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using ProjectGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSInfoModifyEngine
    {
        private List<ThPDSNodeMap> NodeMapList;
        private List<ThPDSEdgeMap> EdgeMapList;
        private ProjectGraph ProjectGraph;
        private DBObjectCollection Revclouds;
        private List<ThPDSProjectGraphNode> NodeList;
        private List<ThPDSProjectGraphEdge> EdgeList;

        public ThPDSInfoModifyEngine(List<ThPDSNodeMap> nodeMapList, List<ThPDSEdgeMap> edgeMapList, ProjectGraph projectGraph)
        {
            NodeMapList = nodeMapList;
            EdgeMapList = edgeMapList;
            ProjectGraph = projectGraph;
            NodeList = ProjectGraph.Vertices.ToList();
            EdgeList = ProjectGraph.Edges.ToList();
        }

        public ThPDSInfoModifyEngine(List<ThPDSNodeMap> nodeMapList, List<ThPDSEdgeMap> edgeMapList, ProjectGraph projectGraph,
            ThPDSProjectGraphNode projectNode)
        {
            NodeMapList = nodeMapList;
            EdgeMapList = edgeMapList;
            ProjectGraph = projectGraph;
            NodeList = ProjectGraph.Vertices.Where(o => o.Load.ID.LoadID.Equals(projectNode.Load.ID.LoadID)).ToList();
            EdgeList = new List<ThPDSProjectGraphEdge>();
        }

        public ThPDSInfoModifyEngine(List<ThPDSNodeMap> nodeMapList, List<ThPDSEdgeMap> edgeMapList, ProjectGraph projectGraph,
           ThPDSProjectGraphEdge projectEdge)
        {
            NodeMapList = nodeMapList;
            EdgeMapList = edgeMapList;
            ProjectGraph = projectGraph;
            NodeList = new List<ThPDSProjectGraphNode>();
            // 若在ui中修改回路编号，那么应如何确定其原来对应的边
            EdgeList = ProjectGraph.Edges.Where(e => e.Equals(projectEdge)).ToList();
        }

        public void InfoModify()
        {
            // TODO：暂时只支持当前文档
            using (var docLock = Active.Document.LockDocument())
            using (var activeDb = AcadDatabase.Active())
            {
                var referenceDWG = Path.GetFileNameWithoutExtension(activeDb.Database.Filename);
                var nodeMap = NodeMapList.FirstOrDefault(o => o.ReferenceDWG.Equals(referenceDWG));
                var edgeMap = EdgeMapList.FirstOrDefault(o => o.ReferenceDWG.Equals(referenceDWG));
                if (nodeMap.IsNull() || ProjectGraph.IsNull())
                {
                    return;
                }
                Revclouds = new DBObjectCollection();

                NodeList.ForEach(o =>
                {
                    var sourceNode = nodeMap.NodeMap
                        .Where(node => node.Key.Loads[0].LoadUID.Equals(o.Load.LoadUID))
                        .ToList();
                    if (sourceNode.Count != 1)
                    {
                        return;
                    }

                    if (o.Tag is ThPDSProjectGraphNodeIdChangeTag idTag)
                    {
                        if (idTag.ChangeFrom)
                        {
                            var sourceLoadID = sourceNode[0].Key.Loads[0].ID.LoadID;
                            sourceNode[0].Value.ForEach(id =>
                            {
                                InfoModify(activeDb, id, sourceLoadID, idTag.ChangedID);
                            });
                        }
                        return;
                    }

                    // 节点负载数据更新
                    var dataTag = new ThPDSProjectGraphNodeDataTag();
                    if (o.Tag is ThPDSProjectGraphNodeDataTag tag)
                    {
                        dataTag = tag;
                    }
                    else if (o.Tag is ThPDSProjectGraphNodeCompositeTag compositeTag)
                    {
                        dataTag = compositeTag.DataTag;
                    }

                    if (dataTag.TagP)
                    {
                        var sourceHighPower = sourceNode[0].Key.Loads[0].InstalledCapacity.HighPower;
                        if (sourceHighPower != 0)
                        {
                            sourceNode[0].Value.ForEach(id =>
                            {
                                InfoModify(activeDb, id, sourceHighPower.ToString(), dataTag.TarP.HighPower.ToString(), true);
                            });
                        }

                        var sourceLowPower = sourceNode[0].Key.Loads[0].InstalledCapacity.LowPower;
                        if (sourceLowPower != 0)
                        {
                            sourceNode[0].Value.ForEach(id =>
                            {
                                InfoModify(activeDb, id, sourceLowPower.ToString(), dataTag.TarP.LowPower.ToString(), true);
                            });
                        }
                    }
                    if (dataTag.TagD)
                    {
                        var sourceDescription = sourceNode[0].Key.Loads[0].ID.Description;
                        sourceNode[0].Value.ForEach(id =>
                        {
                            InfoModify(activeDb, id, sourceDescription, dataTag.TarD);
                        });
                    }
                    if (dataTag.TagF)
                    {
                        sourceNode[0].Value.ForEach(id =>
                        {
                            var entity = activeDb.Element<Entity>(id, true);
                            if (entity is BlockReference block)
                            {
                                if (block.Name.Contains(ThPDSCommon.LOAD_LABELS))
                                {
                                    if (dataTag.TarF)
                                    {
                                        id.SetDynBlockValue("电源类别", ThPDSCommon.PROPERTY_VALUE_FIRE_POWER);
                                    }
                                    else
                                    {
                                        id.SetDynBlockValue("电源类别", ThPDSCommon.NON_PROPERTY_VALUE_FIRE_POWER);
                                    }
                                }
                            }
                        });
                    }
                });

                EdgeList.ForEach(o =>
                {
                    var sourceEdge = edgeMap.EdgeMap
                    .Where(edge => edge.Key.Circuit.CircuitUID.Equals(o.Circuit.CircuitUID))
                    .ToList();
                    if (sourceEdge.Count != 1)
                    {
                        return;
                    }

                    if (o.Tag is ThPDSProjectGraphEdgeIdChangeTag sourcePanelTag)
                    {
                        if (sourcePanelTag.ChangeFrom)
                        {
                            var sourceCircuitNumber = sourceEdge[0].Key.Circuit.ID.CircuitNumber;
                            sourceEdge[0].Value.ForEach(id =>
                            {
                                InfoModify(activeDb, id, sourceCircuitNumber, sourcePanelTag.ChangedLastCircuitID);
                            });
                        }
                    }
                });
            }
        }

        public void GenerateRevcloud()
        {
            if (Revclouds.Count > 0)
            {
                Revclouds = Revclouds.ToNTSMultiPolygon().Union().ToDbCollection();
                Active.Editor.InsertRevcloud(Revclouds, ThPDSCommon.AI_POWR_AUXL1);
            }
        }

        private void InfoModify(AcadDatabase activeDb, ObjectId id, string sourceInfo, string targetInfo, bool isPower = false)
        {
            if (string.IsNullOrEmpty(sourceInfo) || sourceInfo.Equals(targetInfo))
            {
                return;
            }
            var entity = activeDb.Element<Entity>(id, true);
            if (entity is DBText text)
            {
                if ((!isPower || MatchPower(text.TextString)) && text.TextString.Contains(sourceInfo))
                {
                    if (string.IsNullOrEmpty(targetInfo))
                    {
                        if (text.Bounds.HasValue)
                        {
                            Revclouds.Add(text.GeometricExtents.ToRectangle());
                        }
                        text.TextString = StringReplace(text.TextString, sourceInfo, targetInfo);
                    }
                    else
                    {
                        text.TextString = StringReplace(text.TextString, sourceInfo, targetInfo);
                        if (text.Bounds.HasValue)
                        {
                            Revclouds.Add(text.GeometricExtents.ToRectangle());
                        }
                    }
                }
            }
            else if (entity is MText mText)
            {
                var results = new List<Entity>();
                if (MTextInfoModify(mText, sourceInfo, targetInfo, results, isPower))
                {
                    Revclouds.Add(mText.GeometricExtents.ToRectangle());
                    mText.Erase();
                    results.OfType<Entity>().ForEach(o => activeDb.ModelSpace.Add(o));
                }
            }
            else if (entity is MLeader mLeader)
            {
                MutiEntityInfoModify(activeDb, mLeader, sourceInfo, targetInfo, isPower);
            }
            else if (entity is Table table)
            {
                MutiEntityInfoModify(activeDb, table, sourceInfo, targetInfo, isPower);
            }
            else if (ThMEPTCHService.IsTCHWireDim2(entity) || ThMEPTCHService.IsTCHMULTILEADER(entity))
            {
                var contains = false;
                var results = new List<Entity>();
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                var texts = objs.OfType<Entity>().Where(o => ThMEPTCHService.IsTCHText(o)).ToList();
                objs.OfType<Entity>().Except(texts).ForEach(o => results.Add(o));
                texts.ForEach(o =>
                {
                    var t = new DBObjectCollection();
                    o.Explode(t);
                    var thisText = t.OfType<DBText>().First();
                    if (thisText.TextString.Contains(sourceInfo))
                    {
                        contains = true;
                        thisText.TextString = StringReplace(thisText.TextString, sourceInfo, targetInfo);
                    }
                    results.Add(thisText);
                });
                if (contains)
                {
                    Revclouds.Add(entity.GeometricExtents.ToRectangle());
                    entity.Erase();
                    results.OfType<Entity>().ForEach(o => activeDb.ModelSpace.Add(o));
                }
            }
            else if (entity is BlockReference block)
            {
                var blockName = block.GetBlockName();
                if (blockName.Contains(ThPDSCommon.LOAD_LABELS))
                {
                    var attributes = id.GetAttributesInBlockReference();
                    var dictionary = new Dictionary<string, string>();
                    var contains = false;
                    attributes.ForEach(o =>
                    {
                        if (o.Value.Contains(sourceInfo))
                        {
                            contains = true;
                            dictionary.Add(o.Key, StringReplace(o.Value, sourceInfo, targetInfo));
                        }
                        else
                        {
                            dictionary.Add(o.Key, o.Value);
                        }
                    });
                    if (contains)
                    {
                        Revclouds.Add(block.GeometricExtents.ToRectangle());
                        id.UpdateAttributesInBlock(dictionary);
                    }
                }
            }
        }

        private bool MTextInfoModify(MText mText, string sourceInfo, string targetInfo, List<Entity> results, bool isPower)
        {
            var contains = false;
            var texts = new DBObjectCollection();
            mText.Explode(texts);
            foreach (DBText t in texts)
            {
                if ((!isPower || MatchPower(t.TextString)) && t.TextString.Contains(sourceInfo))
                {
                    contains = true;
                    t.TextString = StringReplace(t.TextString, sourceInfo, targetInfo);
                }
                results.Add(t);
            }

            return contains;
        }

        private bool MTextInfoModify(MText mText, string sourceInfo, string targetInfo, List<Entity> results,
            Line direction)
        {
            var contains = false;
            var texts = new DBObjectCollection();
            mText.Explode(texts);
            foreach (DBText t in texts)
            {
                if (t.TextString.Contains(sourceInfo))
                {
                    var sourcePerimeter = t.GeometricExtents.ToRectangle().Length;
                    contains = true;
                    t.TextString = StringReplace(t.TextString, sourceInfo, targetInfo);
                    var targetPerimeter = t.GeometricExtents.ToRectangle().Length;
                    var difference = (targetPerimeter - sourcePerimeter) / 2.0;
                    if ((t.Position - direction.EndPoint).DotProduct(direction.LineDirection()) > 500.0)
                    {
                        t.TransformBy(Matrix3d.Displacement(difference * direction.LineDirection()));
                    }
                }
                results.Add(t);
            }

            return contains;
        }

        private void MutiEntityInfoModify(AcadDatabase activeDb, Entity entity, string sourceInfo, string targetInfo, bool isPower)
        {
            var contains = false;
            var results = new List<Entity>();
            var objs = new DBObjectCollection();
            entity.Explode(objs);
            var mTexts = objs.OfType<MText>().ToList();
            objs.OfType<Entity>().Except(mTexts).ForEach(o =>
            {
                results.Add(o);
            });
            if (entity is MLeader mLeader)
            {
                var direction = objs.OfType<Line>().First();
                mTexts.ForEach(mt =>
                {
                    var texts = new List<Entity>();
                    if (MTextInfoModify(mt, sourceInfo, targetInfo, texts, direction))
                    {
                        contains = true;
                    }
                    results.AddRange(texts);
                });
            }
            else
            {
                mTexts.ForEach(mt =>
                {
                    var texts = new List<Entity>();
                    if (MTextInfoModify(mt, sourceInfo, targetInfo, texts, isPower))
                    {
                        contains = true;
                    }
                    results.AddRange(texts);
                });
            }
            if (contains)
            {
                Revclouds.Add(entity.GeometricExtents.ToRectangle());
                entity.Erase();
                results.OfType<Entity>().ForEach(o => activeDb.ModelSpace.Add(o));
            }
        }

        private bool CharVaild(char c)
        {
            return c != '.' && (c < '0' || c > '9');
        }

        private string StringReplace(string textString, string sourceInfo, string targetInfo)
        {
            if (double.TryParse(sourceInfo, out _))
            {
                var xIndex = textString.IndexOf("x");
                var XIndex = textString.IndexOf("X");
                var startIndex = 0;
                if (xIndex != -1)
                {
                    startIndex = xIndex;
                }
                else if (XIndex != -1)
                {
                    startIndex = XIndex;
                }

                var index = textString.IndexOf(sourceInfo, startIndex);
                while (index != -1
                    && index - 1 >= 0
                    && index + sourceInfo.Count() + 1 <= textString.Count())
                {
                    if (CharVaild(textString[index - 1])
                        && index + sourceInfo.Count() + 1 < textString.Count()
                        && CharVaild(textString[index + sourceInfo.Count() + 1]))
                    {
                        break;
                    }
                    else
                    {
                        index = textString.IndexOf(sourceInfo, index + 1);
                    }
                }

                if (MatchPower(textString))
                {
                    var powerIndex1 = textString.IndexOf("kW");
                    var powerIndex2 = textString.IndexOf("kw");
                    var powerIndex3 = textString.IndexOf("KW");
                    var powerIndex4 = textString.IndexOf("Kw");
                    var powerIndex = powerIndex1 != -1 ? powerIndex1 : (powerIndex2 != -1 ? powerIndex2 : (powerIndex3 != -1 ? powerIndex3 : powerIndex4));
                    if (powerIndex != -1)
                    {
                        index = textString.LastIndexOfAny(sourceInfo.ToCharArray(), powerIndex);
                    }
                }

                if (index != -1)
                {
                    textString = textString.Remove(index, sourceInfo.Count());
                    textString = textString.Insert(index, targetInfo);
                }
            }
            else
            {
                textString = textString.Replace(sourceInfo, targetInfo);
            }

            return textString;
        }

        private bool MatchPower(string str)
        {
            var check = "[kK]?[wW]";
            var regex = new Regex(@check);
            var match = regex.Match(str);
            if (match.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
