using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Layout
{
    /// <summary>
    /// 主次梁异形布置
    /// </summary>
    public class MainSecondBeamPolygonLayout : MainSecondBeamLayout
    {
        public MainSecondBeamPolygonLayout(PlaceInputProfileData inputProfileData, PlaceParameter parameter, Polyline postPoly)
        : base(inputProfileData, parameter)
        {
            PostPoly = postPoly;
        }

        public override List<Point3d> CalculatePlace()
        {
            throw new NotImplementedException();
        }
    }
}
