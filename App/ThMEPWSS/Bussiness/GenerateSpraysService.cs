using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Model;
using ThMEPWSS.Service;
using ThWSS.Bussiness;

namespace ThMEPWSS.Bussiness
{
    public class GenerateSpraysPointService
    {
        /// <summary>
        /// 计算喷淋
        /// </summary>
        /// <param name="sprayLines"></param>
        /// <returns></returns>
        public List<SprayLayoutData> GenerateSprays(List<Polyline> sprayLines)
        {
            var classLines = ClassifySprayLines(sprayLines);
            if (classLines == null)
            {
                return new List<SprayLayoutData>();
            }

            return SprayDataOperateService.CalSprayPoint(classLines[0], classLines[1]);
        }

        /// <summary>
        /// 计算喷淋
        /// </summary>
        /// <param name="sprayLines"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public List<SprayLayoutData> GenerateSprays(List<Polyline> sprayLines, double length)
        {
            var classLines = ClassifySprayLines(sprayLines);
            if (classLines == null || classLines.Count <= 1)
            {
                return new List<SprayLayoutData>();
            }

            var mainPoly = classLines[0].First();
            var otherPoly = classLines[1].First();
            Vector3d mainDir = (mainPoly.GetPoint3dAt(0) - mainPoly.GetPoint3dAt(mainPoly.NumberOfVertices - 1)).GetNormal();
            Vector3d otherDir = (otherPoly.GetPoint3dAt(0) - otherPoly.GetPoint3dAt(otherPoly.NumberOfVertices - 1)).GetNormal();

            var sprayPts = SprayDataOperateService.CalSprayPoint(classLines[0], classLines[1]).Select(x => x.Position).ToList();
            return SprayDataOperateService.CalSprayPoint(sprayPts, mainDir, otherDir, length);
        }

        /// <summary>
        /// 分类喷淋布置线
        /// </summary>
        /// <param name="sprayLines"></param>
        /// <returns></returns>
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
