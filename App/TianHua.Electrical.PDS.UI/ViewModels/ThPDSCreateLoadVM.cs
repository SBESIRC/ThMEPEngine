using TianHua.Electrical.PDS.Project.Module;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public sealed class ThPDSCreateLoadVM : ObservableObject
    {
        /// <summary>
        /// 类型
        /// </summary>
        public ImageLoadType Type { get; set; }

        /// <summary>
        /// 编号
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// 功率
        /// </summary>
        public double Power { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 楼层
        /// </summary>
        public string Storey { get; set; }
    }
}
