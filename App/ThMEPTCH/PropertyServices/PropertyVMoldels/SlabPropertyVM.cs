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
        public double A11_SlabTopElevation
        {
            get { return property.SlabTopElevation; }
            set 
            {
                property.SlabTopElevation = value;
                this.RaisePropertyChanged();
            }
        }
        [DisplayName("板厚")]
        public double A12_SlabThickness
        {
            get { return property.SlabThickness; }
            set
            {
                property.SlabThickness = value;
                this.RaisePropertyChanged();
            }
        }
        [DisplayName("面层厚度")]
        public double A13_SlabBuildingSurfaceThickness
        {
            get { return property.SlabBuildingSurfaceThickness; }
            set
            {
                property.SlabBuildingSurfaceThickness = value;
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
