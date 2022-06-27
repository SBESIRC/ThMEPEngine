using System.Linq;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
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
                    if (CanAcceptChildren(targetItem))
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
                        // 数据更新
                        var targetNode = GetProjectGraphNode(sourceItem, _graph);
                        var oldsourceNode = GetProjectGraphNode(sourceItem.Parent, _graph);
                        var newSourceNode = GetProjectGraphNode(targetItem.Parent, _graph);
                        ThPDSProjectGraphService.DeleteCircuit(_graph, oldsourceNode, targetNode);
                        ThPDSProjectGraphService.SpecifyConnectionCircuit(_graph, newSourceNode, targetNode);

                        // 界面更新
                        sourceItem.Parent.DataList.Remove(sourceItem);
                        targetItem.Parent.DataList.Add(sourceItem);
                        sourceItem.Parent = targetItem.Parent;
                    }
                }
                else
                {
                    if (CanAcceptChildren(targetItem))
                    {
                        // 数据更新
                        var targetNode = GetProjectGraphNode(sourceItem, _graph);
                        var oldsourceNode = GetProjectGraphNode(sourceItem.Parent, _graph);
                        var newSourceNode = GetProjectGraphNode(targetItem, _graph);
                        ThPDSProjectGraphService.DeleteCircuit(_graph, oldsourceNode, targetNode);
                        ThPDSProjectGraphService.SpecifyConnectionCircuit(_graph, newSourceNode, targetNode);

                        // 界面更新
                        sourceItem.Parent.DataList.Remove(sourceItem);
                        targetItem.DataList.Add(sourceItem);
                        sourceItem.Parent = targetItem;
                    }
                }
            }
        }

        private bool CanAcceptChildren(ThPDSCircuitGraphTreeModel item)
        {
            var node = GetProjectGraphNode(item, _graph);
            return !item.IsRoot && !node.IsTerminalPanel();
        }
        #endregion

        private ThPDSProjectGraphNode GetProjectGraphNode(ThPDSCircuitGraphTreeModel item, PDSGraph graph)
        {
            return graph.Vertices.FirstOrDefault(o => o.Load.LoadUID.Equals(item.NodeUID));
        }
    }
}