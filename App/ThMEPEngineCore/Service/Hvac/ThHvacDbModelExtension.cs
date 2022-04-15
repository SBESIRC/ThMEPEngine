using Linq2Acad;
using DotNetARX;
using System.Linq;
using System.Text;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service.Hvac
{
    public static class ThHvacDbModelExtension
    {
        public static ObjectId InsertModel(this Database database, string name, string layer, Dictionary<string, string> attNameValues)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layer,
                    name,
                    Point3d.Origin,
                    new Scale3d(1.0),
                    0.0, 
                    attNameValues);
            }
        }

        public static void ImportModel(this Database database, string name, string layer)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacModelDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
            }
        }

        public static void SetModelIdentifier(this ObjectId obj, string identifier, int number, string style, string scenario)
        {
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString,     identifier },
                { (int)DxfCode.ExtendedDataInteger32,       number },
                { (int)DxfCode.ExtendedDataBinaryChunk,     Encoding.UTF8.GetBytes(style) },
                { (int)DxfCode.ExtendedDataBinaryChunk,     Encoding.UTF8.GetBytes(scenario) },
            };
            obj.AddXData(ThHvacCommon.RegAppName_FanSelection, valueList);
        }
        public static void SetModelIdentifier(this ObjectId obj, TypedValueList valueList, string regAppName = ThHvacCommon.RegAppName_FanSelection)
        {
            obj.AddXData(regAppName, valueList);
        }
        public static void UpdateModelNumber(this ObjectId obj, int number)
        {
            var oldValue = obj.GetModelNumber();
            if (oldValue > 0 && (oldValue != number))
            {
                obj.ModXData(
                    ThHvacCommon.RegAppName_FanSelection,
                    DxfCode.ExtendedDataInteger32,
                    oldValue, number);
            }
        }

        public static void UpdateModelIdentifier(this ObjectId obj, string identifier)
        {
            var oldValue = obj.GetModelIdentifier();
            if (!string.IsNullOrEmpty(identifier) && (oldValue != identifier))
            {
                obj.ModXData(
                    ThHvacCommon.RegAppName_FanSelection,
                    DxfCode.ExtendedDataAsciiString,
                    oldValue, identifier);
            }
        }

        public static void UpdateModelIdentifier(this DBObject dBObject, string identifier)
        {
            var oldValue = dBObject.GetModelIdentifier();
            if (!string.IsNullOrEmpty(identifier) && (oldValue != identifier))
            {
                dBObject.ModXData(
                    ThHvacCommon.RegAppName_FanSelection,
                    DxfCode.ExtendedDataAsciiString,
                    oldValue, identifier);
            }
        }

        public static string GetModelIdentifier(this ObjectId obj, string regAppName = ThHvacCommon.RegAppName_FanSelection)
        {
            var model = obj.GetObject(OpenMode.ForRead, true);
            return model.GetModelIdentifier(regAppName);
        }

        public static string GetModelIdentifier(this DBObject dBObject,string regAppName = ThHvacCommon.RegAppName_FanSelection)
        {
            var valueList = dBObject.GetXData(regAppName);
            if (valueList == null)
            {
                return string.Empty;
            }

            var values = valueList.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
            {
                return string.Empty;
            }

            return (string)values.ElementAt(0).Value;
        }

        private static TypedValueList GetXData(this DBObject dBObject, string regAppName)
        {
            return dBObject.GetXDataForApplication(regAppName);
        }

        public static void SetModelXDataFrom(this ObjectId obj, ObjectId other, string regAppName = ThHvacCommon.RegAppName_FanSelection)
        {
            var xdata = other.GetXData(regAppName);
            obj.AddXData(regAppName, xdata);
        }

        public static bool IsModel(this DBObject dBObject)
        {
            return !string.IsNullOrEmpty(dBObject.GetModelIdentifier());
        }
        public static bool IsModel(this ObjectId obj, string regAppName = ThHvacCommon.RegAppName_FanSelectionEx)
        {
            var model = obj.GetObject(OpenMode.ForRead, true);
            return !string.IsNullOrEmpty(model.GetModelIdentifier(regAppName));
        }

        public static bool IsModel(this ObjectId obj, string identifier, string regAppName = ThHvacCommon.RegAppName_FanSelectionEx)
        {
            var model = obj.GetObject(OpenMode.ForRead, true);
            return model.GetModelIdentifier(regAppName) == identifier;
        }

        public static void EraseModel(this ObjectId obj, bool erasing = true)
        {
            var model = obj.GetObject(OpenMode.ForWrite, true);
            model.Erase(erasing);
        }

        public static void RemoveModel(this ObjectId obj)
        {
            var model = obj.GetObject(OpenMode.ForWrite);
            model.RemoveXData(ThHvacCommon.RegAppName_FanSelection);
            model.Erase();
        }

        private static void RemoveXData(this DBObject dBObject, string regAppName)
        {
            TypedValueList xdata = dBObject.GetXData(regAppName);
            if (xdata != null)// 如果有扩展数据
            {
                // 新建一个TypedValue列表，并只添加注册应用程序名扩展数据项
                TypedValueList newValues = new TypedValueList();
                newValues.Add(DxfCode.ExtendedDataRegAppName, regAppName);
                dBObject.XData = newValues; //为对象的XData属性重新赋值，从而删除扩展数据 
            }
        }

        public static void ModXData(this DBObject dBObject, string regAppName, DxfCode code, object oldValue, object newValue)
        {
            // 获取regAppName注册应用程序下的扩展数据列表
            TypedValueList xdata = dBObject.GetXDataForApplication(regAppName);
            for (int i = 0; i < xdata.Count; i++)// 遍历扩展数据列表
            {
                TypedValue tv = xdata[i]; //扩展数据
                //判断扩展数据的类型和值是否满足条件
                if (tv.TypeCode == (short)code && tv.Value.Equals(oldValue))
                {
                    // 设置新的扩展数据值
                    xdata[i] = new TypedValue(tv.TypeCode, newValue);
                    break; //要修改的数据已找到，跳出循环
                }
            }
            dBObject.XData = xdata; // 覆盖原来的扩展数据，达到修改的目的
        }

        public static string GetModelStyle(this ObjectId obj)
        {
            var valueList = obj.GetXData(ThHvacCommon.RegAppName_FanSelection);
            if (valueList == null)
            {
                return string.Empty;
            }

            var typedValue = valueList.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataBinaryChunk).FirstOrDefault();
            if (typedValue == null)
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(typedValue.Value as byte[]);
        }

        public static string GetModelScenario(this ObjectId obj)
        {
            var valueList = obj.GetXData(ThHvacCommon.RegAppName_FanSelection);
            if (valueList == null)
            {
                return string.Empty;
            }

            var typedValue = valueList.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataBinaryChunk).LastOrDefault();
            if (typedValue == null)
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(typedValue.Value as byte[]);
        }

        public static bool IsHTFCModel(this ObjectId obj)
        {
            var style = GetModelStyle(obj);
            return style.Contains(ThHvacCommon.HTFC_TYPE_NAME);
        }

        public static bool IsAXIALModel(this ObjectId obj)
        {
            var style = GetModelStyle(obj);
            return style.Contains(ThHvacCommon.AXIAL_TYPE_NAME);
        }

        public static bool IsRawModel(this DBObject dBObject)
        {
            // 仅测试用，支持非天华风机选型生成的风机
            if (dBObject is BlockReference reference)
            {
                var blockName = reference.GetEffectiveName();
                return (blockName.Contains(ThHvacCommon.HTFC_BLOCK_NAME)
                    || blockName.Contains(ThHvacCommon.AXIAL_BLOCK_NAME));
            }
            return false;
        }

        public static bool IsRawHTFCModel(this ObjectId obj)
        {
            // 仅测试用，支持非天华风机选型生成的风机
            var dBObject = obj.GetObject(OpenMode.ForRead);
            if (dBObject is BlockReference reference)
            {
                var blockName = reference.GetEffectiveName();
                return blockName.Contains(ThHvacCommon.HTFC_BLOCK_NAME);
            }
            return false;
        }

        public static bool IsRawAXIALModel(this ObjectId obj)
        {
            // 仅测试用，支持非天华风机选型生成的风机
            var dBObject = obj.GetObject(OpenMode.ForRead);
            if (dBObject is BlockReference reference)
            {
                var blockName = reference.GetEffectiveName();
                return blockName.Contains(ThHvacCommon.AXIAL_BLOCK_NAME);
            }
            return false;
        }

        public static int GetModelNumber(this ObjectId obj)
        {
            var model = obj.GetObject(OpenMode.ForRead, true);
            return model.GetModelNumber();
        }

        public static int GetModelNumber(this DBObject dBObject)
        {
            var valueList = dBObject.GetXData(ThHvacCommon.RegAppName_FanSelection);
            if (valueList == null)
            {
                return 0;
            }

            var values = valueList.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataInteger32);
            if (!values.Any())
            {
                return 0;
            }

            return (int)values.ElementAt(0).Value;
        }

        public static void ModifyModelAttributes(this ObjectId obj, Dictionary<string, string> attributes)
        {
            obj.UpdateAttributesInBlock(attributes);
        }
    }
}
