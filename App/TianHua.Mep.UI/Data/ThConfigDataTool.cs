using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace TianHua.Mep.UI.Data
{
    internal class ThConfigDataTool
    {
        //提取房间
        public const string ExtractRoomNamedDictKey = "提取房间配置";
        public const string WallLayerSearchKey = "墙图层";
        public const string DoorBlkNameConfigSearchKey = "门图块";
        public const string YnExtractShearWallSearchKey = "是否提取剪力墙";

        //图层配置
        public const string ExtractLayerNamedDictKey = "图层配置";
        public const string BeamLayerSearchKey = "梁图层";
        public const string ShearWallLayerSearchKey = "剪力墙图层";
        public const string BeamEngineOptionSearchKey = "梁识别引擎"; // DB,图层
        public const string ShearWallOptionSearchKey = "剪力墙图层选项"; //默认，图层

        //建筑轮廓线配置
        public const string ExtractArchitectureOutlineNamedDictKey = "提取建筑轮廓线配置";

        public static DBObjectCollection GetDoorZones(Database database,Point3dCollection pts)
        {
            var doorBlkNames = GetDoorZoneBlkNames();
            return GetDoorZones(database,pts,doorBlkNames);
        }

        public static DBObjectCollection GetDoorZones(Database database, Point3dCollection pts, List<string> doorBlkNames)
        {
            var recognizer = new ThDoorZoneRecognitionEngine()
            {
                CheckQualifiedBlockName = CreateCheckBlkNameMethod(doorBlkNames),
            };
            recognizer.Recognize(database, pts);
            return recognizer.Elements
                .OfType<ThIfcSpace>()
                .Select(o => o.Boundary)
                .ToCollection();
        }
        private static List<string> GetDoorZoneBlkNames()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var results = new List<string>();
                var extractRoomConfigNamedDictId = acadDb.Database.GetNamedDictionary(ExtractRoomNamedDictKey);
                if (extractRoomConfigNamedDictId != ObjectId.Null)
                {
                    var doorBlkTvs = extractRoomConfigNamedDictId.GetXrecord(DoorBlkNameConfigSearchKey);
                    if (doorBlkTvs != null)
                    {
                        foreach (TypedValue tv in doorBlkTvs)
                        {
                            var value = tv.Value.ToString();
                            if (!results.Contains(value))
                            {
                                results.Add(value);
                            }
                        }
                    }
                }
                return results;
            }
        }

        private static Func<Entity, bool> CreateCheckBlkNameMethod(List<string> blkNames)
        {
            var upperBlkNames = blkNames.Select(o => o.ToUpper()).ToList();
            Func<Entity, bool> CheckBlockNameQualified = entity =>
            {
                if (entity is BlockReference br)
                {
                    if (!br.BlockTableRecord.IsNull)
                    {
                        string name = ThMEPEngineCore.Algorithm.ThMEPXRefService.
                        OriginalFromXref(br.GetEffectiveName()).ToUpper();
                        return upperBlkNames.Contains(name);
                    }
                }
                return false;
            };
            return CheckBlockNameQualified;
        }
    }
}
