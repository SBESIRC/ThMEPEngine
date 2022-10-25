using System;
using System.Runtime.Serialization;

namespace ThAnalytics.SDK
{
    /// <summary>
    /// Holds data about segmentation value
    /// </summary>
    [DataContractAttribute]
    public class SegmentationItem : IComparable<SegmentationItem>
    {
        /// <summary>
        /// Segmentation key
        /// </summary>
        [DataMemberAttribute]
        public string Key { get; set; }

        /// <summary>
        /// Segmentation value
        /// </summary>
        [DataMemberAttribute]
        public string Value { get; set; }

        /// <summary>
        /// Creates object with provided values
        /// </summary>
        /// <param name="Key">Segmentation key</param>
        /// <param name="Value">Segmentation value</param>
        public SegmentationItem(string Key, string Value)
        {
            this.Key = Key;
            this.Value = Value;
        }

        public int CompareTo(SegmentationItem other)
        {
            if (!(Key == null && other.Key == null))
            {
                if (Key == null) { return -1; }
                if (other.Key == null) { return 1; }
                if (!Key.Equals(other.Key)) { return Key.CompareTo(other.Key); }
            }

            if (!(Value == null && other.Value == null))
            {
                if (Value == null) { return -1; }
                if (other.Value == null) { return 1; }
                if (!Value.Equals(other.Value)) { return Value.CompareTo(other.Value); }
            }

            return 0;
        }
    }
}
