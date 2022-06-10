using System;
using System.Collections.Generic;

namespace ThMEPEngineCore.Model
{
    public abstract class ThIfcObject : ThIfcObjectDefinition
    {
        public string Uuid { get; set; }
        public Dictionary<string, object> Properties { get; }
        public ThIfcObject()
        {            
            Uuid = Guid.NewGuid().ToString();
            Properties = new Dictionary<string, object>();
        }
    }
}
