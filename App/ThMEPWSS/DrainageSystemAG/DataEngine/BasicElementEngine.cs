using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;

namespace ThMEPWSS.DrainageSystemAG.DataEngine
{
    class BasicElementEngine
    {
        //List<BasicElementVisitor> basicModelSpaceVisitors;
        //List<BasicElementVisitor> externalVisitors;
        List<ElementFilterModel> basicModelSpaceVisitors;
        List<ElementFilterModel> externalVisitors;
        public BasicElementEngine()
        {
            basicModelSpaceVisitors = new List<ElementFilterModel>();
            externalVisitors = new List<ElementFilterModel>();

            InitModelSpaceLayerFilters();
            InitExternalFilters();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                ThBasicElementExtractor modelSpaceExtor = new ThBasicElementExtractor(EnumLoadDataSource.ModelSapce);
                foreach (var visitor in basicModelSpaceVisitors)
                    modelSpaceExtor.Accept(visitor.basicElementVisitor);
                modelSpaceExtor.Extract(acdb.Database);

                ThBasicElementExtractor exExternalExtor = new ThBasicElementExtractor(EnumLoadDataSource.External);
                foreach (var visitor in externalVisitors)
                    exExternalExtor.Accept(visitor.basicElementVisitor);
                exExternalExtor.Extract(acdb.Database);
            }
        }
        public List<Entity> GetAllEntity()
        {
            List<Entity> entities = new List<Entity>();
            foreach (var item in basicModelSpaceVisitors)
            {
                if (item == null || item.basicElementVisitor == null || item.basicElementVisitor.Results == null || item.basicElementVisitor.Results.Count < 1)
                    continue;
                foreach (var entity in item.basicElementVisitor.Results)
                {
                    var ent = entity.Geometry;
                    if (ent is Entity entity1)
                        entities.Add(entity1);
                }
            }
            foreach (var item in externalVisitors)
            {
                if (item == null || item.basicElementVisitor == null || item.basicElementVisitor.Results == null || item.basicElementVisitor.Results.Count < 1)
                    continue;
                foreach (var entity in item.basicElementVisitor.Results)
                {
                    var ent = entity.Geometry;
                    if (ent is Entity entity1)
                        entities.Add(entity1);
                }
            }
            return entities;
        }
        public List<Entity> GetAllEntity(Polyline polyline)
        {
            List<Entity> entities = new List<Entity>();
            entities.AddRange(GetModelSpaceEntity(polyline));
            entities.AddRange(GetExtractorEntity(polyline));
            return entities;
        }
        public Dictionary<EnumElementType, List<Entity>> GetAllTypeEntity(Polyline polyline, List<EnumElementType> filters = null)
        {
            var resKeyValue = new Dictionary<EnumElementType, List<Entity>>();
            var modelSpaceValues = GetModelSpaceTypeEntity(polyline, filters);
            if (null != modelSpaceValues && modelSpaceValues.Count > 0) 
            {
                foreach(var keyValue in modelSpaceValues) 
                {
                    if (keyValue.Value == null || keyValue.Value.Count < 1)
                        continue;
                    if(resKeyValue.Any(c=>c.Key == keyValue.Key)) 
                    {
                        var key = resKeyValue.Where(c => c.Key == keyValue.Key).FirstOrDefault().Key;
                        resKeyValue[key].AddRange(keyValue.Value);
                    }
                    else 
                    {
                        resKeyValue.Add(keyValue.Key, keyValue.Value);
                    }
                }
            }
            var extracotorValues = GetExtractorTypeEntity(polyline, filters);
            if (null != extracotorValues && extracotorValues.Count > 0)
            {
                foreach (var keyValue in extracotorValues)
                {
                    if (keyValue.Value == null || keyValue.Value.Count < 1)
                        continue;
                    if (resKeyValue.Any(c => c.Key == keyValue.Key))
                    {
                        var key = resKeyValue.Where(c => c.Key == keyValue.Key).FirstOrDefault().Key;
                        resKeyValue[key].AddRange(keyValue.Value);
                    }
                    else
                    {
                        resKeyValue.Add(keyValue.Key, keyValue.Value);
                    }
                }
            }
            return resKeyValue;
        }
        public List<Entity> GetModelSpaceEntity(Polyline polyline, List<EnumElementType> filters = null) 
        {
            List<Entity> entities = new List<Entity>();
            var modelSpaceValues = GetModelSpaceTypeEntity(polyline, filters);
            if (null == modelSpaceValues || modelSpaceValues.Count < 1)
                return entities;
            foreach (var keyValue in modelSpaceValues)
            {
                if (keyValue.Value == null || keyValue.Value.Count < 1)
                    continue;
                entities.AddRange(keyValue.Value);
            }
            return entities;
        }
        public List<Entity> GetExtractorEntity(Polyline polyline, List<EnumElementType> filters = null)
        {
            List<Entity> entities = new List<Entity>();
            var modelSpaceValues = GetExtractorTypeEntity(polyline, filters);
            if (null == modelSpaceValues || modelSpaceValues.Count < 1)
                return entities;
            foreach (var keyValue in modelSpaceValues)
            {
                if (keyValue.Value == null || keyValue.Value.Count < 1)
                    continue;
                entities.AddRange(keyValue.Value);
            }
            return entities;
        }

