using System.ComponentModel;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.PropertyVMoldels
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    class HolePropertyVM : PropertyVMBase
    {
        private HoleProperty property { get; }

        public HolePropertyVM(string typeName, HoleProperty slabHoleProperty) : base(typeName, slabHoleProperty)
        {
            property = slabHoleProperty;
        }

        [DisplayName("是否忽略尺寸标注")]
        public bool A10_ShowDimension
        {
            get { return property.ShowDimension; }
            set
            {
                property.ShowDimension = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("是否遮挡元素")]
        public bool A11_Hidden
        {
            get { return property.Hidden; }
            set
            {
                property.Hidden = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("底高")]
        public double A12_BottomHeight
        {
            get { return property.BottomElevation; }
            set
            {
                property.BottomElevation = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("洞高")]
        public double A13_HoleHeight
        {
            get { return property.Height; }
            set
            {
                property.Height = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("编号前缀")]
        public string A14_NumberPrefix
        {
            get { return property.NumberPrefix; }
            set
            {
                property.NumberPrefix = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("编号后缀")]
        public string A15_NumberPostfix
        {
            get { return property.NumberPostfix; }
            set
            {
                property.NumberPostfix = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("立面显示")]
        public bool A16_ElevationDisplay
        {
            get { return property.ElevationDisplay; }
            set
            {
                property.ElevationDisplay = value;
                this.RaisePropertyChanged();
            }
        }

        public override object Clone()
        {
            var clone = new HolePropertyVM(this.TypeName, this.property.Clone() as HoleProperty);
            clone.A01_ShowTypeName = this.A01_ShowTypeName;
            return clone;
        }
    }
}
