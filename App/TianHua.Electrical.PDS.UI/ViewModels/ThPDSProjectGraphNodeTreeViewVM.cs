﻿using System.Linq;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Models;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PDSGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public sealed class ThPDSProjectGraphNodeTreeViewVM : ObservableObject, IDropTarget
    {
        private readonly PDSGraph _graph;
        public ThPDSCircuitGraphTreeModel Root { get; private set; }
        public ThPDSProjectGraphNodeTreeViewVM(PDSGraph graph)
        {
            _graph = graph;
            Root = ThPDSCircuitGraphTreeBuilder.Build(graph);
        }

        #region DragDrop
        public void DragEnter(IDropInfo dropInfo)
        {
            //
        }

        public void DragOver(IDropInfo dropInfo)
        {
            var sourceItem = dropInfo.Data as ThPDSCircuitGraphTreeModel;
            var targetItem = dropInfo.TargetItem as ThPDSCircuitGraphTreeModel;
            if (sourceItem != null && targetItem != null)
            {
                if (dropInfo.KeyStates.HasFlag(DragDropKeyStates.AltKey))
                {
                    if (CanAcceptChildren(targetItem.Parent))
                    {
                        // Move to be target's sibling
                        dropInfo.Effects = DragDropEffects.Move;
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    }
                    else
                    {
                        // Invalid target
                        dropInfo.Effects = DragDropEffects.None;
                    }
                }
                else
                {
                    if (CanAcceptChildren(targetItem, sourceItem))
                    {
                        // Move to be target's child
                        dropInfo.Effects = DragDropEffects.Move;
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                    }
                    else
                    {
                        // Invalid target
                        dropInfo.Effects = DragDropEffects.None;
                    }
                }
            }
        }

        public void DragLeave(IDropInfo dropInfo)
        {
            //
        }

        public void Drop(IDropInfo dropInfo)
        {
            var sourceItem = dropInfo.Data as ThPDSCircuitGraphTreeModel;
            var targetItem = dropInfo.TargetItem as ThPDSCircuitGraphTreeModel;
            if (sourceItem != null && targetItem != null)
            {
                if (dropInfo.KeyStates.HasFlag(DragDropKeyStates.AltKey))
                {
                    if (CanAcceptChildren(targetItem.Parent))
                    {
                        var oldsourceNode = GetProjectGraphNode(sourceItem.Parent, _graph);
                        var newSourceNode = GetProjectGraphNode(targetItem.Parent, _graph);
                        if (!object.ReferenceEquals(oldsourceNode, newSourceNode))
                        {
                            var targetNode = GetProjectGraphNode(sourceItem, _graph);
                            ThPDSProjectGraphService.DeleteCircuit(_graph, oldsourceNode, targetNode);
                            ThPDSProjectGraphService.SpecifyConnectionCircuit(_graph, newSourceNode, targetNode);
                        }
                        if (!object.ReferenceEquals(oldsourceNode, newSourceNode))
                        {
                            sourceItem.Parent.DataList.Remove(sourceItem);
                            targetItem.Parent.DataList.Add(sourceItem);
                            sourceItem.Parent = targetItem.Parent;
                        }
                    }
                }
                else
                {
                    if (CanAcceptChildren(targetItem, sourceItem))
                    {
                        var oldsourceNode = GetProjectGraphNode(sourceItem.Parent, _graph);
                        var newSourceNode = GetProjectGraphNode(targetItem, _graph);
                        if (!object.ReferenceEquals(oldsourceNode, newSourceNode))
                        {
                            var targetNode = GetProjectGraphNode(sourceItem, _graph);
                            ThPDSProjectGraphService.DeleteCircuit(_graph, oldsourceNode, targetNode);
                            ThPDSProjectGraphService.SpecifyConnectionCircuit(_graph, newSourceNode, targetNode);
                        }
                        if (!object.ReferenceEquals(oldsourceNode, newSourceNode))
                        {
                            sourceItem.Parent.DataList.Remove(sourceItem);
                            targetItem.DataList.Add(sourceItem);
                            sourceItem.Parent = targetItem;
                        }
                    }
                }
            }
        }

        private bool CanAcceptChildren(ThPDSCircuitGraphTreeModel item)
        {
            var node = GetProjectGraphNode(item, _graph);
            return !item.IsRoot && !node.IsTerminalPanel();
        }

        private bool CanAcceptChildren(ThPDSCircuitGraphTreeModel targetItem, ThPDSCircuitGraphTreeModel sourceItem)
        {
            if (!CanAcceptChildren(targetItem))
            {
                return false;
            }

            var targetNode = GetProjectGraphNode(targetItem, _graph);
            var sourceNode = GetProjectGraphNode(sourceItem, _graph);
            if (!ThPDSProjectGraphService.LegalDragDrop(_graph, sourceNode, targetNode))
            {
                return false;
            }

            return true;
        }

        #endregion

        private ThPDSProjectGraphNode GetProjectGraphNode(ThPDSCircuitGraphTreeModel item, PDSGraph graph)
        {
            return graph.Vertices.FirstOrDefault(o => o.Load.LoadUID.Equals(item.NodeUID));
        }
        public void RemoveNode(string uid, string puid)
        {
            var tokills = new List<ThPDSCircuitGraphTreeModel>();
            void dfs(ThPDSCircuitGraphTreeModel md)
            {
                if (md.NodeUID == uid && md.Parent?.NodeUID == puid) tokills.Add(md);
                foreach (var ch in md.DataList) dfs(ch);
            }
            dfs(Root);
            foreach (var md in tokills)
            {
                md.Parent?.DataList?.Remove(md);
            }
        }
    }
}