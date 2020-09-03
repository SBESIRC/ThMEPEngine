using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.PostProcess.ConstraintInterface
{
    /// <summary>
    /// 距离约束
    /// </summary>
    public interface IDistanceConstraint
    {
        List<Point3d> CalculateDistancePoints();
    }
}
