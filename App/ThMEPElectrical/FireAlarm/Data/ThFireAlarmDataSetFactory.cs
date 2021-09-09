using System;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarm.Interface;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace FireAlarm.Data
{
    public class ThFireAlarmDataSetFactory : ThMEPDataSetFactory
    {
        private List<ThGeometry> Geos { get; set; }
        public ThFireAlarmDataSetFactory()
        {
            Geos = new List<ThGeometry>();
        }
        protected override void GetElements(Database database, Point3dCollection collection)
        {
            // ArchitectureWall、Shearwall、Column、Window、Room
            // Beam、DoorOpening、Railing、FireproofShutter(防火卷帘)
            UpdateTransformer(collection);
            var vm = Extract(database); // visitor manager,提取的是原始数据
            vm.MoveToOrigin(Transformer); // 移动到原点

            //先提取楼层框线
            var storeyExtractor = new ThFaEStoreyExtractor()
            {
                ElementLayer = "AI-楼层框定E",
                Transformer = Transformer,
            };
            storeyExtractor.Extract(database, collection);
            storeyExtractor.Transform(); //移到原点

            //再提取防火分区，接着用楼层框线对防火分区分组
            var storeyInfos = storeyExtractor.Storeys.Cast<ThStoreyInfo>().ToList();
            var fireApartExtractor = new ThFireApartExtractor()
            {
                ElementLayer = "AI-防火分区,AD-AREA-DIVD",
                StoreyInfos = storeyInfos, //用于创建防火分区
                Transformer = Transformer, //把变换器传给防火分区
            };
            fireApartExtractor.Extract(database, collection);
            fireApartExtractor.Group(storeyExtractor.StoreyIds); //判断防火分区属于哪个楼层框线
            fireApartExtractor.BuildFireAPartIds(); //创建防火分区编号

            var extractors = new List<ThExtractorBase>()
                {
                    new ThFaArchitectureWallExtractor()
                    {
                        ElementLayer = "AI-墙",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ArchWallVisitor.Results,
                    },
                    new ThFaShearWallExtractor()
                    {
                        ElementLayer = "AI-剪力墙",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ShearWallVisitor.Results,
                        NonDb3ExtractResults = vm.ShearWallVisitor.Results,
                    },
                    new ThFaColumnExtractor()
                    {
                        ElementLayer = "AI-柱",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ColumnVisitor.Results,
                        NonDb3ExtractResults = vm.ColumnVisitor.Results,
                    },
                    new ThFaWindowExtractor()
                    {
                        ElementLayer="AI-窗",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3WindowVisitor.Results,
                    },
                    new ThFaRoomExtractor()
                    {
                        UseDb3Engine=true,
                        Transformer = Transformer,
                    },
                    new ThFaBeamExtractor()
                    {
                        ElementLayer = "AI-梁",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3BeamVisitor.Results,
                    },
                    new ThFaDoorOpeningExtractor()
                    {
                        ElementLayer = "AI-门",
                        Transformer = Transformer,
                        VisitorManager = vm,
                    },
                    new ThFaRailingExtractor()
                    {
                        ElementLayer = "AI-栏杆",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3RailingVisitor.Results,
                    },
                    new ThFaFireproofshutterExtractor()
                    {
                        ElementLayer = "AI-防火卷帘",
                        Transformer = Transformer,
                    },
                    new ThHoleExtractor()
                    {
                        ElementLayer = "AI-洞",
                        Transformer = Transformer,
                    },
                     new ThFireAlarmBlkExtractor ()
                    {
                        Transformer = Transformer ,
                        BlkNameList = ThMEPElectrical.FireAlarm.ThFixLayoutCommon.BlkNameList, //add needed all blk name string 
                    }
                };
            extractors.ForEach(o => o.Extract(database, collection));

            //把楼层信息传入到提取器中，对于不在防火分区内的图形要判断在哪个楼层
            extractors.ForEach(o =>
            {
                if (o is ISetStorey iStorey)
                {
                    iStorey.Set(storeyInfos);
                }
            });

            //将房间外扩的区域得到的差集作为墙传入到建筑墙中
            var selfBuildWalls = BuildWalls(extractors);
            var architectureWallExtractor = extractors.Where(
                o => o is ThFaArchitectureWallExtractor).First()
                as ThFaArchitectureWallExtractor;
            architectureWallExtractor.Walls.AddRange(selfBuildWalls);

            //用防火分区对墙、柱...分组
            extractors.ForEach(o =>
            {
                if (o is IGroup group)
                {
                    group.Group(fireApartExtractor.FireApartIds);
                }
            });

            //找到防火门、防火卷帘邻接的防火分区
            var faDoorExtractor = extractors.Where(o => o is ThFaDoorOpeningExtractor).First() as ThFaDoorOpeningExtractor;
            faDoorExtractor.SetTags(fireApartExtractor.FireApartIds);
            var fireProofShutter = extractors.Where(o => o is ThFaFireproofshutterExtractor).First() as ThFaFireproofshutterExtractor;
            fireProofShutter.SetTags(fireApartExtractor.FireApartIds);
            // 把房间传给门提取器
            var roomExtractor = extractors.Where(o => o is ThFaRoomExtractor).First() as ThFaRoomExtractor;
            faDoorExtractor.SetRooms(roomExtractor.Rooms);

            //把洞传给门提取器
            var holeExtractor = extractors.Where(o => o is ThHoleExtractor).First() as ThHoleExtractor;
            faDoorExtractor.SetHoles(holeExtractor.HoleDic.Keys.ToList());

            //最后将楼层框线和防火分区提取器加入，生成Geometries
            extractors.Add(storeyExtractor);
            extractors.Add(fireApartExtractor);
            /*Print(database, extractors);*/
            //收集数据
            extractors.ForEach(o => Geos.AddRange(o.BuildGeometries()));
            // 移回原位
            storeyExtractor.Reset();
            fireApartExtractor.Reset();
            extractors.ForEach(o =>
            {
                if (o is ITransformer iTransformer)
                {
                    iTransformer.Reset();
                }
            });
            MoveToXYPlane(Geos);
        }

        public void MoveToXYPlane(List<ThGeometry> geos)
        {
            geos.ForEach(g =>
            {
                if (g.Boundary != null)
                {
                    if (g.Boundary is Polyline polyline)
                    {
                        var vec = new Vector3d(0, 0, -polyline.GetPoint3dAt(0).Z);
                        var mt = Matrix3d.Displacement(vec);
                        g.Boundary.TransformBy(mt);
                    }
                    else if (g.Boundary is MPolygon mPolygon)
                    {
                        var vec = new Vector3d(0, 0, -1.0 * mPolygon.Shell().GetPoint3dAt(0).Z);
                        var mt = Matrix3d.Displacement(vec);
                        g.Boundary.TransformBy(mt);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
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

        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = Geos,
            };
        }

        private void Print(Database database, List<ThExtractorBase> extractors)
        {
            short colorIndex = 1;
            extractors.ForEach(o =>
            {
                o.ColorIndex = colorIndex++;
                if (o is IPrint printer)
                {
                    printer.Print(database);
                }
            });
        }

        private List<Entity> BuildWalls(List<ThExtractorBase> extractors)
        {
            var roomExtractor = extractors.Where(o => o is ThFaRoomExtractor).First() as ThFaRoomExtractor;
            var handleBufferService = new ThHandleRoomBufferService(roomExtractor.GetEntities());
            extractors.ForEach(o =>
            {
                if (o is ThFaArchitectureWallExtractor ||
                o is ThFaShearWallExtractor ||
                o is ThFaColumnExtractor ||
                o is ThFaWindowExtractor ||
                o is ThFaDoorOpeningExtractor ||
                o is ThFaBeamExtractor ||
                o is ThFaRailingExtractor ||
                o is ThFaFireproofshutterExtractor)
                {
                    handleBufferService.Add(o.GetEntities());
                }
            });
            handleBufferService.Handle();
            return handleBufferService.Walls;
        }
    }
}
