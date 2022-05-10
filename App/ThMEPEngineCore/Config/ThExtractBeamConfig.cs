using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Common;

namespace ThMEPEngineCore.Config
{
    public class ThExtractBeamConfig
    {
        private static readonly ThExtractBeamConfig instance = new ThExtractBeamConfig() { };
        public static ThExtractBeamConfig Instance { get { return instance; } }
        internal ThExtractBeamConfig()
        {
            BeamEngineOption = BeamEngineOps.DB;
            LayerInfos = new List<ThLayerInfo>();
        }
        static ThExtractBeamConfig()
        {
        }
        public List<ThLayerInfo> LayerInfos { get; set; }
        public BeamEngineOps BeamEngineOption { get; set; }
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
    public enum BeamEngineOps
    {
        Layer = 0,
        DB = 1,
        BeamArea = 2,
    }
}