using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Layout
{
    /// <summary>
    /// 主次梁布置
    /// </summary>
    public class MainSecondBeamLayout : SensorLayout
    {
        public override List<Point3d> CalculatePlace()
        {
            return PlacePoints;
        }

        public MainSecondBeamLayout(PlaceInputProfileData inputProfileData, PlaceParameter parameter)
            : base(inputProfileData, parameter)
        {

        }
    }
}
