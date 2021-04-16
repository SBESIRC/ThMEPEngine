using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.Model
{
    public class EvacuationPathModel
    {
        public Line line { get; set; }

        public PathType evaPathType { get; set; }

        public SetUpType setUpType { get; set; }
    }

    public enum SetUpType
    {
        /// <summary>
        /// 吊装
        /// </summary>
        ByHoisting,

        /// <summary>
        /// 壁装
        /// </summary>
        ByWall,
    }

    public enum PathType
    {
        /// <summary>
        /// 主要疏散路径
        /// </summary>
        MainPath,

        /// <summary>
        /// 辅助疏散路径
        /// </summary>
        AuxiliaryPath,
    }
}
