using HandyControl.Controls;
using System.ComponentModel;
using ThMEPTCH.PropertyServices.PropertyEnums;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.PropertyVMoldels
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    class SlabPropertyVM : PropertyVMBase
    {
        private SlabProperty property { get; }

        public SlabPropertyVM(string typeName, SlabProperty slabProperty) : base(typeName, slabProperty)
        {
            property = slabProperty;
        }

        [Browsable(true)]
        [DisplayName("材质")]
        [Editor(typeof(EnumPropertyEditor<EnumSlabMaterial>), typeof(PropertyEditorBase))]
        public EnumSlabMaterial A10_Material
        {
            get { return property.EnumMaterial; }
            set
            {
                property.EnumMaterial = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("建筑顶标高")]
        public double A11_TopElevation
        {
            get { return property.TopElevation; }
            set
            {
                property.TopElevation = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("结构板厚")]
        public double A12_Thickness
        {
            get { return property.Thickness; }
            set
            {
                property.Thickness = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("建筑面层厚度")]
        public double A13_SurfaceThickness
        {
            get { return property.SurfaceThickness; }
            set
            {
                property.SurfaceThickness = value;
                this.RaisePropertyChanged();
            }
        }

        public override object Clone()
        {
            var clone = new SlabPropertyVM(this.TypeName, this.property.Clone() as SlabProperty);
            clone.A01_ShowTypeName = this.A01_ShowTypeName;
            return clone;
        }
    }
}
