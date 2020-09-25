using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Layout
{
    // 布置接口，增加一些公用的处理函数
    public interface ILayout
    {
        List<Point3d> CalculatePlace();
    }
}
