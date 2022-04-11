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
        public int Index;
        public SecondaryCircuit(int index)
        {
            Index = index;
        }

        /// <summary>
        /// 回路ID
        /// </summary>
        public string CircuitID { 
            get 
            { 
                return $"WC{Index.ToString("00")}"; 
            } 
        }

        /// <summary>
        /// 回路描述
        /// </summary>
        public string CircuitDescription { get; set; }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor conductor { get; set; }
    }
}
