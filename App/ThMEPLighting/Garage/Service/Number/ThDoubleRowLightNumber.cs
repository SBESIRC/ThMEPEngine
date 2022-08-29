using ThMEPLighting.Common;
using System.Collections.Generic;

namespace ThMEPLighting.Garage.Service.Number
{
    public class ThDoubleRowLightNumber
    {
        private List<ThLightEdge> Edges { get; set; }
        private int LoopNumber { get; set; }
        private int LoopCharLength { get; set; }
        private int StartIndex { get; set; }
        private int DefaultStartNumber { get; set; }
        /// <summary>
        /// 作为下一段的起始序号
        /// </summary>
        public int LastIndex { get; private set; }

        private ThDoubleRowLightNumber(List<ThLightEdge> edges, int loopNumber, int startIndex, int defaultStartNumber)
        {
            Edges = edges;
            LoopNumber = loopNumber;
            StartIndex = startIndex;
            DefaultStartNumber = defaultStartNumber;
            LoopCharLength = loopNumber.GetLoopCharLength();
        }
        public static ThDoubleRowLightNumber Build(List<ThLightEdge> edges, int loopNumber, int startIndex, int defaultStartNumber)
        {
            var instance = new ThDoubleRowLightNumber(edges, loopNumber, startIndex, defaultStartNumber);
            instance.Build();
            return instance;
        }
        private void Build()
        {
            Edges.ForEach(o =>
            {
                StartIndex = Build(o, StartIndex);
            });
            LastIndex = StartIndex;
        }
        private int Build(ThLightEdge lightEdge, int startIndex)
        {
            if (!lightEdge.IsDX)
            {
                var code = startIndex.ToString().PadLeft(LoopCharLength, '0');
                lightEdge.LightNodes[0].Number = ThGarageLightCommon.LightNumberPrefix + code;
                return startIndex;
            }
            else
            {
                int currentIndex = startIndex;
                for (int i = 0; i < lightEdge.LightNodes.Count; i++)
                {
                    lightEdge.LightNodes[i].Number = ThNumberService.FormatNumber(currentIndex, LoopCharLength);
                    currentIndex = NextIndex(LoopNumber, currentIndex, DefaultStartNumber);
                }
                return currentIndex;
            }
        }
        public static int NextIndex(int loopNumber, int preIndex, int defaultStartNumber)
        {
            if (preIndex == (defaultStartNumber + (loopNumber - 1) * 2))
            {
                return defaultStartNumber;
            }
            else
            {
                return preIndex + 2;
            }
        }
        public static int PreIndex(int loopNumber, int nextIndex, int defaultStartNumber)
        {
            if (nextIndex == defaultStartNumber)
            {
                return defaultStartNumber + (loopNumber - 1) * 2;
            }
            else
            {
                return nextIndex - 2;
            }
        }
    }
}
