using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Hvac.UI.UI.IndoorFan
{
    public abstract class RadioButtonItem
    {
        public RadioButtonItem()
        {
            this.Id = System.Guid.NewGuid().ToString();
        }
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 显示内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 组名称
        /// </summary>
        public string GroupName { get; set; }
        /// <summary>
        /// 扩展标识数据
        /// </summary>
        public object DynTag { get; set; }
    }
}
