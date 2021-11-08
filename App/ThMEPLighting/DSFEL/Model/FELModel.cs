using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.DSFEL.Model
{
    public class FELModel
    {
        /// <summary>
        /// 插入基点
        /// </summary>
        public Point3d positin { get; set; }

        /// <summary>
        /// 布置方向
        /// </summary>
        public Vector3d direction { get; set; }
    }
}