        public Dictionary<EnumElementType, List<Entity>> GetModelSpaceTypeEntity(Polyline polyline, List<EnumElementType> filters = null)
        {
            return GetTypeEntity(polyline, basicModelSpaceVisitors, filters);
        }
        public Dictionary<EnumElementType, List<Entity>> GetExtractorTypeEntity(Polyline polyline,List<EnumElementType> filters=null) 
        {
            return GetTypeEntity(polyline, externalVisitors, filters);
        }
        Dictionary<EnumElementType, List<Entity>> GetTypeEntity(Polyline polyline, List<ElementFilterModel> elementFilters, List<EnumElementType> filters = null)
        {
            var resKeyValue = new Dictionary<EnumElementType, List<Entity>>();
            var ntsGeo = polyline.ToNTSPolygon();
            foreach (var item in elementFilters)
            {
                if (item == null || item.basicElementVisitor == null || item.basicElementVisitor.Results == null || item.basicElementVisitor.Results.Count < 1)
                    continue;
                if (filters != null && filters.Count > 0 && !filters.Any(c => c == item.elementType))
                    continue;
                var entities = new List<Entity>();
                foreach (var entity in item.basicElementVisitor.Results)
                {
                    var ent = entity.Geometry;
                    if (ent is Entity entity1)
                    {
                        try
                        {
                            var entGeo = entity1.GeometricExtents.ToNTSPolygon();
                            if (ntsGeo.Intersects(entGeo))
                                entities.Add(entity1);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Equals("eNullExtents"))
                                continue;
                        }
                    }
                }
                if (null == entities || entities.Count < 1)
                    continue;
                resKeyValue.Add(item.elementType, entities);
            }
            return resKeyValue;
        }
        void InitModelSpaceLayerFilters() 
        {
            //添加 立管过滤
            BasicElementVisitor pipe = new BasicElementVisitor();
            ElementFilter elementFilter = new ElementFilter();
            elementFilter.AddAndFilter(new FilterBase("W-", EnumFilterType.IsContains));
            elementFilter.AddAndFilter(new FilterBase("EQPM", EnumFilterType.IsContains));
            pipe.AddLayerName(elementFilter);
            pipe.AddElementType(typeof(Circle));
            basicModelSpaceVisitors.Add(new ElementFilterModel(EnumElementType.ModelSpacePipe,pipe));

            //添加 编号过滤
            BasicElementVisitor label = new BasicElementVisitor();
            ElementFilter labelFilter = new ElementFilter();
            labelFilter.AddAndFilter(new FilterBase("W-", EnumFilterType.IsContains));
            labelFilter.AddAndFilter(new FilterBase("-NOTE", EnumFilterType.IsContains));
            label.AddLayerName(labelFilter);
            ElementFilter labelFilterElems = new ElementFilter();
            labelFilterElems.AddAndFilter(new FilterBase("FRPT", EnumFilterType.IsContains));
            labelFilterElems.AddAndFilter(new FilterBase("-EQPM", EnumFilterType.IsContains));
            label.AddLayerName(labelFilterElems);
            label.AddElementType(typeof(Line));
            label.AddElementType(typeof(Polyline));
            label.AddElementType(typeof(DBText));
            label.AddElementType(typeof(MText));
            basicModelSpaceVisitors.Add(new ElementFilterModel(EnumElementType.ModelSpaceNumber,label));

            //添加 管线
            ElementFilter lineFilter = new ElementFilter();
            lineFilter.AddAndFilter(new FilterBase("W-", EnumFilterType.IsContains));
            lineFilter.AddAndFilter(new FilterBase("-PIPE", EnumFilterType.IsContains));
            BasicElementVisitor pipeLine = new BasicElementVisitor();
            pipeLine.AddLayerName(lineFilter);
            pipeLine.AddElementType(typeof(Line));
            pipeLine.AddElementType(typeof(Polyline));
            basicModelSpaceVisitors.Add(new ElementFilterModel(EnumElementType.ModelSpacePipeLine,pipeLine));
        }

        void InitExternalFilters() 
        {
            //添加 空间名称
            var nameFilter = new ElementFilter(new FilterBase("AD-NAME-ROOM", EnumFilterType.IsContains));
            var spaceName = new BasicElementVisitor(new List<ElementFilter> { nameFilter });
            spaceName.AddElementType(typeof(DBText));
            spaceName.AddElementType(typeof(MText));
            externalVisitors.Add(new ElementFilterModel(EnumElementType.ExternalSpaceName,spaceName));

            //添加 横向轴网
            var axisFilter = new ElementFilter(new FilterBase("AD-AXIS-AXIS",EnumFilterType.IsContains));
            var axisVisitor = new BasicElementVisitor(new List<ElementFilter> { axisFilter });
            axisVisitor.AddElementType(typeof(Line));
            axisVisitor.AddElementType(typeof(Polyline));
            externalVisitors.Add(new ElementFilterModel(EnumElementType.ExternalLineAxis,axisVisitor));

            //添加 圆圈轴网
            var axisFilterCircle = new ElementFilter(new FilterBase("AD-AXIS-CRCL", EnumFilterType.IsContains));
            var axisVisitorCircle = new BasicElementVisitor(new List<ElementFilter> { axisFilterCircle });
            axisVisitorCircle.AddElementType(typeof(Circle));
            externalVisitors.Add(new ElementFilterModel(EnumElementType.ExternalCircleAxis,axisVisitorCircle));

            //添加 尺寸标注
            var dimFilter = new ElementFilter(new FilterBase("AE-DIMS-MAIN", EnumFilterType.IsContains));
            var dimFilter2 = new ElementFilter(new FilterBase("AE-DIMS-OTSD",EnumFilterType.IsContains));
            var dimVisitor = new BasicElementVisitor(new List<ElementFilter> { dimFilter, dimFilter2 });
            dimVisitor.AddElementType(typeof(Dimension));
            externalVisitors.Add(new ElementFilterModel(EnumElementType.ExternalDimension, dimVisitor));

            //添加面积文字识别
            var areaFilter = new ElementFilter(new FilterBase("AD-AREA-USNO", EnumFilterType.IsContains));
            var areaVisitor = new BasicElementVisitor(new List<ElementFilter> { areaFilter });
            areaVisitor.AddElementType(typeof(DBText));
            areaVisitor.AddElementType(typeof(MText));
            externalVisitors.Add(new ElementFilterModel(EnumElementType.ExternalAreaText, areaVisitor));

            //添加窗线识别
            var windowLineFilter = new ElementFilter(new FilterBase("AE-WIND", EnumFilterType.IsContains));
            var windowLineVisitor = new BasicElementVisitor(new List<ElementFilter> { windowLineFilter });
            windowLineVisitor.AddElementType(typeof(Line));
            windowLineVisitor.AddElementType(typeof(Polyline));
            externalVisitors.Add(new ElementFilterModel(EnumElementType.ExternalWindowLine, windowLineVisitor));

            //添加楼板线的识别
            var floorLineFilter = new ElementFilter(new FilterBase("AE-FLOR", EnumFilterType.IsContains));
            var floorLineVisitor = new BasicElementVisitor(new List<ElementFilter> { floorLineFilter });
            floorLineVisitor.AddElementType(typeof(Line));
            floorLineVisitor.AddElementType(typeof(Polyline));
            externalVisitors.Add(new ElementFilterModel(EnumElementType.ExternalFloorLine, floorLineVisitor));

            //添加设备线识别
            var equmLineFilter = new ElementFilter(new FilterBase("AE-EQPM", EnumFilterType.IsContains));
            var equmLineVisitor = new BasicElementVisitor(new List<ElementFilter> { equmLineFilter });
            equmLineVisitor.AddElementType(typeof(Line));
            equmLineVisitor.AddElementType(typeof(Polyline));
            externalVisitors.Add(new ElementFilterModel(EnumElementType.ExternalEqumLine, equmLineVisitor));
        }
    }
    class ElementFilterModel 
    {
        public EnumElementType elementType { get; }
        public BasicElementVisitor basicElementVisitor { get; }
        public ElementFilterModel(EnumElementType type,BasicElementVisitor visitor) 
        {
            this.elementType = type;
            this.basicElementVisitor = visitor;
        }
    }
    enum EnumElementType 
    {
        ModelSpacePipe=1,
        ModelSpaceNumber=2,
        ModelSpacePipeLine=3,
        ModelSpaceDimension=4,

        ExternalSpaceName=100,
        ExternalLineAxis=101,
        ExternalCircleAxis=102,
        ExternalDimension=103,
        ExternalAreaText=104,
        ExternalWindowLine = 105,
        ExternalFloorLine =106,
        ExternalEqumLine=107,
    }
}
