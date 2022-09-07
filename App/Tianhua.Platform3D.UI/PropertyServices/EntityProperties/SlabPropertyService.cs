using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using Tianhua.Platform3D.UI.PropertyServices.PropertyModels;
using Tianhua.Platform3D.UI.PropertyServices.PropertyVMoldels;

namespace Tianhua.Platform3D.UI.PropertyServices.EntityProperties
{
    [PropertyAttribute("楼板", "")]
    class SlabPropertyService : ITHProperty
    {
        public string ShowTypeName => "楼板";
        public string XDataAppName => "THProperty";
        public bool GetProperty(ObjectId objectId, out PropertyBase property)
        {
            property = null;
            var isVaild = CheckVaild(objectId);
            if(!isVaild)
                return false;
            property = GetProperty(objectId);
            return true;
        }

        public bool GetVMProperty(ObjectId objectId, out PropertyVMBase property)
        {
            property = null;
            var isVaild = CheckVaild(objectId);
            if (!isVaild)
                return false;
            var tempProp = GetProperty(objectId);
            property = PropertyToVM(tempProp as SlabProperty);
            return true;
        }

        public bool SetProperty(ObjectId objectId, PropertyBase property)
        {
            var isVaild = CheckVaild(objectId);
            if (!isVaild)
                return false;
            var m_DocumentLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
            using (var acadDb = AcadDatabase.Active())
            {
                objectId.AddXData(XDataAppName, XDataValueList(property as SlabProperty));
            }
            m_DocumentLock.Dispose();
            return true;
        }

        public bool CheckVaild(ObjectId objectId)
        {
            bool isVaild = false;
            using (var acadDb = AcadDatabase.Active())
            {
                var entity = acadDb.ModelSpace.Element(objectId);
                if (null == entity || entity.IsErased)
                {
                    return isVaild;
                }
                if (entity is Curve polyline)
                {
                    if (polyline.Layer.Contains("楼板"))
                    {
                        isVaild = true;
                    }
                }
            }
            return isVaild;
        }
        public PropertyBase GetProperty(ObjectId objectId) 
        {
            SlabProperty property = null;
            using (var acadDb = AcadDatabase.Active())
            {
                TypedValueList valueList = new TypedValueList();
                var dbObject = objectId.GetObject(OpenMode.ForRead, true);
                valueList = dbObject.GetXDataForApplication(XDataAppName);
                if (valueList == null || valueList.Count < 1)
                {
                    property = DefaultProperties(objectId) as SlabProperty;
                }
                else 
                {
                    property = new SlabProperty(objectId);
                    //读取XData（第0个是AppName，不需要读取）,这边读数据的顺序要和写入数据的顺序一致
                    for (int i = 1; i < valueList.Count; i++)
                    {
                        var strData = valueList.ElementAt(i).Value.ToString();
                        switch (i)
                        {
                            case 1:
                                var enumInt = int.Parse(strData);
                                property.Material = strData;
                                property.EnumMaterial = (Enums.EnumSlabMaterial)enumInt;
                                break;
                            case 2:
                                property.SlabTopElevation = double.Parse(strData);
                                break;
                            case 3:
                                property.SlabThickness = double.Parse(strData);
                                break;
                            case 4:
                                property.SlabBuildingSurfaceThickness = double.Parse(strData);
                                break;
                        }
                    }
                }
            }
            return property;
        }
        public PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new SlabProperty(objectId);
            property.Material = "材质";
            property.EnumMaterial = Enums.EnumSlabMaterial.ReinforcedConcrete;
            property.SlabTopElevation = 0.0;
            property.SlabThickness = 100.0;
            property.SlabBuildingSurfaceThickness = 50.0;
            return property;
        }
        
        public PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties)
        {
            PropertyVMBase propertyVM = null;
            var allSlabVMs = properties.OfType<SlabPropertyVM>().ToList();
            if (allSlabVMs.Count < 1)
                return null;
            propertyVM = allSlabVMs.First().Clone() as SlabPropertyVM;
            return propertyVM;
        }
        private PropertyVMBase PropertyToVM(SlabProperty property)
        {
            var vmProp = new SlabPropertyVM(ShowTypeName, property);
            return vmProp;
        }
        private TypedValueList XDataValueList(SlabProperty property)
        {
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, ((int)property.EnumMaterial).ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, property.SlabTopElevation.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, property.SlabThickness.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, property.SlabBuildingSurfaceThickness.ToString()},
            };
            return valueList;
        }
    }
}
