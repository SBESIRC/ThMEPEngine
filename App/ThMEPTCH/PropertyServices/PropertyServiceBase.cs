using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices
{
    abstract class PropertyServiceBase
    {
        public virtual string XDataAppName => "THBim";
        public virtual PropertyBase GetProperty(ObjectId objectId) 
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
        public virtual bool SetProperty(ObjectId objectId, PropertyBase property)
        {
            return SetXDataValue(objectId, property);
        }
        protected abstract PropertyBase DefaultProperties(ObjectId objectId);
        protected abstract PropertyBase XDataProperties(ObjectId objectId, TypedValueList typedValues);
        protected abstract TypedValueList PropertyToXDataValue(PropertyBase property);
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
    }
}
