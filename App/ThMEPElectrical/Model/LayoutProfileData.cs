using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    /// <summary>
    /// 布置的原始轮廓和处理后的轮廓
    /// </summary>
    public class LayoutProfileData
    {
        public Polyline SrcPolyline = null;
        public Polyline PostPolyline = null;
        
        public LayoutProfileData(Polyline poly, Polyline postPoly)
        {
            SrcPolyline = poly;
            PostPolyline = postPoly;
        }
    }
}
