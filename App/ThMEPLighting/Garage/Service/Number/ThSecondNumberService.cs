using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using Dreambuild.AutoCAD;

namespace ThMEPLighting.Garage.Service.Number
{
    internal class ThSecondNumberService
    {
        private int LoopNumber { get; set; }
        private List<ThLightEdge> SecondEdges { get; set; }
        private ThQueryLineService LineQuery { get; set; }
        private int LoopCharLength { get; set; }
        private int DefaultStartNumber { get; set; }
        public ThSecondNumberService(List<ThLightEdge> secondEdges,int loopNumber,int defaultStartNumber)
        {
            LoopNumber = loopNumber;
            SecondEdges = secondEdges;
            LoopCharLength = loopNumber.GetLoopCharLength();
            DefaultStartNumber = defaultStartNumber;
            LineQuery = ThQueryLineService.Create(secondEdges.Select(o => o.Edge).ToList());
        }
        public void Number()
        {
            NumberBySameEdge(); // 先根据边的方向和边上已编号的灯来给此边上未编号的边编号
            NumberByNeibourEdge(); // 根据邻边的灯来编号
            NumberByDefaultNumber(); // 对于找不到的，用默认编号
        }

        private void NumberBySameEdge()
        {
            SecondEdges
                .Where(o => HasNumberedLight(o.LightNodes))
                .Where(o => HasUnNumberedLight(o.LightNodes))
                .ForEach(o =>
                {
                    if (o.IsSameDirection)
                    {
                        NumberFromHead(o.LightNodes);
                    }
                    else
                    {
                        NumberFromTail(o.LightNodes);
                    }
                });
        }

        private void NumberByNeibourEdge()
        {
            // 对于边上没有任何编号的灯，通过邻近的边来传递编号
            SecondEdges
                .Where(o => !HasNumberedLight(o.LightNodes))
                .ForEach(current =>
                 {
                     bool isFinded = false;
                     var nextNeibour = FindNeibour(current, current.EndPoint);
                     if (nextNeibour != null &&
                         HasNumberedLight(nextNeibour.LightNodes) &&
                         IsForward(current, nextNeibour))
                     {
                         var nextClosetNode = nextNeibour.GetRecentNode(current.EndPoint);
                         if (!nextClosetNode.IsEmpty)
                         {
                             isFinded = true;
                             var closetIndex = nextClosetNode.GetIndex();
                             var startIndex = PreIndex(closetIndex);
                             NumberFromTail(current.LightNodes, current.EndPoint, startIndex);
                         }
                     }
                     if (!isFinded)
                     {
                         var prevNeibour = FindNeibour(current, current.StartPoint);
                         if (prevNeibour != null &&
                             HasNumberedLight(prevNeibour.LightNodes) &&
                             IsForward(prevNeibour, current))
                         {
                             var prevClosetNode = prevNeibour.GetRecentNode(current.EndPoint);
                             if (!prevClosetNode.IsEmpty)
                             {
                                 var closetIndex = prevClosetNode.GetIndex();
                                 var nextIndex = NextIndex(closetIndex);
                                 NumberFromHead(current.LightNodes, current.EndPoint, nextIndex);
                             }
                         }
                     }
                 });
        }

        private void NumberByDefaultNumber()
        {
            SecondEdges.ForEach(m =>
            {
                var hasUnNumbered = HasUnNumberedLight(m.LightNodes);
                if(hasUnNumbered)
                {
                    m.LightNodes.ForEach(n =>
                    {
                        if(n.IsEmpty)
                        {
                            n.Number = ThNumberService.FormatNumber(this.DefaultStartNumber, LoopCharLength);
                        }
                    });
                }
            });
        }

        private bool IsForward(ThLightEdge preEdge,ThLightEdge nextEdge)
        {
            var preEndPt = preEdge.EndPoint;
            var nextStartPt = nextEdge.StartPoint;
            return preEndPt.DistanceTo(nextStartPt) <= 
                ThGarageLightCommon.RepeatedPointDistance;
        }

        private ThLightEdge FindNeibour(ThLightEdge edge,Point3d pt)
        {
            var neibours = LineQuery.Query(pt,ThGarageLightCommon.RepeatedPointDistance);
            neibours.Remove(edge.Edge);
            if(neibours.Count==1)
            {
                return FindEdge(neibours[0]);
            }
            return null;
        }

