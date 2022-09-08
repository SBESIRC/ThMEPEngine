using System.ComponentModel;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.PropertyVMoldels
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    class RailingPropertyVM : PropertyVMBase
    {
        private RailingProperty property { get; }

        public RailingPropertyVM(string typeName, RailingProperty railingProperty) : base(typeName, railingProperty)
        {
            property = railingProperty;
        }

        [DisplayName("底高")]
        public double A10_RailingBottomHeight
        {
            get { return property.RailingBottomHeight; }
            set
            {
                property.RailingBottomHeight = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("栏杆高")]
        public double A11_RailingHeight
        {
            get { return property.RailingHeight; }
            set
            {
                property.RailingHeight = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("厚度")]
        public double A12_RailingThickness
        {
            get { return property.RailingThickness; }
            set
            {
                property.RailingThickness = value;
                this.RaisePropertyChanged();
            }
        }

        public override object Clone()
        {
            var clone = new RailingPropertyVM(this.TypeName, this.property.Clone() as RailingProperty);
            clone.A01_ShowTypeName = this.A01_ShowTypeName;
            return clone;
        }
    }
}
