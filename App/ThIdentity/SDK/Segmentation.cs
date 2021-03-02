using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ThIdentity.SDK
{
    /// <summary>
    /// Holds an array of segmentation values
    /// </summary>
    [DataContractAttribute]
    public class Segmentation : IComparable<Segmentation>
    {
        /// <summary>
        /// Segmenation array
        /// </summary>
        [DataMemberAttribute]
        public List<SegmentationItem> segmentation { get; set; }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        public Segmentation()
        {
            segmentation = new List<SegmentationItem>();
        }

        /// <summary>
        /// Add new segmentation value
        /// </summary>
        /// <param name="Key">Segmenation key</param>
        /// <param name="Value">Segmenation value</param>
        public void Add(string Key, string Value)
        {
            segmentation.Add(new SegmentationItem(Key, Value));
        }

        public int CompareTo(Segmentation other)
        {
            if (!(segmentation == null && other.segmentation == null))
            {
                if (segmentation == null) { return -1; }
                if (other.segmentation == null) { return 1; }
                if (!segmentation.Count.Equals(other.segmentation.Count)) { return segmentation.Count.CompareTo(other.segmentation.Count); }

                for (int a = 0; a < segmentation.Count; a++)
                {
                    if (!segmentation[a].Equals(other.segmentation[a])) { return segmentation[a].CompareTo(other.segmentation[a]); }
                }
            }

            return 0;
        }
    }
}
