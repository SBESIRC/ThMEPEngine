using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class PolyLineSplit
    {
        //将多段线打断成线段List
        public static List<Line> ConvertPolylineToLineList(Polyline polyline)
        {
            var lineList = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                var ptPre = polyline.GetPoint3dAt(i);
                var ptNext = polyline.GetPoint3dAt(i+1);
                
                lineList.Add(new Line(ptPre, ptNext));
            }
            return lineList;
        }
    }
}
