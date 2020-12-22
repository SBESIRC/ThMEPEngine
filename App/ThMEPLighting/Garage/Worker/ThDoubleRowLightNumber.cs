using System.Collections.Generic;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Worker
{
    public class ThDoubleRowLightNumber
    {
        private List<ThLightEdge> Edges { get; set; }
        private int LoopNumber { get; set; }
        private int LoopCharLength { get; set; }
        private int StartIndex { get; set; }
        /// <summary>
        /// 作为下一段的起始序号
        /// </summary>
        public int LastIndex { get; private set; }
        
        private ThDoubleRowLightNumber(List<ThLightEdge> edges, int loopNumber, int startIndex)
        {
            Edges = edges;
            LoopNumber = loopNumber;
            StartIndex = startIndex;
            LoopCharLength = GetLoopCharLength(loopNumber);
        }
        public static ThDoubleRowLightNumber Build(List<ThLightEdge> edges, int loopNumber,int startIndex)
        {
            var instance = new ThDoubleRowLightNumber(edges, loopNumber,startIndex);
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
        private int Build(ThLightEdge lightEdge,int startIndex)
        {
            if(!lightEdge.IsDX)
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
                    var code = currentIndex.ToString().PadLeft(LoopCharLength, '0');
                    lightEdge.LightNodes[i].Number = ThGarageLightCommon.LightNumberPrefix + code;
                    currentIndex = NextIndex(LoopNumber, currentIndex);
                }
                return currentIndex;
            }
        }
        public static int GetLoopCharLength(int loopNumber)
        {
            if (loopNumber >= 1 && loopNumber < 100)
            {
                return 2;
            }
            else
            {
                return loopNumber.ToString().Length;
            }
        }
        public static int NextIndex(int loopNumber , int preIndex)
        {
            return (preIndex + 2) % loopNumber;
        }
        public static int PreIndex(int loopNumber, int nextIndex)
        {
            return (nextIndex - 2 + loopNumber) % loopNumber;
        }
    }
}
