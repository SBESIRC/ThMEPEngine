using System;

namespace TianHua.Electrical.PDS.UI.Helpers
{
    public class ThPDSCustomField
    {
        public ThPDSCustomField(string name, Type dataType)
        {
            Name = name;
            DataType = dataType;
        }

        public string Name { get; private set; }

        public Type DataType { get; private set; }
    }
}
