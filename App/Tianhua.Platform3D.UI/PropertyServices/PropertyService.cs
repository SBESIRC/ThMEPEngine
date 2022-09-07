using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using Tianhua.Platform3D.UI.PropertyServices.PropertyVMoldels;

namespace Tianhua.Platform3D.UI.PropertyServices
{
    class PropertyService
    {
        private string assemblyPath = "";
        private List<PropertySvrCache> propertySvrCaches { get; }
        public ITHProperty LastSvrCache { get; protected set; }
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
        public bool GetShowProperties(ObjectId objectId, out PropertyVMBase properties)
        {
            bool isVaild = false;
            properties = null;
            foreach (var svr in propertySvrCaches)
            {
                if (svr.Instacne == null)
                    svr.Instacne = Activator.CreateInstance(svr.PType) as ITHProperty;
                if (null == svr.Instacne)
                    continue;
                if (!svr.Instacne.CheckVaild(objectId))
                    continue;
                LastSvrCache = svr.Instacne;
                svr.Instacne.GetVMProperty(objectId, out properties);
                isVaild = true;
                break;
            }
            return isVaild;
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
