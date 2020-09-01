using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public abstract class ThIfcElement : ThIfcProduct
    {
        public Entity Outline { get; set; }
        public string Uuid { get; set; }        
    }
}
