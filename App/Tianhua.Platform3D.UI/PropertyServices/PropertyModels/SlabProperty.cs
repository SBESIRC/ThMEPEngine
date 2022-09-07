using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tianhua.Platform3D.UI.Enums;

namespace Tianhua.Platform3D.UI.PropertyServices.PropertyModels
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
