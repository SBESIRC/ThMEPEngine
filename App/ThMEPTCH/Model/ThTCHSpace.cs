using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.Model
{
    public class ThTCHSpace : ThTCHElement, ICloneable
    {
        private ThTCHSpace()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="height"></param>
        /// <param name="extVector"></param>
        public ThTCHSpace(MPolygon polygon)
        {
            Outline = polygon;
        }

        public ThTCHSpace(Polyline pline)
        {
            Outline = pline;
        }

        public object Clone()
        {
            var clone = new ThTCHSpace();
            clone.Uuid = this.Uuid;
            clone.Usage = this.Usage;
            clone.Name = this.Name;
            clone.ZOffSet = this.ZOffSet;
            clone.ExtrudedDirection = this.ExtrudedDirection;
            clone.Height = this.Height;
            clone.EnumMaterial = this.EnumMaterial;
            if (this.Outline != null)
                clone.Outline = this.Outline.Clone() as Entity;
            foreach (var item in this.Properties)
            {
                clone.Properties.Add(item.Key, item.Value);
            }
            return clone;
        }
    }
}
