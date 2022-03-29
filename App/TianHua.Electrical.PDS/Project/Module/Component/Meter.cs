using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 电表
    /// </summary>
    public abstract class Meter : PDSBaseComponent
    {
        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        protected List<string> AlternativeParameters { get; set; }

        /// <summary>
        /// 修改参数
        /// </summary>
        /// <param name="polesNum"></param>
        public abstract void SetParameters(string parameters);

        public abstract List<string> GetParameters();
    }
}
