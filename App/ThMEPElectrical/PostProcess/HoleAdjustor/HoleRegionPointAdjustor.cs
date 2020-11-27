using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.PostProcess.HoleAdjustor
{
    public abstract class HoleRegionPointAdjustor
    {
        protected List<Point3d> m_points;

        public List<Point3d> ValidPoints
        {
            get;
            set;
        } = new List<Point3d>();

        public HoleRegionPointAdjustor(List<Point3d> points)
        {
            m_points = points;
        }

        public virtual void DoAdjustor()
        {
        }
    }
}
