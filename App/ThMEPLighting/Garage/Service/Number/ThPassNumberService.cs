using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.Number
{
    internal class ThPassNumberService
    {
        private ThFirstSecondPairService firstSecondPairService;
        private int LoopNumber { get; set; }
        private double DoubleRowOffsetDis { get; set; }
        private List<ThLightEdge> FirstEdges { get; set; }
        private List<ThLightEdge> SecondEdges { get; set; }
        private bool IsDoubleRow { get; set; }

        public ThPassNumberService(List<ThLightEdge> firstEdges, List<ThLightEdge> secondEdges, int loopNumber, double doubleRowOffsetDis,
            bool isDoubleRow)
        {
            LoopNumber = loopNumber;
            FirstEdges = firstEdges;
            SecondEdges = secondEdges;
            IsDoubleRow = isDoubleRow;
            DoubleRowOffsetDis = doubleRowOffsetDis;
        }
        public void Pass()
        {
            // 建立1、2的配对查询
            this.firstSecondPairService = new ThFirstSecondPairService(
                FirstEdges.Select(e => e.Edge).ToList(),
                SecondEdges.Select(e => e.Edge).ToList(),
                DoubleRowOffsetDis);

            // 把1号线编号传递到2号线
            PassFirstToSecond();
        }

        private List<ThLightEdge> PassFirstToSecond()
        {
            var results = new List<ThLightEdge>();
            var firstLines = FirstEdges.Select(o => o.Edge).ToList();
            var firstSecondDict = firstSecondPairService.FindSecondLines(firstLines);
            firstSecondDict.ForEach(o =>
            {
                var firstEdge = FirstEdges[firstLines.IndexOf(o.Key)];
                var secondPairEdges = SecondEdges.Where(e => o.Value.Contains(e.Edge)).ToList();
                results.AddRange(secondPairEdges);
                // 把1号线编号传递到2号线
                PassFirstNumberToSecond(firstEdge, secondPairEdges);
            });
            return results;
        }

        /// <summary>
        /// 把1号布灯的编号传递到2号边
        /// </summary>
        /// <param name="firstEdge">已编号的1号边</param>
        /// <param name="secondEdge">对应的二号边</param>
        /// <param name="loopNumber">回路编号</param>
        private void PassFirstNumberToSecond(ThLightEdge firstEdge, List<ThLightEdge> secondEdges)
        {
            int loopCharLength = LoopNumber.GetLoopCharLength();
            firstEdge.LightNodes.ForEach(m =>
            {
                if (!string.IsNullOrEmpty(m.Number) && m.GetIndex() != -1)
                {
                    foreach (var secondEdge in secondEdges)
                    {
                        secondEdge.Direction = firstEdge.Direction; // 传递编号时，把1号边的方向传给2号边
                        var position = m.Position.GetProjectPtOnLine(
                            secondEdge.Edge.StartPoint, secondEdge.Edge.EndPoint);
                        if (position.IsPointOnCurve(secondEdge.Edge, 5.0)) // 5.0 
                        {
                            var findSecondNodes = secondEdge.LightNodes
                            .Where(k => k.Position.DistanceTo(position) <= 1.0)
                            .OrderBy(k => k.Position.DistanceTo(position));

                            if (findSecondNodes.Count() > 0)
                            {
                                var secondLightIndex = m.GetIndex() + 1;
                                var secondNode = findSecondNodes.First();
                                secondNode.Number = ThNumberService.FormatNumber(secondLightIndex, loopCharLength);
                            }
                            break;
                        }
                    }
                }
            });
        }
    }
}