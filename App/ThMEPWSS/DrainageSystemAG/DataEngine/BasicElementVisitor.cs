using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.DrainageSystemAG.DataEngine
{
    class BasicElementVisitor : ThAnnotationElementExtractionVisitor
    {
        //判断类型（1，相等，2包含，3已改字符串开头，4已改字符串结尾）
        protected List<ElementFilter> layerNamesFilters;
        List<object> filterTypes;
        public BasicElementVisitor(List<ElementFilter> filters=null) 
        {
            filterTypes = new List<object>();
            layerNamesFilters = new List<ElementFilter>();
            if (null != filters && filters.Count > 0)
            {
                foreach (var filter in filters)
                {
                    if (filter == null || filter.filters == null || filter.filters.Count < 1)
                        continue;
                    this.layerNamesFilters.Add(filter);
                }
            }
        }
        /// <summary>
        /// 图层名名称添加
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <param name="judgeType">
        /// 判断类型（1，相等，2包含，3已该字符串开头，4已该字符串结尾）
        /// </param>
        public void AddLayerName(ElementFilter filter)
        {
            if (filter == null)
                return;
            this.layerNamesFilters.Add(filter);
        }
        public void AddElementType(Type type) 
        {
            if (null == type)
                return;
            filterTypes.Add(type);
        }
        public override bool CheckLayerValid(Entity entity)
        {
            if (layerNamesFilters == null || layerNamesFilters.Count < 1)
                return false;
            if (entity is BlockReference)
                return false;
            var name = entity.Layer;
            name = ThMEPXRefService.OriginalFromXref(entity.Layer);
            if (entity.ToString().ToUpper().Contains("DBTEXT")) 
            {
                var str = ((DBText)entity).TextString;
                if (str.Contains("卫生")) 
                {
                }
            }
            if (string.IsNullOrEmpty(name))
                return false;
            bool isAdd = false;
            foreach (var filter in this.layerNamesFilters)
            {
                if (isAdd)
                    break;
                bool isFilter = true;
                foreach (var andFiler in filter.filters) 
                {
                    if (!isFilter)
                        break;
                    switch (andFiler.filterType) 
                    {
                        case EnumFilterType.IsEquals:
                            isFilter = name.Equals(andFiler.filterValue);
                            break;
                        case EnumFilterType.IsContains:
                            isFilter = name.Contains(andFiler.filterValue);
                            break;
                        case EnumFilterType.IsStartsWith:
                            isFilter = name.StartsWith(andFiler.filterValue);
                            break;
                        case EnumFilterType.IsEndsWith:
                            isFilter = name.EndsWith(andFiler.filterValue);
                            break;
                    }
                }
                isAdd = isFilter;
            }
            if (!isAdd)
                return false;
            var typeName = entity.GetType().ToString().ToUpper();
            isAdd = false;
            foreach (var type in filterTypes) 
            {
                if (isAdd)
                    break;
                var strType = type.ToString().ToUpper();
                isAdd = typeName.Equals(strType);
            }
            return isAdd;
        }
        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (null == dbObj || !IsAnnotationElement(dbObj) || !CheckLayerValid(dbObj) || !CheckLayerValid(dbObj))
                return;
            Entity clone = dbObj.GetTransformedCopy(matrix);
            elements.Add(new ThRawIfcAnnotationElementData
            {
                Data = dbObj.ObjectId,
                Geometry = clone
            });
        }

        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj)
        {
            if (null == dbObj || !IsAnnotationElement(dbObj) || !CheckLayerValid(dbObj) || !CheckLayerValid(dbObj))
                return;
            Entity clone = dbObj.GetTransformedCopy(Matrix3d.Identity);
            elements.Add(new ThRawIfcAnnotationElementData
            {
                Data = dbObj.ObjectId,
                Geometry = clone
            });
        }

        public override void DoXClip(List<ThRawIfcAnnotationElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o =>
                {
                    if (null == o)
                        return true;
                    var point = GetEntityPoint(o.Geometry as Entity);
                    if (point == null || !point.HasValue)
                        return true;
                    return !xclip.Contains(point.Value);
                });
            }
        }
        Point3d? GetEntityPoint(Entity entity) 
        {
            if (entity != null) 
            {
            
            }
            if (entity is DBText dbText)
            {
                return dbText.Position;
            }
            else if (entity is MText mText)
            {
                return mText.Location;
            }
            else if (entity is Circle circle)
            {
                return circle.Center;
            }
            else if (entity is Arc arc)
            {
                return arc.Center;
            }
            else if (entity is Polyline pLine)
            {
                return pLine.StartPoint;
            }
            else if (entity is Line line)
            {
                return line.StartPoint;
            }
            else 
            {
                return null;
            }
        }
    }
    class ElementFilter
    {
        public string Uid { get; }
        public List<FilterBase> filters { get; }
        public ElementFilter() :this(null)
        {
            
        }
        public ElementFilter(FilterBase filter)
        {
            this.Uid = Guid.NewGuid().ToString();
            this.filters = new List<FilterBase>();
            if (filter == null)
                return;
            this.filters.Add(filter);
        }
        public void AddAndFilter(FilterBase filter) 
        {
            if (filter == null)
                return;
            this.filters.Add(filter);
        }

    }
    class FilterBase
    {
        public string filterValue { get; }
        public EnumFilterType filterType { get; }
        public FilterBase(string filterValue, EnumFilterType filterType) 
        {
            this.filterValue = filterValue;
            this.filterType = filterType;
        }
    }
    enum EnumFilterType 
    {
        IsEquals=1,
        IsContains = 2,
        IsStartsWith=3,
        IsEndsWith=4,
    }
}
