using System;

namespace ThMEPEngineCore.Model
{
    public abstract class ThIfcObject
    {
        public string Uuid { get; set; }
        
        public ThIfcObject()
        {            
            Uuid = Guid.NewGuid().ToString();
        }
    }
}
