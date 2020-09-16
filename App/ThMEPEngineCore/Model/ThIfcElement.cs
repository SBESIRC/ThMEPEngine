using Autodesk.AutoCAD.DatabaseServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public abstract class ThIfcElement : ThIfcProduct
    {
        [JsonIgnore]
        public Entity Outline { get; set; }
        [JsonIgnore]
        public string Uuid { get; set; }        
    }
}
