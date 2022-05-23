using System;

namespace ThMEPEngineCore.Model
{
    public abstract class ThIfcObject : ThIfcObjectDefinition
    {
        public string Uuid { get; set; }
        
        public ThIfcObject()
        {            
            Uuid = Guid.NewGuid().ToString();
        }
    }
}
