using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.LowVoltageCabinet;

namespace TianHua.Electrical.PDS.Project.Module
{
    public class THPDSSubstationMap
    {
        /// <summary>
        /// 变电所至一级负载映射
        /// </summary>
        private List<SubstationMapInfo> _substationMap;

        public THPDSSubstationMap()
        {
            _substationMap = new List<SubstationMapInfo>();
        }

        public void Clear()
        {
            _substationMap.Clear();
        }

        public void AddMap(THPDSProjectSubstation substation, ThPDSProjectGraphNode node)
        {
            AddMap(substation, null, node);
        }

        public void AddMap(THPDSProjectSubstation substation, THPDSProjectTransformer transformer, ThPDSProjectGraphNode node)
        {
            AddMap(substation, transformer, null, node);
        }

        /// <summary>
        /// 新增一条映射
        /// </summary>
        /// <param name="substation">变电所</param>
        /// <param name="transformer">变压器</param>
        /// <param name="lowVoltageCabinet">低压柜</param>
        /// <param name="node">负载</param>
        public void AddMap(THPDSProjectSubstation substation, THPDSProjectTransformer transformer, PDSBaseLowVoltageCabinet lowVoltageCabinet, ThPDSProjectGraphNode node)
        {
            _substationMap.Add(new SubstationMapInfo() { Node = node, Substation = substation, Transformer = transformer, LowVoltageCabinet = lowVoltageCabinet });
        }

        public List<ThPDSProjectGraphNode> GetNodes(THPDSProjectSubstation substation, THPDSProjectTransformer transformer, PDSBaseLowVoltageCabinet lowVoltageCabinet)
        {
            return GetMapInfos(substation, transformer, lowVoltageCabinet).Select(o => o.Node).ToList();
        }

        public List<ThPDSProjectGraphNode> GetNodes(THPDSProjectSubstation substation, THPDSProjectTransformer transformer)
        {
            return GetMapInfos(substation, transformer).Select(o => o.Node).ToList();
        }

        public List<ThPDSProjectGraphNode> GetNodes(THPDSProjectSubstation substation)
        {
            return GetMapInfos(substation).Select(o => o.Node).ToList();
        }

        public List<SubstationMapInfo> GetMapInfos(THPDSProjectSubstation substation, THPDSProjectTransformer transformer, PDSBaseLowVoltageCabinet lowVoltageCabinet)
        {
            if (lowVoltageCabinet.IsNull())
            {
                return GetMapInfos(substation, transformer);
            }
            else if (transformer.IsNull())
            {
                throw new NotSupportedException();
            }
            else
            {
                return _substationMap.Where(o => substation.Equals(o.Substation) && transformer.Equals(o.Transformer) && lowVoltageCabinet.Equals(o.LowVoltageCabinet)).ToList();
            }
        }

        public List<SubstationMapInfo> GetMapInfos(THPDSProjectSubstation substation, THPDSProjectTransformer transformer)
        {
            if (transformer.IsNull())
            {
                return GetMapInfos(substation);
            }
            return _substationMap.Where(o => substation.Equals(o.Substation) && transformer.Equals(o.Transformer)).ToList();
        }

        public List<SubstationMapInfo> GetMapInfos(THPDSProjectSubstation substation)
        {
            return _substationMap.Where(o => substation.Equals(o.Substation)).ToList();
        }
    }

    public class SubstationMapInfo
    {
        public ThPDSProjectGraphNode Node { get; set; }
        public THPDSProjectSubstation Substation { get; set; }
        public THPDSProjectTransformer Transformer { get; set; }
        public PDSBaseLowVoltageCabinet LowVoltageCabinet { get; set; }
    }
}