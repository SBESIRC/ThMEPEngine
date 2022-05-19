using NFox.Cad;
using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThDynamicBlockUtils
    {
        // Reference:
        //  https://adndevblog.typepad.com/autocad/2012/05/accessing-visible-entities-in-a-dynamic-block.html
        public static Dictionary<string, ObjectIdCollection> DynablockVisibilityStates(this ThBlockReferenceData blockReference)
        {
            return DynablockVisibilityStates(blockReference.Database, blockReference.EffectiveName);
        }
        private static Dictionary<string, ObjectIdCollection> DynablockVisibilityStates(Database database, string blockName)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var groups = new Dictionary<string, ObjectIdCollection>();
                var btr = acadDatabase.Blocks.ElementOrDefault(blockName);
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
        /// 提取动态块中当前可见性下可见的实体
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static ObjectIdCollection VisibleEntities(this ThBlockReferenceData blockReference)
        {
            var visibility = blockReference.CurrentVisibilityStateValue();
            if (!string.IsNullOrEmpty(visibility))
            {
                return VisibleEntities(blockReference.Database, blockReference.EffectiveName, visibility);
            }
            return new ObjectIdCollection();
        }

        /// <summary>
        /// 提取动态块中指定可见性下可见的实体
        /// </summary>
        /// <param name="database"></param>
        /// <param name="blockName"></param>
        /// <param name="visibility"></param>
        /// <returns></returns>
        public static ObjectIdCollection VisibleEntities(Database database, string blockName, string visibility)
        {
            var visibilityStates = DynablockVisibilityStates(database, blockName);
            if (visibilityStates.ContainsKey(visibility))
            {
                return visibilityStates[visibility];
            }
            return new ObjectIdCollection();
        }

        /// <summary>
        /// 提取动态块中当前可见性的值
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static string CurrentVisibilityStateValue(this ThBlockReferenceData blockReference)
        {
            if (null == blockReference.CustomProperties)
                return "";
            var properties = blockReference.CustomProperties
                .Cast<DynamicBlockReferenceProperty>()
                .Where(o => o.IsVisibility());
            return properties.Any() ? properties.First().Value as string : "";
        }

        // Reference:
        // https://forums.autodesk.com/t5/net/explode-dynamic-block-with-stretch-action/td-p/4673471
        // https://forums.autodesk.com/t5/net/explode-dynamic-block-with-visibility-states/td-p/3643036
        public static void ExplodeWithVisible(this BlockReference blockReference, DBObjectCollection entitySet)
        {
            entitySet.Clear();
            var objs = new DBObjectCollection();
            blockReference.Explode(objs);
            objs.Cast<Entity>()
                .Where(e => e.Visible)
                .Where(e => e.Bounds.HasValue)
                .ForEachDbObject(o => entitySet.Add(o));
        }

        /// <summary>
        /// 是否是可见性属性
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool IsVisibility(this DynamicBlockReferenceProperty property)
        {
            // “可见性”的属性名称是可以改变的，
            // 没有好的办法去判断某个动态属性是“可见性”
            // 暂时用白名单的方式罗列出“可见性”属性可能的名称
            var names = new string[]
            {
                "可见性",
                "可见性1",
            };
            return names.Contains(property.PropertyName);
        }
    }
}
