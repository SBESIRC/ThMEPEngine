﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPTCH.PropertyServices.PropertyEnums;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHSlab : ThTCHElement, ICloneable
    {
        /// <summary>
        /// 降板信息
        /// </summary>
        [ProtoMember(21)]
        public List<ThTCHDescending> Descendings { get; set; }

        private ThTCHSlab()
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="thickness"></param>
        /// <param name="extVector"></param>
        public ThTCHSlab(MPolygon polygon, double thickness, Vector3d extVector)
        {
            Outline = polygon;
            Height = thickness;
            ExtrudedDirection = extVector;
            Descendings = new List<ThTCHDescending>();
            EnumMaterial = EnumSlabMaterial.ReinforcedConcrete.GetDescription();
        }

        public ThTCHSlab(Polyline pline, double thickness, Vector3d extVector)
        {
            Outline = pline;
            Height = thickness;
            ExtrudedDirection = extVector;
            Descendings = new List<ThTCHDescending>();
            EnumMaterial = EnumSlabMaterial.ReinforcedConcrete.GetDescription();
        }

        public object Clone()
        {
            var clone = new ThTCHSlab();
            clone.Descendings = new List<ThTCHDescending>();
            clone.Uuid = this.Uuid;
            clone.Usage = this.Usage;
            clone.ZOffSet = this.ZOffSet;
            clone.ExtrudedDirection = this.ExtrudedDirection;
            clone.Height = this.Height;
            clone.EnumMaterial = this.EnumMaterial;
            if (this.Outline != null)
                clone.Outline = this.Outline.Clone() as Entity;
            if (null != this.Descendings)
            {
                foreach (var item in this.Descendings)
                {
                    clone.Descendings.Add(item.Clone() as ThTCHDescending);
                }
            }
            foreach (var item in this.Properties)
            {
                clone.Properties.Add(item.Key, item.Value);
            }
            return clone;
        }
    }
}
