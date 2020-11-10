using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.PostProcess.ConstraintInterface
{
    /// <summary>
    /// 美观约束
    /// </summary>
    public interface IBeautyConstraint
    {
        List<Point3d> CalculateBeautifyPoints();
    }
}
