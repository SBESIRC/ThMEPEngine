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
    /// 主次梁矩形布置
    /// </summary>
    public class MainSecondBeamRectangleLayout : MainSecondBeamLayout
    {
        public MainSecondBeamRectangleLayout(PlaceInputProfileData inputProfileData, PlaceParameter parameter, Polyline postPoly)
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
