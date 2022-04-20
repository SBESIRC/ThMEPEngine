using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

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

        public ThPDSInfoModifyEngine(List<ThPDSNodeMap> nodeMapList, List<ThPDSEdgeMap> edgeMapList, ProjectGraph projectGraph)
        {
            NodeMapList = nodeMapList;
            EdgeMapList = edgeMapList;
            ProjectGraph = projectGraph;
        }

        public void Execute()
        {
            var dm = Application.DocumentManager;
            foreach (Document doc in dm)
            {
                //var fileName = doc.Name.Split('\\').Last();
                //if (FireCompartmentParameter.ChoiseFileNames.Count(file => string.Equals(fileName, file)) != 1)
                //{
                //    continue;
                //}

                using (var docLock = doc.LockDocument())
                using (var acad = AcadDatabase.Use(doc.Database))
                {
                    var referenceDWG = doc.Database.OriginalFileName.Split("\\".ToCharArray()).Last();
                    var nodeMap = NodeMapList.FirstOrDefault(o => o.ReferenceDWG.Equals(referenceDWG));
                    var edgeMap = EdgeMapList.FirstOrDefault(o => o.ReferenceDWG.Equals(referenceDWG));
                    if (nodeMap.IsNull() || ProjectGraph.IsNull())
                    {
                        return;
                    }

                    ProjectGraph.Vertices.ForEach(o =>
                    {
                        // 节点负载编号更新
                        if (o.Type == PDSNodeType.Load)
                        {
                            o.Tag = new ThPDSProjectGraphNodeDataTag
                            {
                                TagD = true,
                                TarD = "_潜水泵",
                                TagP = true,
                                TarP = new ThInstalledCapacity
                                {
                                    IsDualPower = false,
                                    LowPower = 0,
                                    HighPower = 15.5,
                                },
                            };
                        }

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
                                    InfoModify(acad, id, sourceLoadID, idTag.ChangedID);
                                });
                            }
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
                                    InfoModify(acad, id, sourceHighPower.ToString(), dataTag.TarP.HighPower.ToString());
                                });
                            }

                            var sourceLowPower = sourceNode[0].Key.Loads[0].InstalledCapacity.LowPower;
                            if (sourceLowPower != 0)
                            {
                                sourceNode[0].Value.ForEach(id =>
                                {
                                    InfoModify(acad, id, sourceLowPower.ToString(), dataTag.TarP.LowPower.ToString());
                                });
                            }
                        }
                        if (dataTag.TagD)
                        {
                            var sourceDescription = sourceNode[0].Key.Loads[0].ID.Description;
                            sourceNode[0].Value.ForEach(id =>
                            {
                                InfoModify(acad, id, sourceDescription, dataTag.TarD);
                            });
                        }
                        if (dataTag.TagF)
                        {
                            sourceNode[0].Value.ForEach(id =>
                            {
                                var entity = acad.Element<Entity>(id, true);
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

                    ProjectGraph.Edges.ForEach(o =>
                    {
                        o.Tag = new ThPDSProjectGraphEdgeIdChangeTag
                        {
                            ChangeFrom = true,
                            ChangedLastCircuitID = "1B-B3ACq02",
                        };
                        if (o.Tag is ThPDSProjectGraphEdgeIdChangeTag sourcePanelTag)
                        {
                            var sourceEdge = edgeMap.EdgeMap
                                .Where(edge => edge.Key.Circuit.CircuitUID.Equals(o.Circuit.CircuitUID))
                                .ToList();
                            if (sourceEdge.Count != 1)
                            {
                                return;
                            }
                            if (sourcePanelTag.ChangeFrom)
                            {
                                var sourceCircuitNumber = sourceEdge[0].Key.Circuit.ID.CircuitNumber.Last();
                                sourceEdge[0].Value.ForEach(id =>
                                {
                                    InfoModify(acad, id, sourceCircuitNumber, sourcePanelTag.ChangedLastCircuitID);
                                });
                            }
                        }
                    });
                }
            }
        }

        private void InfoModify(AcadDatabase acad, ObjectId id, string sourceInfo, string targetInfo)
        {
            if (sourceInfo.Equals(targetInfo))
            {
                return;
            }
            var entity = acad.Element<Entity>(id, true);
            if (entity is DBText text)
            {
                if (text.TextString.Contains(sourceInfo))
                {
                    text.TextString = StringReplace(text.TextString, sourceInfo, targetInfo);
                }
            }
            else if (entity is MText mText)
            {
                var results = new List<Entity>();
                if (MTextInfoModify(mText, sourceInfo, targetInfo, results))
                {
                    mText.Erase();
                    results.OfType<Entity>().ForEach(o => acad.ModelSpace.Add(o));
                }
            }
            else if (entity is MLeader mLeader)
            {
                MutiEntityInfoModify(acad, mLeader, sourceInfo, targetInfo);
            }
            else if (entity is Table table)
            {
                MutiEntityInfoModify(acad, table, sourceInfo, targetInfo);
            }
            else if (ThMEPTCHService.IsTCHWireDim2(entity))
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
                    entity.Erase();
                    results.OfType<Entity>().ForEach(o => acad.ModelSpace.Add(o));
                }
            }
            else if (entity is BlockReference block)
            {
                if (block.Name.Contains(ThPDSCommon.LOAD_LABELS))
                {
                    var attributes = id.GetAttributesInBlockReference();
                    var dictionary = new Dictionary<string, string>();
                    attributes.ForEach(o =>
                    {
                        if (o.Value.Contains(sourceInfo))
                        {
                            dictionary.Add(o.Key, StringReplace(o.Value, sourceInfo, targetInfo));
                        }
                        else
                        {
                            dictionary.Add(o.Key, o.Value);
                        }
                    });
                    id.UpdateAttributesInBlock(dictionary);
                }
            }
        }

        private bool MTextInfoModify(MText mText, string sourceInfo, string targetInfo, List<Entity> results)
        {
            var contains = false;
            var texts = new DBObjectCollection();
            mText.Explode(texts);
            foreach (DBText t in texts)
            {
                if (t.TextString.Contains(sourceInfo))
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

        private void MutiEntityInfoModify(AcadDatabase acad, Entity entity, string sourceInfo, string targetInfo)
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
                    if (MTextInfoModify(mt, sourceInfo, targetInfo, texts))
                    {
                        contains = true;
                    }
                    results.AddRange(texts);
                });
            }
            if (contains)
            {
                entity.Erase();
                results.OfType<Entity>().ForEach(o => acad.ModelSpace.Add(o));
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
                        && CharVaild(textString[index + sourceInfo.Count() + 1]))
                    {
                        break;
                    }
                    else
                    {
                        index = textString.IndexOf(sourceInfo, index + 1);
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
    }
}
