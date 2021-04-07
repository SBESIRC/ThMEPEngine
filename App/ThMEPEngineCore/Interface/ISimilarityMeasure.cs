using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Interface
{
    interface ISimilarityMeasure
    {
        double SimilarityMeasure(Polyline first, Polyline second);
    }
}
