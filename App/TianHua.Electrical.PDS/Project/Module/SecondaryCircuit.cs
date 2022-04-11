using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 控制回路
    /// </summary>
    public class SecondaryCircuit
    {
        private int _index;
        public SecondaryCircuit(int index)
        {
            _index = index;
            edges = new List<ThPDSProjectGraphEdge>();
        }

        /// <summary>
        /// 回路ID
        /// </summary>
        public string CircuitID { 
            get 
            { 
                return $"WC{_index.ToString("00")}"; 
            } 
        }

        /// <summary>
        /// 回路描述
        /// </summary>
        public string CircuitDescription { get; set; }

        public List<ThPDSProjectGraphEdge> edges { get; set; }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor conductor { get; set; }
    }
}
