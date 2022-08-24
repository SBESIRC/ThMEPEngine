using System.Linq;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPLighting.Garage.Service.Number
{
    public static class ThNumberService
    {
        public static void Number(this ThLightGraphService graph, int loopNumber, bool isSingleRowNumber, int defaultStartNumber, bool isDoubleRow)
        {
            if (graph == null || graph.Links.Count == 0)
            {
                return;
            }
            foreach (var linkPath in graph.Links)
            {
                var findStartIndex = ThFindStartIndexService.Find(graph, linkPath, loopNumber, isSingleRowNumber, defaultStartNumber);
                if (!findStartIndex.IsFind)
                {
                    findStartIndex.StartIndex = defaultStartNumber;
                }
                int startIndex = findStartIndex.StartIndex;
                if (!linkPath.Edges[0].IsDX)
                {
                    startIndex = findStartIndex.FindIndex;
                }

                Point3d start = linkPath.Start;
                for (int i = 0; i < linkPath.Edges.Count; i++)
                {
                    var edges = new List<ThLightEdge> { linkPath.Edges[i] };
                    int j = i + 1;
                    for (; j < linkPath.Edges.Count; j++)
                    {
                        var preEdge = edges.Last();
                        var nextEdge = linkPath.Edges[j];
                        if (ThGarageUtils.IsLessThan45Degree(preEdge.Edge.StartPoint, preEdge.Edge.EndPoint,
                                                             nextEdge.Edge.StartPoint, nextEdge.Edge.EndPoint))
                        {
                            edges.Add(nextEdge);
                        }
                        else
                        {
                            break;  //拐弯
                        }
                    }
                    i = j - 1;
                    //对当前直段编号

                    if (isSingleRowNumber)
                    {
                        var numberInstance = ThSingleRowLightNumber.Build(edges, loopNumber, startIndex, defaultStartNumber);
                        startIndex = numberInstance.LastIndex; //下一段的起始序号
                    }
                    else
                    {
                        var numberInstance = ThDoubleRowLightNumber.Build(edges, loopNumber, startIndex, defaultStartNumber);
                        startIndex = numberInstance.LastIndex; //下一段的起始序号
                    }
                }
            }
        }

        public static void Number1(this ThLightGraphService graph, int loopNumber, bool isSingleRowNumber, int defaultStartNumber, bool isDoubleRow)
        {
            if (graph == null || graph.Links.Count == 0)
            {
                return;
            }
            foreach (var linkPath in graph.Links)
            {
                var findStartIndex = ThFindStartIndexService.Find(graph, linkPath, loopNumber, isSingleRowNumber, defaultStartNumber);
                if (!findStartIndex.IsFind)
                {
                    findStartIndex.StartIndex = defaultStartNumber;
                }
                int startIndex = findStartIndex.StartIndex;
                if (!linkPath.Edges[0].IsDX)
                {
                    startIndex = findStartIndex.FindIndex;
                }
                if (isSingleRowNumber)
                {
                    ThSingleRowLightNumber.Build(linkPath.Edges, loopNumber, startIndex, defaultStartNumber);
                }
                else
                {
                    ThDoubleRowLightNumber.Build(linkPath.Edges, loopNumber, startIndex, defaultStartNumber);
                }
            }
        }

        public static string FormatNumber(int index, int loopCharLength)
        {
            var number = index.ToString().PadLeft(loopCharLength, '0');
            return ThGarageLightCommon.LightNumberPrefix + number;
        }
    }
}
