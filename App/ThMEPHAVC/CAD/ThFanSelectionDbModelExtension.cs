using System;
using System.IO;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using System.Text;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using TianHua.FanSelection.Function;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.FanSelection;

namespace ThMEPHAVC.CAD
{
    public static class ThFanSelectionDbModelExtension
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
            using (AcadDatabase blockDb = AcadDatabase.Open(BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
            }
        }

        public static void SetModelNumber(this ObjectId obj, string storey, int number)
        {
            obj.UpdateAttributesInBlock(new Dictionary<string, string>()
            {
                { ThFanSelectionCommon.BLOCK_ATTRIBUTE_STOREY_AND_NUMBER, ThFanSelectionUtils.StoreyNumber(storey, number.ToString()) }
            });
        }

        public static void SetModelIdentifier(this ObjectId obj, string identifier, int number, string style)
        {
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, identifier },
                { (int)DxfCode.ExtendedDataInteger32, number },
                { (int)DxfCode.ExtendedDataBinaryChunk,  Encoding.UTF8.GetBytes(style) },
            };
            obj.AddXData(ThFanSelectionCommon.RegAppName_FanSelection, valueList);
        }

        public static void UpdateModelNumber(this ObjectId obj, int number)
        {
            var oldValue = obj.GetModelNumber();
            if (oldValue > 0 && (oldValue != number))
            {
                obj.ModXData(
                    ThFanSelectionCommon.RegAppName_FanSelection,
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
                    ThFanSelectionCommon.RegAppName_FanSelection,
                    DxfCode.ExtendedDataAsciiString,
                    oldValue, identifier);
            }
        }

        public static string GetModelIdentifier(this ObjectId obj)
        {
            var valueList = obj.GetXData(ThFanSelectionCommon.RegAppName_FanSelection);
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

        public static string GetModelIdentifier(this DBObject dBObject)
        {
            var valueList = dBObject.GetXData(ThFanSelectionCommon.RegAppName_FanSelection);
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

        public static void SetModelXDataFrom(this ObjectId obj, ObjectId other)
        {
            var xdata = other.GetXData(ThFanSelectionCommon.RegAppName_FanSelection);
            obj.AddXData(ThFanSelectionCommon.RegAppName_FanSelection, xdata);
        }

        public static bool IsModel(this DBObject dBObject)
        {
            return !string.IsNullOrEmpty(dBObject.GetModelIdentifier());
        }

        public static bool IsModel(this ObjectId obj, string identifier)
        {
            var valueList = obj.GetXData(ThFanSelectionCommon.RegAppName_FanSelection);
            if (valueList == null)
            {
                return false;
            }

            var values = valueList.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
            {
                return false;
            }

            return (string)values.ElementAt(0).Value == identifier;
        }

        public static string GetModelStyle(this ObjectId obj)
        {
            var valueList = obj.GetXData(ThFanSelectionCommon.RegAppName_FanSelection);
            if (valueList == null)
            {
                return string.Empty;
            }

            var values = valueList.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataBinaryChunk).First();
            return Encoding.UTF8.GetString(values.Value as byte[]);
        }

        public static bool IsHTFCModel(this ObjectId obj)
        {
            var style = GetModelStyle(obj);
            return style.Contains(ThFanSelectionCommon.HTFC_TYPE_NAME);
        }

        public static int GetModelNumber(this ObjectId obj)
        {
            var valueList = obj.GetXData(ThFanSelectionCommon.RegAppName_FanSelection);
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

        private static string BlockDwgPath()
        {
            return Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.BLOCK_FAN_FILE);
        }
    }
}
