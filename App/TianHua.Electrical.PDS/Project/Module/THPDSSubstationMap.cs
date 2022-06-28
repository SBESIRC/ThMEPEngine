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
        private Dictionary<ThPDSProjectGraphNode, Tuple<THPDSProjectSubstation, THPDSProjectTransformer, PDSBaseLowVoltageCabinet>> _substationMap;

        public THPDSSubstationMap()
        {
            _substationMap = new Dictionary<ThPDSProjectGraphNode, Tuple<THPDSProjectSubstation, THPDSProjectTransformer, PDSBaseLowVoltageCabinet>>();
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
            if (_substationMap.ContainsKey(node))
            {
                _substationMap[node] = (substation,transformer, lowVoltageCabinet).ToTuple();
            }
            else
            {
                _substationMap.Add(node, (substation, transformer, lowVoltageCabinet).ToTuple());
            }
        }

        public List<ThPDSProjectGraphNode> GetNodes(THPDSProjectSubstation substation, THPDSProjectTransformer transformer, PDSBaseLowVoltageCabinet lowVoltageCabinet)
        {
            if (lowVoltageCabinet.IsNull())
            {
                return GetNodes(substation, transformer);
            }
            else if (transformer.IsNull())
            {
                throw new NotSupportedException();
            }
            else
            {
                return _substationMap.Where(o => substation.Equals(o.Value.Item1) && transformer.Equals(o.Value.Item2) && lowVoltageCabinet.Equals(o.Value.Item3)).Select(o => o.Key).ToList();
            }
        }

        public List<ThPDSProjectGraphNode> GetNodes(THPDSProjectSubstation substation, THPDSProjectTransformer transformer)
        {
            if (transformer.IsNull())
            {
                return GetNodes(substation);
            }
            return _substationMap.Where(o => substation.Equals(o.Value.Item1) && transformer.Equals(o.Value.Item2)).Select(o => o.Key).ToList();
        }

        public List<ThPDSProjectGraphNode> GetNodes(THPDSProjectSubstation substation)
        {
            return _substationMap.Where(o => substation.Equals(o.Value.Item1)).Select(o => o.Key).ToList();
        }
    }
}
