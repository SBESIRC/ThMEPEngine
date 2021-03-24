using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThReadStoreyInformationService
    {
        public string DeviceName { get; set; }
        public string RoofName { get; set; }
        public List<Tuple<string,string>> StandardSpaceNames { get; set; }
        public List<Tuple<string, string>> NonStandardSpaceNames { get; set; }
        public List<Tuple<string, string>> StoreyNames { get; set; }
        public ThReadStoreyInformationService()
        {
            RoofName = "";
            DeviceName = "";
            StoreyNames = new List<Tuple<string, string>>();
            StandardSpaceNames = new List<Tuple<string,string>>();
            NonStandardSpaceNames = new List<Tuple<string, string>>();
        }
        public void Read(ObjectIdCollection objIds)
        {
            if (objIds.Count == 0)
            {
                return;
            }

            //排序规则为小屋面，大屋面，接着从大到小排列
            DeviceName = GetDeviceName(objIds);
            RoofName = GetRoofName(objIds);
            StandardSpaceNames = GetSortList(DivideCharacter(GetStandardSpaceName(objIds), "标准层"));
            NonStandardSpaceNames = GetSortList(DivideCharacter(GetNonStandardSpaceName(objIds), "非标层"));
            if (DeviceName != "")
            {
                StoreyNames.Add(Tuple.Create(DeviceName, "小屋面"));
            }
            if (RoofName != "")
            {
                StoreyNames.Add(Tuple.Create(RoofName, "大屋面"));
            }
            foreach (var StandardSpaceName in StandardSpaceNames)
            {
                StoreyNames.Add(Tuple.Create($"{StandardSpaceName.Item1}{"标准层"}", StandardSpaceName.Item2));
            }
            foreach (var NonStandardSpaceName in NonStandardSpaceNames)
            {
                StoreyNames.Add(Tuple.Create($"{NonStandardSpaceName.Item1}{"非标层"}", NonStandardSpaceName.Item2));
            }
        }
        public void Read(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var engine = new ThWStoreysRecognitionEngine();
                engine.Recognize(acadDatabase.Database, new Point3dCollection());                           
                engine.Elements.Cast<ThWStoreys>().ForEach(o =>
                {
                    ThTagParametersService.blockCollection.Add(acadDatabase.Element<BlockReference>(o.ObjectId));
                });
                Read(engine.Elements.Cast<ThWStoreys>().Select(o => o.ObjectId).ToObjectIdCollection());
            }           
        }
        private static List<Tuple<string, string>> GetSortList(List<Tuple<string, string>> names)
        {
            for (int i=0;i< names.Count-1;i++)
            {
                for(int j=i;j< names.Count;j++)
                {
                    Tuple<string, string> value = Tuple.Create("","");
                    if (int.Parse(names[i].Item1)< int.Parse(names[j].Item1))
                    {
                        value = names[i];
                        names[i] = names[j];
                        names[j] = value;
                    }
                }
            }
            return names;
        }

        private static List<Tuple<string,string>> DivideCharacter(List<string> names,string floor)
        {
            var dividesNames = new List<Tuple<string, string>>();
            foreach (string name in names)
            {
                List<string> characters= ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToList();
                if (characters[0].Contains(','))
                {
                    List<string> characters1= ThStructureUtils.OriginalFromXref(name).ToUpper().Split(',').Reverse().ToList();
                    characters[0] = characters1[0];
                }
                dividesNames.Add(Tuple.Create( $"{characters[0]}",$"{ floor}{ name}"));         
            }
            return dividesNames;
        }
        public static string GetDeviceName(ObjectIdCollection blocks)
        {
            foreach (ObjectId objId in blocks)
            {
                if (BlockTools.GetDynBlockValue(objId, "楼层类型").Equals("小屋面"))
                {
                    return "小屋面";
                }
            }
            return string.Empty;
        }
        public static string GetRoofName(ObjectIdCollection blocks)
        {
            foreach (ObjectId objId in blocks)
            {
                if (BlockTools.GetDynBlockValue(objId, "楼层类型").Equals("大屋面"))
                {
                    return ("大屋面");
                }
            }
            return string.Empty;
        }
        public static List<string> GetStandardSpaceName(ObjectIdCollection blocks)
        {
            var blockString = new List<string>();
            foreach (ObjectId objId in blocks)
            {               
                if (BlockTools.GetDynBlockValue(objId, "楼层类型").Equals("标准层"))
                {
                    blockString.Add(BlockTools.GetAttributeInBlockReference(objId, "楼层编号"));
                }                       
            }

            return blockString;
        }
        public static List<string> GetNonStandardSpaceName(ObjectIdCollection blocks)
        {
            var blockString = new List<string>();
            foreach (ObjectId objId in blocks)
            {
                if (BlockTools.GetDynBlockValue(objId, "楼层类型").Equals("非标层"))
                {
                    blockString.Add(BlockTools.GetAttributeInBlockReference(objId, "楼层编号"));
                }              
            }
            return blockString;
        }
    }
}
