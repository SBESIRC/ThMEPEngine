using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.PropertyServices.PropertyEnums;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class SlabProperty: PropertyBase
    {
        public SlabProperty(ObjectId objectId) : base(objectId) 
        { 
        
        }
        public EnumSlabMaterial EnumMaterial { get; set; }
        public string Material { get; set; }
        public double SlabTopElevation { get; set; }
        public double SlabThickness { get; set; }
        public double SlabBuildingSurfaceThickness { get; set; }

        public override object Clone()
        {
            var clone = new SlabProperty(this.ObjId);
            clone.Material = this.Material;
            clone.EnumMaterial = this.EnumMaterial;
            clone.SlabTopElevation = this.SlabTopElevation;
            clone.SlabThickness = this.SlabThickness;
            clone.SlabBuildingSurfaceThickness = this.SlabBuildingSurfaceThickness;
            return clone;
        }
    }
}
