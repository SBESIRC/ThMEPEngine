using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    // Reference:
    //  https://www.keanw.com/2012/09/creating-a-selection-filter-that-finds-dynamic-blocks-in-autocad-using-net.html
    public static class ThDynamicBlockFilter
    {
        public static List<ObjectId> FilterBlocks(this Editor editor, List<string> blkNames)
        {
            var objIds = new List<ObjectId>();
            var anonBlks = GetDynamicBlockNames(blkNames);
            var filter = CreateFilterListForBlocks(anonBlks);
            var psr = editor.SelectAll(new SelectionFilter(filter));
            if (psr.Status == PromptStatus.OK)
            {
                objIds.AddRange(psr.Value.GetObjectIds());
            }
            return objIds;
        }

        public static List<string> GetDynamicBlockNames(List<string> blkNames)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var names = new List<string>();
                foreach(var blkName in blkNames)
                {
                    var btr = acadDatabase.Blocks.ElementOrDefault(blkName);
                    if (btr == null)
                    {
                        continue;
                    }

                    names.Add(btr.Name);
                    names.AddRange(btr.GetAnonymousBlockIds().OfType<ObjectId>()
                        .Select(o => acadDatabase.Blocks.Element(o).Name));
                }
                return names;
            }
        }

        private static TypedValue[] CreateFilterListForBlocks(List<string> blkNames)
        {
            // If we don't have any block names, return null
            if (blkNames.Count == 0)
                return null;

            // If we only have one, return an array of a single value
            if (blkNames.Count == 1)
                return new TypedValue[] { new TypedValue((int)DxfCode.BlockName, blkNames[0]) };

            // We have more than one block names to search for...
            // Create a list big enough for our block names plus
            // the containing "or" operators
            List<TypedValue> tvl = new List<TypedValue>(blkNames.Count + 2);

            // Add the initial operator
            tvl.Add(new TypedValue((int)DxfCode.Operator, "<or"));

            // Add an entry for each block name, prefixing the
            // anonymous block names with a reverse apostrophe
            foreach (var blkName in blkNames)
            {
                tvl.Add(new TypedValue((int)DxfCode.BlockName, (blkName.StartsWith("*") ? "`" + blkName : blkName)));
            }

            // Add the final operator
            tvl.Add(new TypedValue((int)DxfCode.Operator, "or>"));

            // Return an array from the list
            return tvl.ToArray();
        }
    }
}
