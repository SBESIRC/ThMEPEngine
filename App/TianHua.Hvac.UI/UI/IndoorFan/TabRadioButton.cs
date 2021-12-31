using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Hvac.UI.UI.IndoorFan
{
    public class TabRadioButton : RadioButtonItem
    {
        public bool InEdit { get; set; }
        /// <summary>
        /// 是否可以编辑
        /// </summary>
        public bool CanEdit { get; set; }
        /// <summary>
        /// 是否可以删除
        /// </summary>
        public bool CanDelete { get; set; }
        /// <summary>
        /// 是否是添加按钮
        /// </summary>
        public bool IsAddBtn { get; set; }
    }
}
