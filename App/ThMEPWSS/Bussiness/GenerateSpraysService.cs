using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Service;
using ThWSS.Bussiness;

namespace ThMEPWSS.Bussiness
{
    public class GenerateSpraysService
    {
        public void GenerateSprays(List<Polyline> sprayLines)
        {
            var classLines = ClassifySprayLines(sprayLines);
            if (classLines == null)
            {
                return;
            }

            var sprayData = SprayDataOperateService.CalSprayPoint(classLines[0], classLines[1]);

            //放置喷头
            InsertSprayService.InsertSprayBlock(sprayData.Select(o => o.Position).ToList(), SprayType.SPRAYDOWN);
        }

        private List<List<Polyline>> ClassifySprayLines(List<Polyline> sprayLines)
        {
            if (sprayLines.Count <= 0)
            {
                return null; ;
            }

            Vector3d mainDir = (sprayLines.First().GetPoint3dAt(0) - sprayLines.First().GetPoint3dAt(sprayLines.First().NumberOfVertices - 1)).GetNormal();
            List<Polyline> mainLine = new List<Polyline>();
            List<Polyline> otherLines = new List<Polyline>();
            foreach (var line in sprayLines)
            {
                Vector3d lineDir = (line.GetPoint3dAt(0) - line.GetPoint3dAt(line.NumberOfVertices - 1)).GetNormal();
                if (lineDir == mainDir || lineDir == -mainDir)
                {
                    mainLine.Add(line);
                }
                else
                {
                    otherLines.Add(line);
                }
            }

            return new List<List<Polyline>>() { mainLine, otherLines };
        }
    }
}
