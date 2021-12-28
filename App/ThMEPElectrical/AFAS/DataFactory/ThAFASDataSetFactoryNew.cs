using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.Interface;

namespace ThMEPElectrical.AFAS.Data
{
    public class ThAFASDataSetFactoryNew
    {
        public ThAFASDataSetFactoryNew()
        { }
        private ThMEPOriginTransformer Transformer { get; set; }
        public List<ThExtractorBase> Extractors { get; set; }
        public void SetTransformer(ThMEPOriginTransformer transformer)
        {
            Transformer = transformer;
        }
        public void GetElements(Database database, Point3dCollection collection)
        {
            // ArchitectureWall、Shearwall、Column、Room、Hole
           
            var vm = Extract(database); // visitor manager,提取的是原始数据
            vm.MoveToOrigin(Transformer); // 移动到原点

            //先提取楼层框线
            var storeyExtractor = new ThAFASEStoreyExtractor()
            {
                // ElementLayer = "AI-楼层框定E",
                ElementLayer = "AD-FLOOR-AREA",
                Transformer = Transformer,
            };
            storeyExtractor.Extract(database, collection);
            storeyExtractor.Transform(); //移到原点

            //再提取防火分区，接着用楼层框线对防火分区分组
            var storeyInfos = storeyExtractor.Storeys.Cast<ThStoreyInfo>().ToList();
            var fireApartExtractor = new ThAFASFireCompartmentExtractor()
            {
                ElementLayer = "AI-防火分区,AD-AREA-DIVD",
                StoreyInfos = storeyInfos, //用于创建防火分区
                Transformer = Transformer, //把变换器传给防火分区
            };
            fireApartExtractor.Extract(database, collection);
            fireApartExtractor.Group(storeyExtractor.StoreyIds); //判断防火分区属于哪个楼层框线
            fireApartExtractor.BuildFireAPartIds(); //创建防火分区编号

            Extractors = new List<ThExtractorBase>()
                {
                    new ThAFASArchitectureWallExtractor()
                    {
                        ElementLayer = "AI-墙",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ArchWallVisitor.Results,
                    },
                    new ThAFASShearWallExtractor()
                    {
                        ElementLayer = "AI-剪力墙",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ShearWallVisitor.Results,
                        NonDb3ExtractResults = vm.ShearWallVisitor.Results,
                    },
                    new ThAFASColumnExtractor()
                    {
                        ElementLayer = "AI-柱",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ColumnVisitor.Results,
                        NonDb3ExtractResults = vm.ColumnVisitor.Results,
                    },
                    new ThAFASWindowExtractor()
                    {
                        ElementLayer="AI-窗",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3WindowVisitor.Results,
                    },
                    new ThAFASRoomExtractor()
                    {
                        UseDb3Engine=true,
                        Transformer = Transformer,
                    },
                    new ThAFASBeamExtractor()
                    {
                        ElementLayer = "AI-梁",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3BeamVisitor.Results,
                    },
                    new ThAFASDoorOpeningExtractor()
                    {
                        ElementLayer = "AI-门",
                        Transformer = Transformer,
                        VisitorManager = vm,
                    },
                    new ThAFASRailingExtractor()
                    {
                        ElementLayer = "AI-栏杆",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3RailingVisitor.Results,
                    },
                    new ThAFASFireProofShutterExtractor()
                    {
                        ElementLayer = "AI-防火卷帘",
                        Transformer = Transformer,
                    },
                    new ThAFASHoleExtractor()
                    {
                        ElementLayer = "AI-洞",
                        Transformer = Transformer,
                    },
            };

            Extractors.ForEach(o => o.Extract(database, collection));

            Extractors.Add(storeyExtractor);
            Extractors.Add(fireApartExtractor);

            //////移回原位，保留，之后不是所有的extractor会被选。必须回原位
            Extractors.ForEach(o =>
            {
                if (o is ITransformer iTransformer)
                {
                    iTransformer.Reset();
                }
            });
        }

       

        private ThBuildingElementVisitorManager Extract(Database database)
        {
            var visitors = new ThBuildingElementVisitorManager(database);
            var extractor = new ThBuildingElementExtractorEx();
            extractor.Accept(visitors.DB3ArchWallVisitor);
            extractor.Accept(visitors.DB3ShearWallVisitor);
            extractor.Accept(visitors.DB3ColumnVisitor);
            extractor.Accept(visitors.DB3WindowVisitor);
            extractor.Accept(visitors.DB3BeamVisitor);
            extractor.Accept(visitors.DB3RailingVisitor);
            extractor.Accept(visitors.ColumnVisitor);
            extractor.Accept(visitors.ShearWallVisitor);
            extractor.Accept(visitors.DB3CurtainWallVisitor);
            extractor.Accept(visitors.DB3DoorMarkVisitor);
            extractor.Accept(visitors.DB3DoorStoneVisitor);
            extractor.Extract(database);
            return visitors;
        }
    }
}
