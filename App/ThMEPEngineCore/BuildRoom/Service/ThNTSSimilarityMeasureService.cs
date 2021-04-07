using ThCADCore.NTS;
using ThMEPEngineCore.Interface;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BuildRoom.Service
{
    public class ThNTSSimilarityMeasureService : ISimilarityMeasure
    {
        public double SimilarityMeasure(Polyline first, Polyline second)
        {
            return first.SimilarityMeasure(second);
        }
    }
}
