using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{

    public class ParkGroupInfo
    {
        public Polyline BigPolyline; // 合并后的大轮廓
        public Polyline SmallPolyline; // 原始小组的其中一个轮廓

        public ParkGroupInfo(Polyline bigPoly, Polyline smallPoly)
        {
            BigPolyline = bigPoly;
            SmallPolyline = smallPoly;
        }
    }
}
