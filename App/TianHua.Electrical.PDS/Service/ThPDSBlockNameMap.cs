using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;

using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSBlockNameMap
    {
        /// <summary>
        /// 负载类型
        /// </summary>
        public ImageLoadType LoadType { get; set; }

        /// <summary>
        /// 图层
        /// </summary>
        public string LayerName { get; set; }

        /// <summary>
        /// 插入块名
        /// </summary>
        public string BlockName { get; set; }

        /// <summary>
        /// 标注插入点偏移量
        /// </summary>
        public Vector3d LabelOffset { get; set; }

        /// <summary>
        /// 插入块属性
        /// </summary>
        public Dictionary<string, string> AttNameValues { get; set; }

        public ThPDSBlockNameMap(ImageLoadType loadType,string layerName, string blockName, Vector3d labelOffset, Dictionary<string, string> attNameValues)
        {
            LoadType = loadType;
            LayerName = layerName;
            BlockName = blockName;
            LabelOffset = labelOffset;
            AttNameValues = attNameValues;
        }

        public ThPDSBlockNameMap(ImageLoadType loadType, string layerName, string blockName, Vector3d labelOffset)
        {
            LoadType = loadType;
            LayerName = layerName;
            BlockName = blockName;
            LabelOffset = labelOffset;
            AttNameValues = null;
        }
    }

    public static class ThPDSBlockNameMapService
    {
        public static List<ThPDSBlockNameMap> MapList = new List<ThPDSBlockNameMap>
        {
            new ThPDSBlockNameMap(ImageLoadType.AL, ThPDSCommon.LAYER_E_POWR_EQPM,"E-BDB001", new Vector3d(0,150,0), new Dictionary<string, string>{ { "BOX", "AL" } }),
            new ThPDSBlockNameMap(ImageLoadType.AP, ThPDSCommon.LAYER_E_POWR_EQPM, "E-BDB001", new Vector3d(0,150,0), new Dictionary<string, string>{ { "BOX", "AP" } }),
            new ThPDSBlockNameMap(ImageLoadType.ALE, ThPDSCommon.LAYER_E_POWR_EQPM, "E-BDB003", new Vector3d(0,150,0), new Dictionary<string, string>{ { "BOX", "ALE" } }),
            new ThPDSBlockNameMap(ImageLoadType.APE, ThPDSCommon.LAYER_E_POWR_EQPM, "E-BDB004", new Vector3d(0,150,0), new Dictionary<string, string>{ { "BOX", "APE" } }),
            new ThPDSBlockNameMap(ImageLoadType.AW, ThPDSCommon.LAYER_E_POWR_EQPM, "E-BDB011", new Vector3d(0,150,0), new Dictionary<string, string>{ { "BOX", "AW" } }),
            new ThPDSBlockNameMap(ImageLoadType.AC, ThPDSCommon.LAYER_E_POWR_EQPM, "E-BDB012", new Vector3d(0,150,0), new Dictionary<string, string>{ { "BOX", "AC" } }),
            new ThPDSBlockNameMap(ImageLoadType.RD, ThPDSCommon.LAYER_E_POWR_EQPM, "E-BDB006-1", new Vector3d(0,125,0)),
            new ThPDSBlockNameMap(ImageLoadType.RS, ThPDSCommon.LAYER_E_POWR_EQPM, "E-BFAS810", new Vector3d(0,150,0), new Dictionary<string, string>{ { "BOX", "RS" } }),
            new ThPDSBlockNameMap(ImageLoadType.INT, ThPDSCommon.LAYER_E_POWR_EQPM, "E-BDB015", new Vector3d(0,150,0), new Dictionary<string, string>{ { "BOX", "INT" } }),
            new ThPDSBlockNameMap(ImageLoadType.Light, ThPDSCommon.LAYER_E_LITE_LITE, "E-BL302", new Vector3d(0,0,0)),
            new ThPDSBlockNameMap(ImageLoadType.Socket, ThPDSCommon.LAYER_E_POWR_DEVC, "E-BS201", new Vector3d(0,300,0)),
            new ThPDSBlockNameMap(ImageLoadType.Motor, ThPDSCommon.LAYER_E_POWR_EQPM, "E-BDB052", new Vector3d(0,250,0)),
            new ThPDSBlockNameMap(ImageLoadType.Pump, ThPDSCommon.LAYER_E_POWR_EQPM, "E-BDB054", new Vector3d(0,250,0)),
        };

        public static ThPDSBlockNameMap Match(ImageLoadType loadType)
        {
            return MapList.Where(o => o.LoadType.Equals(loadType)).First();
        }
    }
}
