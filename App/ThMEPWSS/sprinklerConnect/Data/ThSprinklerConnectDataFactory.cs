using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using DotNetARX;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;


using ThMEPEngineCore.Data;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Engine;

using ThMEPWSS.Sprinkler.Data;
using ThMEPWSS.Sprinkler.Service;

namespace ThMEPWSS.SprinklerConnect.Data
{


    class ThSprinklerConnectDataFactory : ThMEPDataSetFactory
    {
        private List<ThGeometry> Geos { get; set; }
        public ThSprinklerConnectDataFactory()
        {
            Geos = new List<ThGeometry>();
        }

        /// <summary>
        /// 获取建筑元素
        /// </summary>
        protected override void GetElements(Database database, Point3dCollection collection)
        {
            UpdateTransformer(collection);
            var manger = Extract(database); // visitor manager,提取的是原始数据
            manger.MoveToOrigin(Transformer); // 移动到原点

            var extractors = new List<ThExtractorBase>()
            {
                new ThSprinklerArchitectureWallExtractor()
                {
                    ElementLayer = "AI-墙",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3ArchWallVisitor.Results,
                 },
                new ThSprinklerShearWallExtractor()
                {
                    ElementLayer = "AI-剪力墙",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3ShearWallVisitor.Results,
                    NonDb3ExtractResults = manger.ShearWallVisitor.Results,
                },
                new ThSprinklerColumnExtractor()
                {
                    ElementLayer = "AI-柱",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3ColumnVisitor.Results,
                    NonDb3ExtractResults = manger.ColumnVisitor.Results,
                },
                new ThSprinklerHoleExtractor()
                {
                    ElementLayer = "AI-洞",
                    Transformer = Transformer,
                },
                new ThSprinklerDoorOpeningExtractor()
                {
                    ElementLayer = "AI-门",
                    Transformer = Transformer,
                    VisitorManager = manger,
                },
                new ThSprinklerFireproofshutterExtractor()
                {
                    ElementLayer = "AI-防火卷帘",
                    Transformer = Transformer,
                },
                new ThSprinklerBeamExtractor()
                {
                    ElementLayer = "AI-梁",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3BeamVisitor.Results,
                },
                new ThSprinklerWindowExtractor()
                    {
                        ElementLayer="AI-窗",
                        Transformer = Transformer,
                        Db3ExtractResults = manger.DB3WindowVisitor.Results,
                    },
                new ThSprinklerRoomExtractor()
                {
                    IsWithHole=false,
                    UseDb3Engine=true,
                    Transformer = Transformer,
                },
            };
            extractors.ForEach(o => o.Extract(database, collection));
            //收集数据
            extractors.ForEach(o => Geos.AddRange(o.BuildGeometries()));
            // 移回原位
            extractors.ForEach(o =>
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
            extractor.Accept(visitors.DB3BeamVisitor);
            extractor.Accept(visitors.DB3DoorMarkVisitor);
            extractor.Accept(visitors.DB3DoorStoneVisitor);
            extractor.Accept(visitors.DB3WindowVisitor);
            extractor.Accept(visitors.ColumnVisitor);
            extractor.Accept(visitors.ShearWallVisitor);
            extractor.Extract(database);
            return visitors;
        }

        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = Geos,
            };
        }

        public static List<Point3d> getSprinklerConnectData(Polyline frame)
        {
            var sprinklerPt = new List<Point3d>();

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var recognizeAllEngine = new ThTCHSprinklerRecognitionEngine();
                recognizeAllEngine.RecognizeMS(acadDatabase.Database, new Point3dCollection());



                var sprinklersData = recognizeAllEngine.Elements 
                                      .OfType<ThSprinkler>()
                                      .Where(o => frame.Contains(o.Position))
                                      .Select(o => o.Position)
                                      .ToList();
                sprinklerPt.AddRange(sprinklersData);
            }

            return sprinklerPt;

        }

        private static DBObjectCollection getSprayItems(Polyline frame)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThWSSCommon.SprayLayerName);
                acadDatabase.Database.UnLockLayer(ThWSSCommon.SprayLayerName);
                acadDatabase.Database.UnOffLayer(ThWSSCommon.SprayLayerName);

                //获取喷淋
                var dxfNames = new string[]
                {
                    ThCADCommon.DxfName_TCH_EQUIPMENT_16,
                    ThCADCommon.DxfName_TCH_EQUIPMENT_12,
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };

                var filterlist = OpFilter.Bulid(o =>
                                o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.SprayLayerName &
                                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var sprays = new List<Entity>();
                var allSprays = Active.Editor.SelectAll(filterlist);
                if (allSprays.Status == PromptStatus.OK)
                {
                    using (AcadDatabase acdb = AcadDatabase.Active())
                    {
                        foreach (ObjectId obj in allSprays.Value.GetObjectIds())
                        {
                            sprays.Add(acdb.Element<Entity>(obj));
                        }
                    }
                }

                var objs = new DBObjectCollection();
                sprays.Where(o =>
                {
                    var pts = o.GeometricExtents;
                    var position = new Point3d((pts.MinPoint.X + pts.MaxPoint.X) / 2, (pts.MinPoint.Y + pts.MaxPoint.Y) / 2, 0);
                    return frame.Contains(position);
                })
                .ForEachDbObject(o => objs.Add(o));

                return objs;
            }
        }
    }
}






