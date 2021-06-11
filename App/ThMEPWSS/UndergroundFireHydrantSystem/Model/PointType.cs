using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    class PointType
    {
        private Point3d BranchPoint { get; set; }
        private int BranchType { get; set; }

        public PointType(Point3d branchPoint, int branchType)
        {
            BranchPoint = branchPoint;
            BranchType = branchType;
        }

        public Point3d GetBranchPoint()
        {
            return BranchPoint;
        }

        public int GetBranchType()
        {
            return BranchType;
        }
    }
}
