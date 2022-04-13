using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 小母排
    /// </summary>
    public class MiniBusbar
    {
        /// <summary>
        /// 用来标识小母排的唯一ID
        /// </summary>
        //public string SmallBusbarGuid { get; set; }

        /// <summary>
        /// 功率
        /// </summary>
        public double Power { get; set; }

        /// <summary>
        /// 级联电流额定值
        /// </summary>
        public double CascadeCurrent { get; set; }

        /// <summary>
        /// 坑位1：预留
        /// </summary>
        public Breaker Breaker { get; set; }

        /// <summary>
        /// 坑位2：预留
        /// </summary>
        public PDSBaseComponent ReservedComponent { get; set; }
    }
}
