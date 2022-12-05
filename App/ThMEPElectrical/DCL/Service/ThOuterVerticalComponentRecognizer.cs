using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPElectrical.DCL.Data;
using ThMEPEngineCore.Interface;

namespace ThMEPElectrical.DCL.Service
{
    /// <summary>
    /// 外圈竖向构件识别
    /// </summary>
    public abstract class ThOuterVerticalComponentRecognizer
    {
        public Dictionary<MPolygon, HashSet<DBObject>> OuterColumnsMap { get; protected set; }
        public Dictionary<MPolygon, HashSet<DBObject>> OuterShearWallsMap { get; protected set; }
        public HashSet<DBObject> OtherColumns { get; protected set; }
        public HashSet<DBObject> OtherShearWalls { get; protected set; }
        protected double ArchOutlineOffsetLength;
        public ThOuterVerticalComponentRecognizer()
        {
            OtherColumns = new HashSet<DBObject>();
            OtherShearWalls = new HashSet<DBObject>();
            OuterColumnsMap = new Dictionary<MPolygon, HashSet<DBObject>>();
            OuterShearWallsMap = new Dictionary<MPolygon, HashSet<DBObject>>();
        }
        public abstract void Recognize();
    }
    public class ThStruOuterVerticalComponentRecognizer : ThOuterVerticalComponentRecognizer
    {
        private ThStruOuterVertialComponentData _inputData { get; set; }
        public ThStruOuterVerticalComponentRecognizer(ThOuterVertialComponentData inputData)
        {
            if (inputData is ThStruOuterVertialComponentData data)
                _inputData = data;
            ArchOutlineOffsetLength = 500.0;
        }
        public override void Recognize()
        {
            //实现方法
            //外轮廓线内缩500，生成一个内缩框线，外轮廓线和内缩框线形成一个回形区域
            //a、对于直接处于内缩框线外的竖向构件，则直接判定为外圈竖向构件
            //b、对于回形区域内能选中的竖向构件，直接判定为外圈竖向构件
            //c、对于回形区域能选中主梁，其向内缩框线内的一侧连接的竖向构件判定为外圈竖向构件
            //d、对于c有一个例外，即主梁远离内缩框线的一侧连接的竖向构件如果已经判定为外圈竖向构件，
            //且不能存在满足a/b/c的情况,则不应将其向内缩框线内的一侧连接的竖向构件判定为外圈构件，意思是a/b/c的优先级要大于d

            //为了创建悬臂梁的索引，先构建悬臂梁的轮廓与悬臂梁本身的字典
            //创建悬臂梁所有的轮廓线的DBObjectionCollection，以便创建索引
            if (_inputData == null)
            {
                return;
            }
            var beamLinkMap = new Dictionary<DBObject, ThBeamLink>();
            _inputData.PrimaryBeams.ForEach(o => o.Beams.ForEach(p => beamLinkMap.Add(p.Outline, o)));
            _inputData.OverhangingPrimaryBeams.ForEach(o => o.Beams.ForEach(p => beamLinkMap.Add(p.Outline, o)));

            //创建结构轮廓线和建筑轮廓线的字典
            //构建索引
            var columnSpatialIndex = new ThCADCoreNTSSpatialIndex(_inputData.Columns);
            var shearWallSpatialIndex = new ThCADCoreNTSSpatialIndex(_inputData.Shearwalls);
            var beamSpatialIndex = new ThCADCoreNTSSpatialIndex(beamLinkMap.Keys.ToCollection());

            var outerColumnsMap = new Dictionary<MPolygon, HashSet<DBObject>>();
            var outerShearWallsMap = new Dictionary<MPolygon, HashSet<DBObject>>();
            IBuffer bufferService = new ThNTSBufferService();
            _inputData.ArchOutlineAreas.OrderByDescending(o=>o.Area).ForEach(o =>
            {
                var innerArea = bufferService.Buffer(o, -1.0 * ArchOutlineOffsetLength); //内缩
                if (innerArea != null && innerArea is MPolygon polygon)
                {
                    // 拿到回形区域内的元素
                    var innerColumns = columnSpatialIndex.SelectWindowPolygon(innerArea).OfType<DBObject>().ToHashSet();
                    var innerShearWalls = shearWallSpatialIndex.SelectWindowPolygon(innerArea).OfType<DBObject>().ToHashSet();

                    var outerColumns = columnSpatialIndex.SelectCrossingPolygon(o).OfType<DBObject>().ToHashSet();
                    var outerShearWalls = shearWallSpatialIndex.SelectCrossingPolygon(o).OfType<DBObject>().ToHashSet();
                    outerColumns.ExceptWith(innerColumns);
                    outerShearWalls.ExceptWith(innerShearWalls);

                    var outerBeams = beamSpatialIndex.SelectCrossingPolygon(o).OfType<DBObject>().ToHashSet();
                    var innerBeams = beamSpatialIndex.SelectWindowPolygon(innerArea).OfType<DBObject>().ToHashSet();
                    outerBeams.ExceptWith(innerBeams); // 
                    innerBeams.OfType<DBObject>().ForEach(e =>
                    {
                        var link = beamLinkMap[e];
                        var isStartLinkVComponent = link.Start //起始端连接Vertical component
                        .Where(s => s.Outline != null)
                        .Where(s =>
                        {
                            if (s is ThIfcColumn column)
                            {
                                return outerColumns.Contains(column.Outline);
                            }
                            else if (s is ThIfcWall wall)
                            {
                                return outerShearWalls.Contains(wall.Outline);
                            }
                            else
                            {
                                //Unknown
                                return false;
                            }
                        }).Any();
                        var isEndLinkVComponent = link.End //末端连接Vertical component
                        .Where(s => s.Outline != null)
                        .Where(s =>
                        {
                            if (s is ThIfcColumn column)
                            {
                                return outerColumns.Contains(column.Outline);
                            }
                            else if (s is ThIfcWall wall)
                            {
                                return outerShearWalls.Contains(wall.Outline);
                            }
                            else
                            {
                                //Unknown
                                return false;
                            }
                        }).Any();

                        if (isStartLinkVComponent == false && isEndLinkVComponent == false)
                        {
                            link.Start.Where(s => s.Outline != null)
                            .ForEach(s =>
                            {
                                if (s is ThIfcColumn)
                                {
                                    if (innerColumns.Contains(s.Outline))
                                    {
                                        outerColumns.Add(s.Outline);
                                    }
                                }
                                else if (s is ThIfcWall)
                                {
                                    if (innerShearWalls.Contains(s.Outline))
                                    {
                                        outerShearWalls.Add(s.Outline);
                                    }
                                }
                            });
                        }
                    });

                    outerColumnsMap.ForEach(x => outerColumns.ExceptWith(x.Value));
                    outerShearWallsMap.ForEach(x => outerShearWalls.ExceptWith(x.Value));

                    outerColumnsMap.Add(o, outerColumns);
                    outerShearWallsMap.Add(o, outerShearWalls);
                }
            });

            // 收集返回的结果
            OuterColumnsMap = outerColumnsMap;
            OuterShearWallsMap = outerShearWallsMap;

            OtherColumns = _inputData.Columns.OfType<DBObject>().ToHashSet();
            OtherShearWalls = _inputData.Shearwalls.OfType<DBObject>().ToHashSet();
            outerColumnsMap.ForEach(o => OtherColumns.ExceptWith(o.Value));
            outerShearWallsMap.ForEach(o => OtherShearWalls.ExceptWith(o.Value));
        }
    }
    public class ThArchOuterVerticalComponentRecognizer : ThOuterVerticalComponentRecognizer
    {
        private ThArchOuterVertialComponentData _inputData { get; set; }
        public ThArchOuterVerticalComponentRecognizer(ThOuterVertialComponentData inputData)
        {
            if (inputData is ThArchOuterVertialComponentData data)
                _inputData = data;
            ArchOutlineOffsetLength = 2000.0;
        }
        public override void Recognize()
        {
            if (_inputData == null)
            {
                return;
            }
            //创建结构轮廓线和建筑轮廓线的字典
            //构建索引
            var columnSpatialIndex = new ThCADCoreNTSSpatialIndex(_inputData.Columns);
            var shearWallSpatialIndex = new ThCADCoreNTSSpatialIndex(_inputData.Shearwalls);

            var outerColumnsMap = new Dictionary<MPolygon, HashSet<DBObject>>();
            var outerShearWallsMap = new Dictionary<MPolygon, HashSet<DBObject>>();
            IBuffer bufferService = new ThNTSBufferService();
            _inputData.ArchOutlineAreas.ForEach(o =>
            {
                var innerArea = bufferService.Buffer(o, -1.0 * ArchOutlineOffsetLength); //内缩
                if (innerArea != null && innerArea is MPolygon polygon)
                {
                    // 拿到回形区域内的元素
                    var innerColumns = columnSpatialIndex.SelectWindowPolygon(innerArea).OfType<DBObject>().ToHashSet();
                    var innerShearWalls = shearWallSpatialIndex.SelectWindowPolygon(innerArea).OfType<DBObject>().ToHashSet();

                    var outerColumns = columnSpatialIndex.SelectCrossingPolygon(o).OfType<DBObject>().ToHashSet();
                    var outerShearWalls = shearWallSpatialIndex.SelectCrossingPolygon(o).OfType<DBObject>().ToHashSet();
                    outerColumns.ExceptWith(innerColumns);
                    outerShearWalls.ExceptWith(innerShearWalls);
                    outerColumnsMap.Add(o, outerColumns);
                    outerShearWallsMap.Add(o, outerShearWalls);
                }
            });

            // 收集返回的结果
            OuterColumnsMap = outerColumnsMap;
            OuterShearWallsMap = outerShearWallsMap;

            OtherColumns = _inputData.Columns.OfType<DBObject>().ToHashSet();
            OtherShearWalls = _inputData.Shearwalls.OfType<DBObject>().ToHashSet();
            outerColumnsMap.ForEach(o => OtherColumns.Except(o.Value));
            outerShearWallsMap.ForEach(o => OtherShearWalls.Except(o.Value));
        }
    }
}
