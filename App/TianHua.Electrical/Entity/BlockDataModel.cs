using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace TianHua.Electrical
{
    /// <summary>
    /// 图块信息
    /// </summary>
    public class BlockDataModel
    {
        public string Name { get; set; }//普通名称

        public string RealName { get; set; }//真实名称

        public Bitmap Icon { get; set; }//块缩略图
    }
}
