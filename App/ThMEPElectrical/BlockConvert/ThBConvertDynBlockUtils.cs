using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertDynBlockUtils
    {
        // Reference:
        //  https://adndevblog.typepad.com/autocad/2012/05/accessing-visible-entities-in-a-dynamic-block.html
        public static Dictionary<string, ObjectIdCollection> DynablockVisibilityStates(this ThBlockReferenceData blockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.HostDatabase))
            {
                var groups = new Dictionary<string, ObjectIdCollection>();
                var btr = acadDatabase.Blocks.ElementOrDefault(blockReference.EffectiveName);
                if (btr == null)
                {
                    return groups;
                }

                if (!btr.IsDynamicBlock)
                {
                    return groups;
                }

                if (btr.ExtensionDictionary.IsNull)
                {
                    return groups;
                }

                var dict = acadDatabase.Element<DBDictionary>(btr.ExtensionDictionary);
                if (!dict.Contains("ACAD_ENHANCEDBLOCK"))
                {
                    return groups;
                }

                ObjectId graphId = dict.GetAt("ACAD_ENHANCEDBLOCK");
                var parameterIds = graphId.acdbEntGetObjects((short)DxfCode.HardOwnershipId);
                foreach (object parameterId in parameterIds)
                {
                    ObjectId objId = (ObjectId)parameterId;
                    if (objId.ObjectClass.Name == "AcDbBlockVisibilityParameter")
                    {
                        var visibilityParam = objId.acdbEntGetTypedVals();
                        var enumerator = visibilityParam.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current.TypeCode == 303)
                            {
                                string group = (string)enumerator.Current.Value;
                                enumerator.MoveNext();
                                int nbEntitiesInGroup = (int)enumerator.Current.Value;
                                var entities = new ObjectIdCollection();
                                for (int i = 0; i < nbEntitiesInGroup; ++i)
                                {
                                    enumerator.MoveNext();
                                    entities.Add((ObjectId)enumerator.Current.Value);
                                }
                                groups.Add(group, entities);
                            }
                        }
                        break;
                    }
                }
                return groups;
            }
        }

        /// <summary>
        /// 提取动态块中当前可见性的值
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static string CurrentVisibilityStateValue(this ThBlockReferenceData blockReference)
        {
            var visibilityStates = DynablockVisibilityStates(blockReference);
            var properties = blockReference.CustomProperties
                .Cast<DynamicBlockReferenceProperty>()
                .Where(o => o.PropertyName == ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY);
            return properties.First().Value as string;
        }

        /// <summary>
        /// 提取动态块中当前可见性下可见的实体
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static ObjectIdCollection VisibleEntities(this ThBlockReferenceData blockReference)
        {
            var objs = new ObjectIdCollection();
            var visibilityStates = DynablockVisibilityStates(blockReference);
            var properties = blockReference.CustomProperties
                .Cast<DynamicBlockReferenceProperty>()
                .Where(o => o.PropertyName == ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY);
            foreach(var property in properties)
            {
                visibilityStates.Where(o => o.Key == property.Value as string)
                    .ForEach(o => objs.Add(o.Value));
            }
            return objs;
        }

        /// <summary>
        /// 提取动态块中当前可见性下可见的文字的内容
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static List<string> VisibleTexts(this ThBlockReferenceData blockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.HostDatabase))
            {
                var texts = new List<string>();
                foreach (ObjectId obj in blockReference.VisibleEntities())
                {
                    var entity = acadDatabase.Element<Entity>(obj);
                    if (entity is DBText dBText)
                    {
                        texts.Add(dBText.TextString);
                    }
                }
                return texts;
            }
        }
    }
}
