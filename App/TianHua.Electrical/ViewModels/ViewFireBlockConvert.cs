using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Publics.BaseCode;

namespace TianHua.Electrical
{
    public class ViewFireBlockConvert
    {
        public BlockDataModel UpstreamBlockInfo { get; set; }//上游专业块信息

        public BlockDataModel DownstreamBlockInfo { get; set; }//下游专业块信息


        public string UpstreamID { get { return FuncStr.NullToStr(UpstreamBlockInfo.ID); } }


        public string UpstreamName { get { return FuncStr.NullToStr(UpstreamBlockInfo.Name); } }


        public Bitmap UpstreamIcon { get { return UpstreamBlockInfo.Icon; } }


        public string UpstreamVisibility { get { return UpstreamBlockInfo.Visibility; } }


        public string DownstreamID { get { return FuncStr.NullToStr(DownstreamBlockInfo.ID); } }


        public string DownstreamName { get { return FuncStr.NullToStr(DownstreamBlockInfo.Name); } }


        public Bitmap DownstreamIcon { get { return DownstreamBlockInfo.Icon; } }

        public string DownstreamVisibility { get { return DownstreamBlockInfo.Visibility; } }


        public int No { get; set; }



        /// <summary>
        /// 是否选择
        /// </summary>
        public bool IsSelect { get; set; }

    }
}
