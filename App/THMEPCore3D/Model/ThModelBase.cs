using System.Collections.Generic;

namespace THMEPCore3D.Model
{
    public abstract class ThModelBase
    {
        protected Dictionary<string, string> Properties { get; set; }
        public ThModelBase()
        {
            Properties = new Dictionary<string, string>();
        }
        public ThModelBase(Dictionary<string, string> properties)
        {
            Properties = properties;
        }
        protected abstract void SetValue();
    }
}
