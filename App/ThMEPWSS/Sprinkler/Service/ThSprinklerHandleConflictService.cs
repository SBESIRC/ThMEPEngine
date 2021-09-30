using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Service
{
    public class ThSprinklerHandleConflictService
    {
        private List<Entity> HandleElements { get; set; }
        private List<Entity> FirstKeepElements { get; set; }
        public List<Entity> Results { get; private set; }
        public ThSprinklerHandleConflictService()
        {
            Results = new List<Entity>();
            HandleElements = new List<Entity>();
            FirstKeepElements = new List<Entity>();
        }

        public ThSprinklerHandleConflictService(List<Entity> firstKeepElements, List<Entity> handleElements)
        {
            FirstKeepElements = firstKeepElements.ToCollection().FilterSmallArea(1.0).Cast<Entity>().ToList();
            HandleElements = handleElements.ToCollection().FilterSmallArea(1.0).Cast<Entity>().ToList();
            Results = new List<Entity>();
        }

        public void Handle()
        {
            Results.AddRange(FirstKeepElements);
            Results.AddRange(HandleElements);

            //两个物体重叠，优先保留本地的          
            foreach (Entity first in HandleElements) // 100
            {
                foreach (Entity second in FirstKeepElements) // 50 
                {
                    if (IsOverlap(first, second))
                    {
                        Results.Remove(first);
                        break;
                    }
                }
            }
        }

        private bool IsOverlap(Entity first, Entity second)
        {
            //规则待定
            var relateMatrix = new ThCADCoreNTSRelate(first.ToNTSPolygon(), second.ToNTSPolygon());
            if (relateMatrix.IsOverlaps)
                return true;
            else if (relateMatrix.IsContains)
                return true;
            else if (relateMatrix.IsCovers)
                return true;
            else if (relateMatrix.IsIntersects)
                return true;
            return false;
        }
    }
}
