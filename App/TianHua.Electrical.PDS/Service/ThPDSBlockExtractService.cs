using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

using ThCADExtension;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Model;
using Linq2Acad;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSBlockExtractService
    {
        public ThPDSBlockExtractService()
        {
            MarkBlocks = new Dictionary<Entity, ThPDSBlockReferenceData>();
            LoadBlocks = new Dictionary<Entity, ThPDSBlockReferenceData>();
            DistBoxBlocks = new Dictionary<Entity, ThPDSBlockReferenceData>();
            Ignore = new List<Polyline>();
            Attached = new List<Polyline>();
            Terminal = new List<Polyline>();
        }

        /// <summary>
        /// 标注块
        /// </summary>
        public Dictionary<Entity, ThPDSBlockReferenceData> MarkBlocks;

        /// <summary>
        /// 负载块
        /// </summary>
        public Dictionary<Entity, ThPDSBlockReferenceData> LoadBlocks;

        /// <summary>
        /// 配电箱
        /// </summary>
        public Dictionary<Entity, ThPDSBlockReferenceData> DistBoxBlocks;

        /// <summary>
        /// 忽略块
        /// </summary>
        public List<Polyline> Ignore;

        /// <summary>
        /// 附着块
        /// </summary>
        public List<Polyline> Attached;

        /// <summary>
        /// 末端块
        /// </summary>
        public List<Polyline> Terminal;

        public void Extract(Database database, List<ThPDSBlockInfo> tableInfo, List<string> nameFilter,
            List<string> propertyFilter, List<string> distBoxKey, List<ThPDSFilterBlockInfo> filterBlockInfo)
        {
            using (var acad = AcadDatabase.Use(database))
            {
                var newNameFilter = new List<string>();
                var newPropertyFilter = new List<string>();
                nameFilter.ForEach(o => newNameFilter.Add(o));
                propertyFilter.ForEach(o => newPropertyFilter.Add(o));
                filterBlockInfo.ForEach(o =>
                {
                    if (string.IsNullOrEmpty(o.Properties))
                    {
                        newNameFilter.Add(o.BlockName);
                    }
                    else
                    {
                        newPropertyFilter.Add(o.Properties);
                    }
                });

                var engine = new ThPDSBlockExtractionEngine
                {
                    NameFilter = newNameFilter.Distinct().ToList(),
                    PropertyFilter = newPropertyFilter.Distinct().ToList(),
                    DistBoxKey = distBoxKey,
                };
                engine.ExtractFromMS(database);
                engine.Results.Select(o => o.Data as ThPDSBlockReferenceData)
                    .ForEach(blockData =>
                    {
                        var block = acad.Element<BlockReference>(blockData.ObjId, true).Clone() as BlockReference;
                        if (blockData.EffectiveName.IndexOf(ThPDSCommon.LOAD_LABELS) == 0
                            || blockData.EffectiveName.Contains(ThPDSCommon.LOAD_LABELS_AI)
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
                        foreach (var item in blockData.Attributes)
                        {
                            if (!item.Key.Equals("BOX"))
                            {
                                continue;
                            }
                            var value = item.Value;
                            for (var i = 0; i < distBoxKey.Count; i++)
                            {
                                if (value.Contains(distBoxKey[i]) && !checker)
                                {
                                    if (value.Contains("APE") && !distBoxKey[i].Equals("APE"))
                                    {
                                        continue;
                                    }

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
                            var attributes = blockData.Attributes.Select(x => x.Value).ToList();
                            var filterBlock = filterBlockInfo.Where(info => info.BlockName.Equals(blockData.EffectiveName)
                                || attributes.Contains(info.Properties)).FirstOrDefault();
                            if (filterBlock != null)
                            {
                                switch (filterBlock.FilteringMethod)
                                {
                                    case FilteringMethod.Ignore:
                                        Ignore.Add(ThPDSBufferService.Buffer(block.BlockOBB()));
                                        break;
                                    case FilteringMethod.Attached:
                                        Attached.Add(ThPDSBufferService.Buffer(block.BlockOBB()));
                                        break;
                                    case FilteringMethod.Terminal:
                                        Terminal.Add(ThPDSBufferService.Buffer(block.BlockOBB()));
                                        break;
                                }
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
                                if (LoadBlocks.Any(b => b.Value.Position.TransformBy(b.Value.OwnerSpace2WCS)
                                    .DistanceTo(blockData.Position.TransformBy(blockData.OwnerSpace2WCS)) < 1.0))
                                {
                                    return;
                                }
                                LoadBlocks.Add(block, blockData);
                            }
                        }
                    });
            }
        }

        private void Assign(ThPDSBlockReferenceData blockData, ThPDSBlockInfo row)
        {
            blockData.Cat_1 = row.Cat_1;
            blockData.Cat_2 = row.Cat_2;
            blockData.DefaultCircuitType = row.DefaultCircuitType;
            blockData.Phase = row.Phase;
            blockData.DemandFactor = row.DemandFactor;
            blockData.PowerFactor = row.PowerFactor;
            blockData.FireLoad = row.FireLoad;
            blockData.DefaultDescription = row.DefaultDescription;
            blockData.CableLayingMethod1 = row.CableLayingMethod1;
            blockData.CableLayingMethod2 = row.CableLayingMethod2;
        }
    }
}
