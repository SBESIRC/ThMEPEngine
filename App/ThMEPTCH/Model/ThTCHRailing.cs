﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    /// <summary>
    /// 栏杆
    /// </summary>
    [ProtoContract]
    public class ThTCHRailing : ThTCHElement, ICloneable
    {
        public object Clone()
        {
            var clone = new ThTCHRailing();
            clone.Uuid = this.Uuid;
            clone.Useage = this.Useage;
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
