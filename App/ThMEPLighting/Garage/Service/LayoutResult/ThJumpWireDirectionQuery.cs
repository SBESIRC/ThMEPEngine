using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThJumpWireDirectionQuery
    {
        private Dictionary<string, int> DirectionConfig { get; set; }
        private ThJumpWireDirectionCalculator Calculator { get; set; }
        public ThJumpWireDirectionQuery (
            Dictionary<string,int> directionConfig,
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts)
        {
            DirectionConfig = directionConfig;
            Calculator = new ThJumpWireDirectionCalculator(centerSideDicts);
        }

        public Vector3d? Query(string number,Point3d position)
        {
            var direction = Calculator.Calcuate(position);
            if(direction.HasValue)
            {
                var mark = Query(number);
                if (mark > 0) // 外偏
                {
                    return direction.Value.Negate();
                }
                else if (mark < 0)
                {
                    return direction.Value; // 内偏
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        public Vector3d? Query(Point3d position)
        {
            return Calculator.Calcuate(position);
        }

        private int Query(string number)
        {
            if(DirectionConfig.ContainsKey(number))
            {
                return DirectionConfig[number];
            }
            return 0;
        }
    }
}
