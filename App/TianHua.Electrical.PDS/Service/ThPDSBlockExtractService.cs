using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

using ThCADExtension;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSBlockExtractService
    {
        public ThPDSBlockExtractService()
        {
            MarkBlocks = new Dictionary<Entity, ThPDSBlockReferenceData>();
            LoadBlocks = new Dictionary<Entity, ThPDSBlockReferenceData>();
            DistBoxBlocks = new Dictionary<Entity, ThPDSBlockReferenceData>();
        }

        /// <summary>
        /// 标注块
        /// </summary>
        public Dictionary<Entity, ThPDSBlockReferenceData> MarkBlocks { get; set; }

        /// <summary>
        /// 负载块
        /// </summary>
        public Dictionary<Entity, ThPDSBlockReferenceData> LoadBlocks { get; set; }

        /// <summary>
        /// 配电箱
        /// </summary>
        public Dictionary<Entity, ThPDSBlockReferenceData> DistBoxBlocks { get; set; }

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
                    if (blockData.EffectiveName.IndexOf(ThPDSCommon.LOAD_LABELS) == 0
                        || blockData.EffectiveName.Contains(ThPDSCommon.PUMP_LABELS)
                        || blockData.EffectiveName.Contains(ThPDSCommon.LOAD_DETAILS))
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
                                Assign(blockData, row);
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
                                        Assign(blockData, row);
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
                                Assign(blockData, row);
                                break;
                            }
                        }
                        LoadBlocks.Add(block, blockData);
                    }
                });
        }

        private void Assign(ThPDSBlockReferenceData blockData, ThPDSBlockInfo row)
        {
            blockData.Cat_1 = row.Cat_1;
            blockData.Cat_2 = row.Cat_2;
            blockData.DefaultCircuitType = row.DefaultCircuitType;
            blockData.Phase = row.Phase;
            blockData.DemandFactor = row.DemandFactor;
            blockData.PowerFactor = row.PowerFactor;
        }
    }
}
