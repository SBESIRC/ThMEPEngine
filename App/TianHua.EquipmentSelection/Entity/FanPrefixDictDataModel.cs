using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection
{
    public class FanPrefixDictDataModel
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int No { get; set; }

        /// <summary>
        /// 风机用途
        /// </summary>
        public string FanUse { get; set; }

        /// <summary>
        /// 前缀
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string Explain { get; set; }

    }
}
