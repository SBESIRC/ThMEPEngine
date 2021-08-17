using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThMEPEngineCore.CAD;
using System.Linq;

namespace ThMEPElectrical.FireAlarm.Service
{
    public class ThHandleConflictService
    {
        private List<Entity> HandleElements { get; set; }
        private List<Entity> FirstKeepElements { get; set; }
        public List<Entity> Results { get; private set; }
        public ThHandleConflictService(List<Entity> firstKeepElements,List<Entity> handleElements)
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
            foreach(Entity first in HandleElements) // 100
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
        private bool IsOverlap(Entity first,Entity second)
        {
            //规则待定
            var relateMatrix = new ThCADCoreNTSRelate(first.ToNTSPolygon(), second.ToNTSPolygon());
            return relateMatrix.IsOverlaps;
        }
    }
}
