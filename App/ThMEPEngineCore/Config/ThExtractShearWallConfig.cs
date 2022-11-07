using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Common;

namespace ThMEPEngineCore.Config
{
    public class ThExtractShearWallConfig
    {
        private static readonly ThExtractShearWallConfig instance = new ThExtractShearWallConfig() { };
        public static ThExtractShearWallConfig Instance { get { return instance; } }
        internal ThExtractShearWallConfig()
        {
            LayerInfos = new List<ThLayerInfo>();
            ShearWallLayerOption = ShearwallLayerConfigOps.Default;
        }
        static ThExtractShearWallConfig()
        {
        }
        public ShearwallLayerConfigOps ShearWallLayerOption { get; set; }
        public List<ThLayerInfo> LayerInfos { get; set; }        
        public List<string> GetSelectLayers(Database db)
        {
            using(var acadDb = Linq2Acad.AcadDatabase.Use(db))
            {
                return LayerInfos
                    .Where(o => o.IsSelected)
                    .Where(o => acadDb.Layers.Contains(o.Layer))
                    .Select(o => o.Layer).ToList();
            }
        }
    }
    public enum ShearwallLayerConfigOps
    {
        Default = 0,
        LayerConfig = 1,
    }
}