        private ThLightEdge FindEdge(Line line)
        {
            var edges = SecondEdges.Select(o => o.Edge).ToList();
            int index = edges.IndexOf(line);
            return index >= 0 ? SecondEdges[index] : null;
        }

        private void NumberFromHead(List<ThLightNode> nodes)
        {
            while(true)
            {
                bool hasEmptyNumber = false;
                for(int i=0;i<nodes.Count;i++)
                {
                    var current = nodes[i];
                    if(string.IsNullOrEmpty(current.Number))
                    {
                        hasEmptyNumber = true; 
                        continue;
                    }
                    var currentIndex = current.GetIndex();
                    if (i!=0)
                    {
                        var prev = nodes[i - 1];
                        if(string.IsNullOrEmpty(prev.Number))
                        {
                            var preIndex = PreIndex(currentIndex);
                            prev.Number = BuildNumber(preIndex);
                        }
                    }
                    if(i != nodes.Count-1)
                    {
                        var next = nodes[i + 1];
                        if (string.IsNullOrEmpty(next.Number))
                        {
                            var nextIndex = NextIndex(currentIndex);
                            next.Number = BuildNumber(nextIndex);
                        }
                    }
                }
                if(hasEmptyNumber==false)
                {
                    break;
                }
            }
        }

        private void NumberFromTail(List<ThLightNode> nodes)
        {
            while (true)
            {
                bool hasEmptyNumber = false;
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    var current = nodes[i];
                    if (string.IsNullOrEmpty(current.Number))
                    {
                        hasEmptyNumber = true;
                        continue;
                    }
                    var currentIndex = current.GetIndex();
                    if (i != nodes.Count - 1)
                    {
                        var prev = nodes[i + 1];
                        if (string.IsNullOrEmpty(prev.Number))
                        {
                            var preIndex = PreIndex(currentIndex);
                            prev.Number = BuildNumber(preIndex);
                        }
                    }
                    if (i != 0)
                    {
                        var next = nodes[i - 1];
                        if (string.IsNullOrEmpty(next.Number))
                        {
                            var nextIndex = NextIndex(currentIndex);
                            next.Number = BuildNumber(nextIndex);
                        }
                    }
                }
                if (hasEmptyNumber == false)
                {
                    break;
                }
            }
        }

        private void NumberFromHead(List<ThLightNode> nodes, Point3d headPt, int startIndex)
        {
            int currentIndex = startIndex;
            var ids = nodes.OrderBy(o => o.Position.DistanceTo(headPt)).Select(o => o.Id).ToList();
            for (int i = 0; i < ids.Count; i++)
            {
                var node = Query(nodes, ids[i]);
                nodes[i].Number = ThNumberService.FormatNumber(currentIndex, LoopCharLength);
                currentIndex = NextIndex(currentIndex);
            }
        }

        private void NumberFromTail(List<ThLightNode> nodes, Point3d tailPt, int startIndex)
        {
            var ids = nodes.OrderBy(o => o.Position.DistanceTo(tailPt)).Select(o => o.Id).ToList();
            int currentIndex = startIndex;
            for (int i = 0; i < ids.Count; i++)
            {
                var node = Query(nodes, ids[i]);
                node.Number = ThNumberService.FormatNumber(currentIndex, LoopCharLength);
                currentIndex = PreIndex(currentIndex);
            }
        }

        private ThLightNode Query(List<ThLightNode> nodes,string id)
        {
            return nodes.Where(o => o.Id == id).First();
        }

        private string BuildNumber(int index)
        {
            return ThNumberService.FormatNumber(index, LoopCharLength); ;
        }

        private int PreIndex(int index)
        {
            return ThDoubleRowLightNumber.PreIndex(LoopNumber, index, this.DefaultStartNumber);
        }

        private int NextIndex(int index)
        {
            return ThDoubleRowLightNumber.NextIndex(LoopNumber, index, this.DefaultStartNumber);
        }

        private bool HasNumberedLight(List<ThLightNode> lightNodes)
        {
            return lightNodes.Where(o=> !string.IsNullOrEmpty(o.Number)).Any();
        }
        private bool HasUnNumberedLight(List<ThLightNode> lightNodes)
        {
            return lightNodes.Where(o => string.IsNullOrEmpty(o.Number)).Any();
        }
    }
}
