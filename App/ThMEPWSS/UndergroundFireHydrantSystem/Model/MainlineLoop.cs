using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    class MainlineLoop//主线环路
    {
        private List<PointType> PointList { get; set; }

        private List<PipeDiameter> PipeList { get; set; }

        public MainlineLoop(List<PointType> pointList, List<PipeDiameter> pipeList)
        {
            PointList = pointList;
            PipeList = pipeList;
        }

        public List<PointType> GetPointList()
        {
            return PointList;
        }

        public List<PipeDiameter> GetPipeList()
        {
            return PipeList;
        }
    }
}
