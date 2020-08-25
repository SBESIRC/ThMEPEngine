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
    // 主梁布置
    public abstract class MainBeamLayout : SensorLayout
    {
        protected Polyline m_postMainBeamPoly;

        public override List<Point3d> CalculatePlace()
        {
            return null;
        }

        public MainBeamLayout(PlaceInputProfileData inputProfileData, PlaceParameter parameter, Polyline postPoly) 
            : base(inputProfileData, parameter)
        {
            m_postMainBeamPoly = postPoly;
        }
    }
}
