using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Publics.BaseCode;

namespace TianHua.Electrical
{
    public class ViewFireBlockConver
    {
        public BlockDataModel UpstreamBlockInfo { get; set; }//上游专业块信息

        public BlockDataModel DownstreamBlockInfo { get; set; }//下游专业块信息


        public string UpstreamName { get { return FuncStr.NullToStr(UpstreamBlockInfo.Name); } }


        public string UpstreamRealName { get { return FuncStr.NullToStr(UpstreamBlockInfo.RealName); } }


        public Bitmap UpstreamIcon { get { return UpstreamBlockInfo.Icon; } }


        public string DownstreamName { get { return FuncStr.NullToStr(DownstreamBlockInfo.Name); } }


        public string DownstreamRealName { get { return FuncStr.NullToStr(DownstreamBlockInfo.RealName); } }


        public Bitmap DownstreamIcon { get { return DownstreamBlockInfo.Icon; } }


        public int No { get; set; }

        /// <summary>
        /// 可见性
        /// </summary>
        public string Visibility { get; set; }

        /// <summary>
        /// 是否选择
        /// </summary>
        public bool IsSelect { get; set; }

    }
}
