using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.DSFEL.Model
{
    public class ExitModel : FELModel
    {
        /// <summary>
        /// 疏散口类型
        /// </summary>
        public ExitType exitType { get; set; }

        /// <summary>
        /// 属于哪个房间
        /// </summary>
        public Polyline room { get; set; }       
    }

    public enum ExitType
    {
        //疏散出口
        EvacuationExit,

        //安全出口
        SafetyExit,
    }
}
