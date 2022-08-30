using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tianhua.Platform3D.UI.PropertyServices
{
    class PropertyService
    {
        private string assemblyPath = "";
        private List<PropertySvrCache> propertySvrCaches { get; }
        public PropertyService()
        {
            assemblyPath = this.GetType().Assembly.Location.ToString();
            propertySvrCaches = new List<PropertySvrCache>();
            CacheService(assemblyPath);
        }
        void CacheService(string dllPath)
        {
            var types = PropertyAttribute.GetAvailabilityTypes(dllPath);
            if (null == types || types.Count < 1)
                return;
            foreach (var type in types)
            {
                if (type.CustomAttributes == null || type.CustomAttributes.Count() < 1)
                    continue;
                var allAttr = type.GetCustomAttributes(typeof(PropertyAttribute), false);
                if (allAttr == null || allAttr.Count() < 1)
                    continue;
                var temp = new PropertySvrCache(type);
                propertySvrCaches.Add(temp);
            }
        }
        public string GetShowTypeProperties(ObjectId objectId, out Dictionary<string, object> properties)
        {
            string resType = string.Empty;
            properties = new Dictionary<string, object>();
            foreach (var svr in propertySvrCaches)
            {
                if (svr.Instacne == null)
                    svr.Instacne = Activator.CreateInstance(svr.PType) as ITHProperty;
                if (null == svr.Instacne)
                    continue;
                svr.Instacne.InitObjectId(objectId);
                svr.Instacne.CheckAndGetData();
                if (!svr.Instacne.IsVaild)
                    continue;
                resType = svr.TypeName;
                foreach (var item in svr.Instacne.Properties)
                {
                    properties.Add(item.Key, item.Value);
                }
                break;
            }
            return resType;
        }
    }
    class PropertySvrCache
    {
        public string TypeName { get; }
        public string Tag { get; }
        public Type PType { get; }
        public ITHProperty Instacne { get; set; }
        public PropertySvrCache(Type type)
        {
            var allAttr = type.GetCustomAttributes(typeof(PropertyAttribute), false);
            var attr = allAttr.First(j => j is PropertyAttribute) as PropertyAttribute;
            this.TypeName = attr.TypeName;
            this.Tag = attr.Tag;
            this.PType = type;
        }
    }
}
