using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Noding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;
using ThMEPWSS.Model;

namespace ThMEPWSS.Service
{
    public class CheckService
    {
        public List<ThIfcBeam> allBeams = new List<ThIfcBeam>();

        /// <summary>
        /// 判断是否距梁过近
        /// </summary>
        /// <param name="sprayPoly"></param>
        /// <param name="sprays"></param>
        /// <param name="dir"></param>
        /// <param name="dis"></param>
        /// <returns></returns>
        public bool CheckSprayData(Line sprayPoly, List<SprayLayoutData> sprays, Vector3d dir, double dis)
        {
            var polys = SprayDataOperateService.GetAllSanmeDirLines(dir, sprays);
            var pts = SprayDataOperateService.CalSprayPoint(polys, sprayPoly);
            foreach (var beam in allBeams)
            {
                foreach (var pt in pts)
                {
                    var closet = (beam.Outline as Polyline).GetClosestPointTo(pt, false);
                    if (closet.DistanceTo(pt) < dis)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
