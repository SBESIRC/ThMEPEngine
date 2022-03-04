using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThRectangleSpecAnalysisService : ThAnalysisService
    {
        public int L { get; set; } // 长度
        public int W { get; set; } // 宽度
        public override void Analysis(Polyline rectangle)
        {
            /*
             *   --------------
             *   |            | (W)
             *   |            |
             *   --------------
             *        (L)
             */
            var lines = rectangle.ToLines();
            if(lines.Count!=4)
            {
                return;
            }
            var firstDis = lines[0].GetLineDistance();
            var secondDis= lines[1].GetLineDistance();
            if (firstDis.IsEqual(secondDis,1.0))
            {
                L = firstDis.Round();
                W = L;
            }
            else
            {
                L = firstDis > secondDis ? firstDis.Round() : secondDis.Round();
                W = firstDis < secondDis ? firstDis.Round() : secondDis.Round();
            }
        }
    }
}
