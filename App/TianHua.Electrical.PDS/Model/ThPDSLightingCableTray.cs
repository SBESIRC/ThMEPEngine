using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace TianHua.Electrical.PDS.Model
{
    [Serializable]
    public class ThPDSLightingCableTray
    {
        /// <summary>
        /// 是否直接与桥架相连
        /// </summary>
        public bool OnLightingCableTray { get; set; }

        /// <summary>
        /// 桥架实体
        /// </summary>
        public Curve CableTray { get; set; }

        public ThPDSLightingCableTray()
        {
            OnLightingCableTray = false;
            CableTray = null;
        }
    }
}
