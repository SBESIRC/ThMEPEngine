using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.ViewModel;
using ThMEPEngineCore.Model;

namespace TianHua.Mep.UI.Data
{
    internal class ThConfigDataTool
    {
        public static DBObjectCollection GetDoorZones(Database database,Point3dCollection pts)
        {
            var doorBlkNames = GetDoorZoneBlkNames();
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
            var results = new List<string>();   
            var blkNameDict = GetBlockNameList();
            if(blkNameDict.ContainsKey("门块"))
            {
                results.AddRange(blkNameDict["门块"]);
            }
            return results.Distinct().ToList();
        }

        private static Dictionary<string, List<string>> GetBlockNameList()
        {
            var dic = new Dictionary<string, List<string>>();
            var sourceDict = BlockConfigService.Instance.BlockNameList;
            foreach (var item in sourceDict)
            {
                dic.Add(item.Key, item.Value.OfType<BlockNameConfigViewModel>().Select(o => o.layerName).ToList());
            }
            return dic;
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
                        string name = br.GetEffectiveName().ToUpper();
                        return upperBlkNames.Where(o => name.Contains(o.ToUpper())).Any();
                    }
                }
                return false;
            };
            return CheckBlockNameQualified;
        }
    }
}
