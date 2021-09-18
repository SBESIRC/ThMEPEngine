using Autodesk.AutoCAD.DatabaseServices;
using System;
using ThMEPEngineCore.GeojsonExtractor;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerBlindZoneChecker : ThSprinklerChecker
    {
        public override DBObjectCollection Check(ThExtractorBase extractor, Polyline pline)
        {
            throw new NotImplementedException();
        }

        public override void Present(Database database, DBObjectCollection objs)
        {
            throw new NotImplementedException();
        }
    }
}
