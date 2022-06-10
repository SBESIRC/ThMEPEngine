using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    public class ThHandleConflictService
    {
        private List<Entity> HandleElements { get; set; }
        private List<Entity> FirstKeepElements { get; set; }
        public List<Entity> Results { get; private set; }
        public ThHandleConflictService()
        {
            Results = new List<Entity>();
            HandleElements = new List<Entity>();
            FirstKeepElements = new List<Entity>();
        }

        public ThHandleConflictService(List<Entity> firstKeepElements, List<Entity> handleElements)
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
        public List<ThIfcDoor> Union(List<ThIfcDoor> db3Doors, List<ThIfcDoor> localDoors)
        {
            var results = new List<ThIfcDoor>();
            results.AddRange(db3Doors);
            results.AddRange(localDoors);
            foreach (ThIfcDoor first in localDoors)
            {
                foreach (ThIfcDoor second in db3Doors)
                {
                    if (IsOverlap(first.Outline, second.Outline))
                    {
                        var temp = new DBObjectCollection
                        {
                            first.Outline,
                            second.Outline
                        };
                        var mergeObjs = temp.UnionPolygons();
                        if (mergeObjs.Count > 0)
                        {
                            results.Remove(first);
                            results.Remove(second);
                            var result = new ThIfcDoor
                            {
                                Spec = second.Spec,
                                Switch = second.Switch,
                                OpenAngle = second.OpenAngle,
                                Height = second.Height,
                            };
                            var firstPoly = mergeObjs
                                .Cast<Polyline>()
                                .OrderByDescending(p => p.Area).First();
                            result.Outline = firstPoly.GetMinimumRectangle();
                            results.Add(result);
                        }
                        break;
                    }
                }
            }
            return results;
        }
        private bool IsOverlap(Entity first, Entity second)
        {
            //规则待定
            var relateMatrix = new ThCADCoreNTSRelate(first.ToNTSPolygonalGeometry(), second.ToNTSPolygonalGeometry());
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
