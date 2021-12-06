using ThMEPLighting.Common;
using System.Collections.Generic;

namespace ThMEPLighting.Garage.Service.Number
{
    public class ThSingleRowLightNumber
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
        
        private ThSingleRowLightNumber(
            List<ThLightEdge> edges, 
            int loopNumber, 
            int startIndex, 
            int defaultStartNumber)
        {
            Edges = edges;
            LoopNumber = loopNumber;
            StartIndex = startIndex;
            DefaultStartNumber = defaultStartNumber;
            LoopCharLength = loopNumber.GetLoopCharLength();
        }
        public static ThSingleRowLightNumber Build(List<ThLightEdge> edges, int loopNumber,int startIndex,int defaultStartNumber)
        {
            var instance = new ThSingleRowLightNumber(edges, loopNumber,startIndex, defaultStartNumber);
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
                lightEdge.LightNodes[0].Number = startIndex.GetLightNumber(LoopCharLength);
                return startIndex;
            }
            else
            {
                int currentIndex = startIndex;
                for (int i = 0; i < lightEdge.LightNodes.Count; i++)
                {
                    lightEdge.LightNodes[i].Number = currentIndex.GetLightNumber(LoopCharLength);
                    currentIndex = NextIndex(LoopNumber, currentIndex, DefaultStartNumber);
                }
                return currentIndex;
            }
        }
        
        public static int NextIndex(int loopNumber , int preIndex,int defaultStartNumber)
        {
            if(preIndex==(loopNumber + defaultStartNumber - 1))
            {
                return defaultStartNumber;
            }
            else
            {
                return ++preIndex;
            }
        }
        public static int PreIndex(int loopNumber, int nextIndex,int defaultStartNumber)
        {
            if(nextIndex== defaultStartNumber)
            {
                return loopNumber + defaultStartNumber - 1;
            }
            else
            {
                return --nextIndex;
            }
        }
    }
}
