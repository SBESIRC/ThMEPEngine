using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

using ThCADExtension;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Model;
using ThMEPEngineCore.CAD;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSBlockExtractService
    {
        /// <summary>
        /// 标注块
        /// </summary>
        public Dictionary<Entity, ThPDSBlockReferenceData> MarkBlocks { get; set; } = new Dictionary<Entity, ThPDSBlockReferenceData>();

        /// <summary>
        /// 负载块
        /// </summary>
        public Dictionary<Entity, ThPDSBlockReferenceData> LoadBlocks { get; set; } = new Dictionary<Entity, ThPDSBlockReferenceData>();

        /// <summary>
        /// 配电箱
        /// </summary>
        public Dictionary<Entity, ThPDSBlockReferenceData> DistBoxBlocks { get; set; } = new Dictionary<Entity, ThPDSBlockReferenceData>();

        public void Extract(Database database, List<ThPDSBlockInfo> tableInfo, List<string> nameFilter,
            List<string> propertyFilter, List<string> distBoxKey)
        {
            var engine = new ThPDSBlockExtractionEngine
            {
                NameFilter = nameFilter.Distinct().ToList(),
                PropertyFilter = propertyFilter.Distinct().ToList(),
                DistBoxKey = distBoxKey,
            };
            engine.ExtractFromMS(database);
            engine.Results.Select(o => o.Data as BlockReference)
                .ForEach(block =>
                {
                    var blockData = new ThPDSBlockReferenceData(block.ObjectId);
                    if (blockData.EffectiveName.IndexOf("负载标注") == 0
                        || blockData.EffectiveName.Contains("水泵标注")
                        || blockData.EffectiveName.Contains("E-电力平面-负荷明细"))
                    {
                        MarkBlocks.Add(block, blockData);
                        return;
                    }
                    else if (blockData.EffectiveName.Contains("E-BDB006-1"))
                    {
                        foreach (var row in tableInfo)
                        {
                            if (row.BlockName == blockData.EffectiveName)
                            {
                                blockData.Cat_1 = row.Cat_1;
                                blockData.Cat_2 = row.Cat_2;
                                blockData.DefaultCircuitType = row.DefaultCircuitType;
                                break;
                            }
                        }
                        DistBoxBlocks.Add(block, blockData);
                        return;
                    }

                    var checker = false;
                    foreach (var value in blockData.Attributes.Values)
                    {
                        for (var i = 0; i < distBoxKey.Count; i++)
                        {
                            if (value.IndexOf(distBoxKey[i]) == 0 && !checker)
                            {
                                foreach (var row in tableInfo)
                                {
                                    if (row.Properties == distBoxKey[i])
                                    {
                                        blockData.Cat_1 = row.Cat_1;
                                        blockData.Cat_2 = row.Cat_2;
                                        blockData.DefaultCircuitType = row.DefaultCircuitType;
                                        checker = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (checker)
                    {
                        DistBoxBlocks.Add(block, blockData);
                    }
                    else
                    {
                        foreach (var row in tableInfo)
                        {
                            if (row.BlockName == blockData.EffectiveName)
                            {
                                blockData.Cat_1 = row.Cat_1;
                                blockData.Cat_2 = row.Cat_2;
                                blockData.DefaultCircuitType = row.DefaultCircuitType;
                                break;
                            }
                        }
                        LoadBlocks.Add(block, blockData);
                    }
                });
        }
    }
}
