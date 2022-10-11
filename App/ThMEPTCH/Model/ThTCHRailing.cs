using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.Model
{
    /// <summary>
    /// 栏杆
    /// </summary>
    public class ThTCHRailing : ThTCHElement, ICloneable
    {
        public object Clone()
        {
            var clone = new ThTCHRailing();
            clone.Uuid = this.Uuid;
            clone.Usage = this.Usage;
            if (null != this.Outline)
                clone.Outline = this.Outline.Clone() as Entity;
            clone.ExtrudedDirection = this.ExtrudedDirection;
            clone.Length = this.Length;
            clone.Height = this.Height;
            clone.Name = this.Name;
            clone.Origin = this.Origin;
            clone.Width = this.Width;
            clone.XVector = this.XVector;
            clone.ZOffSet = this.ZOffSet;
            foreach (var item in this.Properties)
                clone.Properties.Add(item.Key, item.Value);
            return clone;
        }
    }
}
