using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
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
            return DynablockVisibilityStates(blockReference.HostDatabase, blockReference.EffectiveName);
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
            return VisibleEntities(blockReference.HostDatabase, blockReference.EffectiveName, visibility);
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
            var visibilityStates = blockReference.DynablockVisibilityStates();
            var properties = blockReference.CustomProperties
                .Cast<DynamicBlockReferenceProperty>()
                .Where(o => o.PropertyName == "可见性" || o.PropertyName == "可见性1");
            return properties.First().Value as string;
        }

        // Reference:
        // https://forums.autodesk.com/t5/net/explode-dynamic-block-with-stretch-action/td-p/4673471
        // https://forums.autodesk.com/t5/net/explode-dynamic-block-with-visibility-states/td-p/3643036
        public static void ExplodeWithVisible(this BlockReference blockReference, DBObjectCollection entitySet)
        {
            entitySet.Clear();
            if (blockReference.IsDynamicBlock)
            {
                var objs = new DBObjectCollection();
                blockReference.Explode(objs);
                objs.Cast<Entity>().Where(e => e.Visible).ForEach(e => entitySet.Add(e));
            }
            else
            {
                blockReference.Explode(entitySet);
            }
        }
    }
}
