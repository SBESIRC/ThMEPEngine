using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.ThSprinklerDim.Service;

namespace ThMEPWSS.ThSprinklerDim.Engine
{
    public class ThSprinklerDimEngine
    {

        public static List<ThSprinklerNetGroup> GetSprinklerPtNetwork(List<Point3d> sprinkPts, out double DTTol)
        {

            var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(sprinkPts, out var dtSeg);

            DrawUtils.ShowGeometry(dtOrthogonalSeg, "l0DTO", 241);

            if (dtOrthogonalSeg.Count == 0)
            {
                DTTol = 1600.0;
                return new List<ThSprinklerNetGroup>();
            }

            DTTol = ThSprinklerNetworkService.GetDTLength(dtOrthogonalSeg);
            ThSprinklerNetworkService.FilterTooLongSeg(ref dtOrthogonalSeg, DTTol * 3);
            var netList = ThCreateGroupService.CreateSegGroup(dtOrthogonalSeg, dtSeg, sprinkPts, DTTol);

            return netList;
        }




    }
}
