using ThCADCore.NTS;
using ThMEPEngineCore.Interface;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThNTSSimplifyService : ISimplify
    {
        public Polyline DPSimplify(Polyline pline, double distanceTolerance)
        {
            return pline.DPSimplify(distanceTolerance);
        }

        public Polyline TPSimplify(Polyline pline, double distanceTolerance)
        {
            return pline.TPSimplify(distanceTolerance);
        }

        public Polyline VWSimplify(Polyline pline, double distanceTolerance)
        {
            return pline.VWSimplify(distanceTolerance);
        }
    }
}
