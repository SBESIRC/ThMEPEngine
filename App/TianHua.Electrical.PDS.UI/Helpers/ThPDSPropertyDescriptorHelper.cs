using System.Reflection;
using System.ComponentModel;

namespace TianHua.Electrical.PDS.UI.Helpers
{
    // https://stackoverflow.com/questions/4690481/conditional-browsable-attribute
    // https://www.codeproject.com/Tips/48015/Exploring-the-Behaviour-of-Property-Grid
    public static class ThPDSPropertyDescriptorHelper
    {
        public static void SetBrowsableProperty<T>(string strPropertyName, bool bIsBrowsable)
        {
            // Get the Descriptor's Properties
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(typeof(T))[strPropertyName];

            // Get the Descriptor's "Browsable" Attribute
            BrowsableAttribute attrib = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
            FieldInfo isBrowsable = attrib.GetType().GetField("Browsable", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);

            // Set the Descriptor's "Browsable" Attribute
            isBrowsable.SetValue(attrib, bIsBrowsable);
        }

        public static void SetReadOnlyProperty<T>(string strPropertyName, bool bIsReadOnly)
        {
            // Get the Descriptor's Properties
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(typeof(T))[strPropertyName];

            // Get the Descriptor's "ReadOnly" Attribute
            ReadOnlyAttribute attrib = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
            FieldInfo isReadOnly = attrib.GetType().GetField("IsReadOnly", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);

            // Set the Descriptor's "ReadOnly" Attribute
            isReadOnly.SetValue(attrib, bIsReadOnly);
        }
    }
}
