using DotNetARX;
using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using System;

namespace ThMEPWSS.Pipe.Service
{
    public class ThReadStoreyInformationService
    {
        public string DeviceName { get; set; }
        public string RoofName { get; set; }
        public List<Tuple<string,string>> StandardSpaceNames { get; set; }
        public List<Tuple<string, string>> NonStandardSpaceNames { get; set; }
        public ThReadStoreyInformationService()
        {
            DeviceName = string.Empty;
            RoofName = string.Empty;
            StandardSpaceNames = new List<Tuple<string,string>>();
            NonStandardSpaceNames = new List<Tuple<string, string>>();
        }
        public void Read(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var blockCollection = new List<BlockReference>();
                blockCollection = BlockTools.GetAllDynBlockReferences(database, "楼层框定");
                if (blockCollection.Count > 0)
                {
                    DeviceName = GetDeviceName(blockCollection);
                    RoofName = GetRoofName(blockCollection);
                    StandardSpaceNames = DivideCharacter(GetStandardSpaceName(blockCollection), "标准层");
                    NonStandardSpaceNames = DivideCharacter(GetNonStandardSpaceName(blockCollection), "非标层");
                }
            }
        }
        private static List<Tuple<string,string>> DivideCharacter(List<string> names,string floor)
        {
            var dividesNames = new List<Tuple<string, string>>();
            foreach (string name in names)
            {
                List<string> characters= ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').ToList();
                foreach(string character in characters)
                {
                    ThStructureUtils.OriginalFromXref(character).ToUpper().Split(',').ToList().ForEach(o => dividesNames.Add(Tuple.Create(o,$"{floor}{name}")));
                }
            }
            return dividesNames;
        }
        public static string GetDeviceName(List<BlockReference> blocks)
        {
            var blockBounds = new List<BlockReference>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("小屋面"))
                {
                    return ("小屋面");
                }
            }
            return string.Empty;
        }
        public static string GetRoofName(List<BlockReference> blocks)
        {
            var blockBounds = new List<BlockReference>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("大屋面"))
                {
                    return ("大屋面");
                }
            }
            return string.Empty;
        }
        public static List<string> GetStandardSpaceName(List<BlockReference> blocks)
        {
            var blockString = new List<string>();
            foreach (BlockReference block in blocks)
            {
                var blockBounds = new List<BlockReference>();              
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("标准层"))
                {
                    blockBounds.Add(block);
                }
                blockString.Add(BlockTools.GetAttributeInBlockReference(block.Id, "楼层编号"));           
            }

            return blockString;
        }
        public static List<string> GetNonStandardSpaceName(List<BlockReference> blocks)
        {
            var blockString = new List<string>();
            var blockBounds = new List<BlockReference>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("非标层"))
                {
                    blockBounds.Add(block);
                }
                blockString.Add(BlockTools.GetAttributeInBlockReference(block.Id, "楼层编号"));
            }
            return blockString;
        }
    }
}
