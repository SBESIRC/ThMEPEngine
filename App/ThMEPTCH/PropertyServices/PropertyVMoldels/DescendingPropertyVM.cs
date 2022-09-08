using System.ComponentModel;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.PropertyVMoldels
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    class DescendingPropertyVM : PropertyVMBase
    {
        private DescendingProperty property { get; }

        public DescendingPropertyVM(string typeName, DescendingProperty descendingProperty) : base(typeName, descendingProperty)
        {
            property = descendingProperty;
        }

        [DisplayName("结构降板厚度")]
        public double A10_DescendingThickness
        {
            get { return property.DescendingThickness; }
            set
            {
                property.DescendingThickness = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("结构包围厚度")]
        public double A11_DescendingWrapThickness
        {
            get { return property.DescendingWrapThickness; }
            set
            {
                property.DescendingWrapThickness = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("建筑面层厚度")]
        public double A12_DescendingSurfaceThickness
        {
            get { return property.DescendingSurfaceThickness; }
            set
            {
                property.DescendingSurfaceThickness = value;
                this.RaisePropertyChanged();
            }
        }

        public override object Clone()
        {
            var clone = new DescendingPropertyVM(this.TypeName, this.property.Clone() as DescendingProperty);
            clone.A01_ShowTypeName = this.A01_ShowTypeName;
            return clone;
        }
    }
}
