using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public abstract class ThIfcProduct : ThIfcObject
    {
        public string Name { get; set; }
        public string Spec { get; set; }
        public string Useage { get; set; }
        public ThIfcProduct()
        {
            Name = "";
            Spec = "";
            Useage = "";
        }
    }
}
