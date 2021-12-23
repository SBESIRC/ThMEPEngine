using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Hvac.UI.IndoorFanModels
{
    class IndoorFanSaveModel
    {
        /// <summary>
        /// 不同的风机数据，要解析为不同的数据模型，这里保存json,
        /// 根据名称不同的序列化数据
        /// </summary>
        public string SheetName { get; set; }
        public string StringData { get; set; }
    }
}
