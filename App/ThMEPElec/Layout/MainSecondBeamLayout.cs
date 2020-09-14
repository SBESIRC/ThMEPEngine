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
    /// 主次梁布置
    /// </summary>
    public abstract class MainSecondBeamLayout : SensorLayout
    {
        /// <summary>
        /// ABB处理后的多段线
        /// </summary>
        protected Polyline PostPoly
        {
            get;
            set;
        }

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
