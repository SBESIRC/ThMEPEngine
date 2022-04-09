﻿using System.Reflection;
using System.ComponentModel;

namespace TianHua.Electrical.PDS.UI.Helpers
{
    // https://stackoverflow.com/questions/4690481/conditional-browsable-attribute
    public static class ThPDSPropertyDescriptorHelper
    {
        /// <summary>
        /// Set the Browsable property.
        /// NOTE: Be sure to decorate the property with [Browsable(true)]
        /// </summary>
        /// <param name="PropertyName">Name of the variable</param>
        /// <param name="bIsBrowsable">Browsable Value</param>
        public static void SetBrowsableProperty<T>(string strPropertyName, bool bIsBrowsable)
        {
            // Get the Descriptor's Properties
            PropertyDescriptor theDescriptor = TypeDescriptor.GetProperties(typeof(T))[strPropertyName];

            // Get the Descriptor's "Browsable" Attribute
            BrowsableAttribute theDescriptorBrowsableAttribute = (BrowsableAttribute)theDescriptor.Attributes[typeof(BrowsableAttribute)];
            FieldInfo isBrowsable = theDescriptorBrowsableAttribute.GetType().GetField("Browsable", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);

            // Set the Descriptor's "Browsable" Attribute
            isBrowsable.SetValue(theDescriptorBrowsableAttribute, bIsBrowsable);
        }
    }
}
