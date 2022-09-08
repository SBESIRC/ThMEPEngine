using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices
{
    public class PropertyService
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
                if (svr.Instance == null)
                    svr.Instance = Activator.CreateInstance(svr.PType) as ITHProperty;
                if (null == svr.Instance)
                    continue;
                if (!svr.Instance.CheckVaild(objectId))
                    continue;
                LastSvrCache = svr.Instance;
                svr.Instance.GetVMProperty(objectId, out properties, false);
                isVaild = true;
                break;
            }
            return isVaild;
        }

        public PropertyVMBase GetNoSelectVMProperty()
        {
            var propertyVM = new NoPropertyVM("未选择");
            propertyVM.A01_ShowTypeName = "=未选择=";
            return propertyVM;
        }

        public PropertyVMBase GetMultiSelectVMProperty()
        {
            var propertyVM = new NoPropertyVM("多类别");
            propertyVM.A01_ShowTypeName = "多类别(*)";
            return propertyVM;
        }
    }
    class PropertySvrCache
    {
        public string TypeName { get; }

        public string Tag { get; }

        public Type PType { get; }

        public ITHProperty Instance { get; set; }

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
