using System.Collections.Generic;
using ThMEPEngineCore.Model.Common;

namespace ThMEPEngineCore.Config
{
    public class ThExtratRoomOutlineConfig
    {
        private static readonly ThExtratRoomOutlineConfig instance = new ThExtratRoomOutlineConfig() { };
        public static ThExtratRoomOutlineConfig Instance { get { return instance; } }
        internal ThExtratRoomOutlineConfig()
        {
            LayerInfos = new List<ThLayerInfo>();
        }
        static ThExtratRoomOutlineConfig()
        {
        }
        public List<ThLayerInfo> LayerInfos { get; set; }
        public bool YnExtractShearWall { get; set; } = true;
    }
}
