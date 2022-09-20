using System.ComponentModel;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.PropertyVMoldels
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    class TCHWindowPropertyVM : PropertyVMBase
    {
        private TCHWindowProperty property { get; }

        public TCHWindowPropertyVM(string typeName, TCHWindowProperty tchWindowProperty) : base(typeName, tchWindowProperty)
        {
            property = tchWindowProperty;
        }


        [DisplayName("是否门窗统计")]
        public bool A10_Statistics
        {
            get { return property.Statistics; }
            set
            {
                property.Statistics = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("底高")]
        public double A11_BottomElevation
        {
            get { return property.BottomElevation; }
            set
            {
                property.BottomElevation = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("窗高")]
        public double A12_Height
        {
            get { return property.Height; }
            set
            {
                property.Height = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("编号前缀")]
        public string A13_NumberPrefix
        {
            get { return property.NumberPrefix; }
            set
            {
                property.NumberPrefix = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("编号后缀")]
        public string A14_NumberPostfix
        {
            get { return property.NumberPostfix; }
            set
            {
                property.NumberPostfix = value;
                this.RaisePropertyChanged();
            }
        }

        public override object Clone()
        {
            var clone = new TCHWindowPropertyVM(this.TypeName, this.property.Clone() as TCHWindowProperty);
            clone.A01_ShowTypeName = this.A01_ShowTypeName;
            return clone;
        }
    }
}
