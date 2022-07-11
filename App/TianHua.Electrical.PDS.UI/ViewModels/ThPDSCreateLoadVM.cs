using TianHua.Electrical.PDS.Project.Module;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public sealed class ThPDSCreateLoadVM : ObservableObject
    {
        /// <summary>
        /// 类型
        /// </summary>
        private ImageLoadType _Type;
        public ImageLoadType Type
        {
            get => _Type;
            set => SetProperty(ref _Type, value);
        }

        /// <summary>
        /// 编号
        /// </summary>
        private string _Number;
        public string Number
        {
            get => _Number;
            set => SetProperty(ref _Number, value);
        }

        /// <summary>
        /// 功率
        /// </summary>
        private double _Power;
        public double Power
        {
            get => _Power;
            set => SetProperty(ref _Power, value);
        }

        /// <summary>
        /// 描述
        /// </summary>
        private string _Description;
        public string Description
        {
            get => _Description;
            set => SetProperty(ref _Description, value);
        }

        /// <summary>
        /// 楼层
        /// </summary>
        private string _Storey;
        public string Storey
        {
            get => _Storey;
            set => SetProperty(ref _Storey, value);
        }
    }
}
