using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Model;
using ThMEPWSS.Service;

namespace ThMEPWSS.Bussiness
{
    public class GenerateSpraysPointService
    {
        /// <summary>
        /// 计算喷淋
        /// </summary>
        /// <param name="sprayLines"></param>
        /// <returns></returns>
        public List<SprayLayoutData> GenerateSprays(List<Line> sprayLines)
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
        public List<SprayLayoutData> GenerateSprays(List<Line> sprayLines, double length)
        {
            var classLines = ClassifySprayLines(sprayLines);
            if (classLines == null || classLines.Count <= 1)
            {
                return new List<SprayLayoutData>();
            }

            var mainLine = classLines[0].First();
            var otherLine = classLines[1].First();
            Vector3d mainDir = (mainLine.EndPoint - mainLine.StartPoint).GetNormal();
            Vector3d otherDir = (otherLine.EndPoint - otherLine.StartPoint).GetNormal();

            var sprayPts = SprayDataOperateService.CalSprayPoint(classLines[0], classLines[1]).Select(x => x.Position).ToList();
            return SprayDataOperateService.CalSprayPoint(sprayPts, mainDir, otherDir, length);
        }

        /// <summary>
        /// 分类喷淋布置线
        /// </summary>
        /// <param name="sprayLines"></param>
        /// <returns></returns>
        private List<List<Line>> ClassifySprayLines(List<Line> sprayLines)
        {
            if (sprayLines.Count <= 0)
            {
                return null; ;
            }

            Vector3d mainDir = (sprayLines.First().EndPoint - sprayLines.First().StartPoint).GetNormal();
            List<Line> mainLine = new List<Line>();
            List<Line> otherLines = new List<Line>();
            foreach (var line in sprayLines)
            {
                Vector3d lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                if (lineDir == mainDir || lineDir == -mainDir)
                {
                    mainLine.Add(line);
                }
                else
                {
                    otherLines.Add(line);
                }
            }

            return new List<List<Line>>() { mainLine, otherLines };
        }
    }
}
