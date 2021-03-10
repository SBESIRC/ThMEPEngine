﻿using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using ThMEPWSS.Pipe.Geom;
using ThMEPWSS.Pipe.Tools;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using ThCADExtension;

namespace ThMEPWSS.Pipe.Service
{
    public class ThReadStoreyInformationService
    {
        public string DeviceName { get; set; }
        public string RoofName { get; set; }
        public List<Tuple<string,string>> StandardSpaceNames { get; set; }
        public List<Tuple<string, string>> NonStandardSpaceNames { get; set; }
        public List<Tuple<string, string>> StoreyNames { get; set; }
        public static List<BlockReference> Ablocks { get; set; }
        public ThReadStoreyInformationService()
        {
            DeviceName = string.Empty;
            RoofName = string.Empty;
            StandardSpaceNames = new List<Tuple<string,string>>();
            NonStandardSpaceNames = new List<Tuple<string, string>>();
            StoreyNames = new List<Tuple<string, string>>();
            Ablocks = new List<BlockReference>();
        }
        public void Read(Database database)
        {
            //排序规则为小屋面，大屋面，接着从大到小排列
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (!(database.GetEntsInDatabase<BlockReference>().Count > 0))
                {
                    return;
                }
                var blockLists = database.GetEntsInDatabase<BlockReference>();
                int blockNum = 0;
                foreach(var block in blockLists)
                {
                    if(block.GetEffectiveName()== "楼层框定")
                    {
                        blockNum++;
                        break;
                    }
                    
                }
                if(blockNum==0)
                {
                    return;
                }
                var blockCollection1 = new List<BlockReference>();
                blockCollection1 = BlockTools.GetAllDynBlockReferences(database, "楼层框定");
                var blockCollection = new List<BlockReference>();
                var blockboundary = new List<Polyline>();
                foreach (BlockReference block in blockCollection1)
                {
                    blockboundary.Add(ThWPipeOutputFunction.GetBlockBoundary(block));
                }
                for(int i=0;i< blockCollection1.Count;i++)
                {
                    int n = 0;
                    Polyline polyline = ThWPipeOutputFunction.GetBlockBoundary(blockCollection1[i]);
                    for (int j= 0;j < blockboundary.Count;j++)
                    {
                        if (j != i)
                        {
                            int num = 0;
                            foreach (Point3d pt in polyline.Vertices())
                            {
                                if (GeomUtils.PtInLoop(blockboundary[j], pt))
                                {
                                    num++;
                                }
                            }
                            if (num==5)
                            {
                                n++;
                                break;
                            }
                        }
                    }
                    if (n==0)
                    {
                        blockCollection.Add(blockCollection1[i]);
                    }
                }
                if (blockCollection.Count > 0)
                {
                    DeviceName = GetDeviceName(blockCollection);
                    RoofName = GetRoofName(blockCollection);
                    StandardSpaceNames = GetSortList(DivideCharacter(GetStandardSpaceName(blockCollection), "标准层"));
                    NonStandardSpaceNames = GetSortList(DivideCharacter(GetNonStandardSpaceName(blockCollection), "非标层"));
                }
                StoreyNames.Add(Tuple.Create(DeviceName, "小屋面"));
                StoreyNames.Add(Tuple.Create(RoofName, "大屋面"));
                foreach(var StandardSpaceName in StandardSpaceNames)
                {
                    StoreyNames.Add(Tuple.Create($"{StandardSpaceName.Item1}{"标准层"}", StandardSpaceName.Item2));
                }
                foreach (var NonStandardSpaceName in NonStandardSpaceNames)
                {
                    StoreyNames.Add(Tuple.Create($"{NonStandardSpaceName.Item1}{"非标层"}", NonStandardSpaceName.Item2));
                }
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
        public static string GetDeviceName(List<BlockReference> blocks)
        {
            var blockBounds = new List<BlockReference>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("小屋面"))
                {
                    Ablocks.Add(block);
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
                    Ablocks.Add(block);
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
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Equals("标准层"))
                {
                    blockString.Add(BlockTools.GetAttributeInBlockReference(block.Id, "楼层编号"));
                }                       
            }

            return blockString;
        }
        public static List<string> GetNonStandardSpaceName(List<BlockReference> blocks)
        {
            var blockString = new List<string>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Equals("非标层"))
                {
                    blockString.Add(BlockTools.GetAttributeInBlockReference(block.Id, "楼层编号"));
                }              
            }
            return blockString;
        }
    }
}
