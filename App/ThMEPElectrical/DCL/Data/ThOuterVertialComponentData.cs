using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.DCL.Data
{
    public class ThOuterVertialComponentData
    {
        public DBObjectCollection Columns { get; set; }
        public DBObjectCollection Shearwalls { get; set; }
        public List<Entity> OuterOutlines { get; set; }
        public List<Entity> InnerOutlines { get; set; }

        public ThOuterVertialComponentData()
        {
            Columns = new DBObjectCollection();
            Shearwalls = new DBObjectCollection();
            OuterOutlines = new List<Entity>();
            InnerOutlines = new List<Entity>();
        }
        public ThOuterVertialComponentData(ModelData modelData) :this()
        {
            Columns = modelData._columns;
            Shearwalls = modelData._shearWalls;
        }
    }
    public class ThArchOuterVertialComponentData : ThOuterVertialComponentData
    {

        //通过建筑平面数据寻找外圈竖向构件
        //输入数据：建筑外轮廓线、内庭院洞线、竖向构件(柱、剪力墙)
        public ThArchOuterVertialComponentData(ModelData modelData):base(modelData)
        {           
        }
    }
    public class ThStruOuterVertialComponentData : ThOuterVertialComponentData
    {
        //通过结构平面数据寻找外圈竖向构件
        //输入数据：主梁、悬挑主梁、剪力墙、柱
        public List<ThBeamLink> PrimaryBeams { get; set; }
        public List<ThBeamLink> OverhangingPrimaryBeams { get; set; }
        public ThStruOuterVertialComponentData()
        {
            PrimaryBeams = new List<ThBeamLink>();
            OverhangingPrimaryBeams = new List<ThBeamLink>();
        }
        public ThStruOuterVertialComponentData(ModelData modelData):base(modelData)
        {
            //PrimaryBeams = new List<ThBeamLink>();
            //OverhangingPrimaryBeams = new List<ThBeamLink>();
            //PrimaryBeams = extractor.ArchitectureOutlineData.BeamEngine.PrimaryBeamLinks;
            //OverhangingPrimaryBeams = extractor.ArchitectureOutlineData.BeamEngine.OverhangingPrimaryBeamLinks;
            PrimaryBeams = new List<ThBeamLink>();
            OverhangingPrimaryBeams = new List<ThBeamLink>();
            if (modelData is Model2Data data)
            {
                PrimaryBeams = data.BeamEngine.PrimaryBeamLinks;
                OverhangingPrimaryBeams = data.BeamEngine.OverhangingPrimaryBeamLinks;
            }
        }
    }
}
