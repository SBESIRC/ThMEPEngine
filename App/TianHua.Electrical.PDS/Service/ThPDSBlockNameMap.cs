﻿using System.Collections.Generic;
using System.Linq;
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
        /// 插入块名
        /// </summary>
        public string BlockName { get; set; }

        /// <summary>
        /// 插入块属性
        /// </summary>
        public Dictionary<string, string> AttNameValues { get; set; }

        public ThPDSBlockNameMap(ImageLoadType loadType, string blockName, Dictionary<string, string> attNameValues)
        {
            LoadType = loadType;
            BlockName = blockName;
            AttNameValues = attNameValues;
        }

        public ThPDSBlockNameMap(ImageLoadType loadType, string blockName)
        {
            LoadType = loadType;
            BlockName = blockName;
            AttNameValues = null;
        }
    }

    public static class ThPDSBlockNameMapService
    {
        public static List<ThPDSBlockNameMap> MapList = new List<ThPDSBlockNameMap>
        {
            new ThPDSBlockNameMap(ImageLoadType.AL, "E-BDB001", new Dictionary<string, string>{ { "BOX", "AL" } }),
            new ThPDSBlockNameMap(ImageLoadType.AP, "E-BDB001", new Dictionary<string, string>{ { "BOX", "AP" } }),
            new ThPDSBlockNameMap(ImageLoadType.ALE, "E-BDB003", new Dictionary<string, string>{ { "BOX", "ALE" } }),
            new ThPDSBlockNameMap(ImageLoadType.APE, "E-BDB004", new Dictionary<string, string>{ { "BOX", "APE" } }),
            new ThPDSBlockNameMap(ImageLoadType.AW, "E-BDB011", new Dictionary<string, string>{ { "BOX", "AW" } }),
            new ThPDSBlockNameMap(ImageLoadType.AC, "E-BDB012", new Dictionary<string, string>{ { "BOX", "AC" } }),
            new ThPDSBlockNameMap(ImageLoadType.RD, "E-BDB006-1"),
            new ThPDSBlockNameMap(ImageLoadType.RS, "E-BFAS810", new Dictionary<string, string>{ { "BOX", "RS" } }),
            new ThPDSBlockNameMap(ImageLoadType.INT, "E-BDB015", new Dictionary<string, string>{ { "BOX", "INT" } }),
            new ThPDSBlockNameMap(ImageLoadType.Light, "E-BL302"),
            new ThPDSBlockNameMap(ImageLoadType.Socket, "E-BS201"),
            new ThPDSBlockNameMap(ImageLoadType.Motor, "E-BDB052"),
            new ThPDSBlockNameMap(ImageLoadType.Pump, "E-BDB054"),
        };

        public static ThPDSBlockNameMap Match(ImageLoadType loadType)
        {
            return MapList.Where(o => o.LoadType.Equals(loadType)).First();
        }
    }
}
