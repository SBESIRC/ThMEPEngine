using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices
{
    abstract class PropertyUIServiceBase : ITHProperty
    {
        protected PropertyServiceBase serviceBase;//每个里面要给serviceBase 赋值
        public abstract string ShowTypeName { get; }
        public abstract bool CheckVaild(ObjectId objectId);
        public virtual bool GetProperty(ObjectId objectId, out PropertyBase property, bool checkId)
        {
            property = null;
            if (checkId)
            {
                var isVaild = CheckVaild(objectId);
                if (!isVaild)
                    return false;
            }
            property = serviceBase.GetProperty(objectId);
            return true;
        }
        public virtual bool GetVMProperty(ObjectId objectId, out PropertyVMBase property, bool checkId)
        {
            property = null;
            if (checkId)
            {
                var isVaild = CheckVaild(objectId);
                if (!isVaild)
                    return false;
            }
            var tempProp = serviceBase.GetProperty(objectId);
            property = PropertyToVM(tempProp);
            return true;
        }
        public abstract PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties);
        public virtual bool SetProperty(ObjectId objectId, PropertyBase property, bool checkId)
        {
            if (checkId)
            {
                var isVaild = CheckVaild(objectId);
                if (!isVaild)
                    return false;
            }
            return serviceBase.SetProperty(objectId, property);
        }
        protected abstract PropertyVMBase PropertyToVM(PropertyBase property);
        protected bool CheckCurveLayerVaild(ObjectId objectId, string layer)
        {
            var isVaild = false;
            using (var acadDb = AcadDatabase.Active())
            {
                var entity = acadDb.ModelSpace.Element(objectId);
                if (null == entity || entity.IsErased)
                {
                    return isVaild;
                }
                if (entity is Polyline polyline)
                {
                    if (polyline.Layer.Equals(layer))
                    {
                        isVaild = true;
                    }
                }
            }
            return isVaild;
        }
        protected bool CheckTCHEntityVaild(ObjectId objectId, Func<Entity, bool> f)
        {
            var isVaild = false;
            using (var acadDb = AcadDatabase.Active())
            {
                var entity = acadDb.ModelSpace.Element(objectId);
                if (null == entity || entity.IsErased)
                {
                    return isVaild;
                }
                if (f(entity))
                {
                    isVaild = true;
                }
            }
            return isVaild;
        }
    }
}
