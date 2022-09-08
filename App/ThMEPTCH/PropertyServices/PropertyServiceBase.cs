using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices
{
    abstract class PropertyServiceBase : ITHProperty
    {
        public abstract string XDataAppName { get; }

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
            property = GetProperty(objectId);
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
            var tempProp = GetProperty(objectId);
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
            return SetXDataValue(objectId, property);
        }

        protected abstract PropertyBase DefaultProperties(ObjectId objectId);

        protected abstract PropertyBase XDataProperties(ObjectId objectId, TypedValueList typedValues);

        protected abstract TypedValueList PropertyToXDataValue(PropertyBase property);

        protected abstract PropertyVMBase PropertyToVM(PropertyBase property);

        protected TypedValueList GetXDataValue(ObjectId objectId)
        {
            var m_DocumentLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
            TypedValueList valueList = new TypedValueList();
            using (var acadDb = AcadDatabase.Active())
            {
                var dbObject = objectId.GetObject(OpenMode.ForRead, true);
                valueList = dbObject.GetXDataForApplication(XDataAppName);
            }
            m_DocumentLock.Dispose();
            return valueList;
        }

        protected bool SetXDataValue(ObjectId objectId,PropertyBase property)
        {
            var xdataValues = PropertyToXDataValue(property);
            if (xdataValues == null || xdataValues.Count < 1)
                return false;
            var m_DocumentLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
            using (var acadDb = AcadDatabase.Active())
            {
                objectId.AddXData(XDataAppName, xdataValues);
            }
            m_DocumentLock.Dispose();
            return true;
        }

        protected PropertyBase GetProperty(ObjectId objectId) 
        {
            PropertyBase property = null;
            var valueList = GetXDataValue(objectId);
            if (valueList == null || valueList.Count < 1)
            {
                property = DefaultProperties(objectId);
            }
            else
            {
                property = XDataProperties(objectId, valueList);
            }
            return property;
        }

        public bool CheckVaild(ObjectId objectId, string layer)
        {
            var isVaild = false;
            using (var acadDb = AcadDatabase.Active())
            {
                var entity = acadDb.ModelSpace.Element(objectId);
                if (null == entity || entity.IsErased)
                {
                    return isVaild;
                }
                if (entity is Curve polyline)
                {
                    if (polyline.Layer.Contains(layer))
                    {
                        isVaild = true;
                    }
                }
            }
            return isVaild;
        }
    }
}
