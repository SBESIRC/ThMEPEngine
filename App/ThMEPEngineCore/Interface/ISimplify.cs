using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Interface
{
    internal interface ISimplify
    {
        Polyline VWSimplify(Polyline pline, double distanceTolerance);
        Polyline DPSimplify(Polyline pline, double distanceTolerance);
        Polyline TPSimplify(Polyline pline, double distanceTolerance);
    }
}
