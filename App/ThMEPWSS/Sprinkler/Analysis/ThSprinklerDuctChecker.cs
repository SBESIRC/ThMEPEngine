using ThMEPEngineCore.Model;
using ThMEPWSS.Sprinkler.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDuctChecker : ThSprinklerChecker
    {
        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            
        }

        public override void Clean(Polyline pline)
        {
            CleanPline(ThSprinklerCheckerLayer.Duct_Checker_LayerName, pline);
            CleanDimension(ThSprinklerCheckerLayer.Duct_Blind_Zone_LayerName, pline);
        }
    }
}
