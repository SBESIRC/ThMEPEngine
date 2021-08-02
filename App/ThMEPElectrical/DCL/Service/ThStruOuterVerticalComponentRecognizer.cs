using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPElectrical.DCL.Data;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.DCL.Service
{
    /// <summary>
    /// 通过
    /// </summary>
    public class ThStruOuterVerticalComponentRecognizer : ThOuterVerticalComponentRecognizer
    {        
        private ThStruOuterVertialComponentData InputData { get; set; }
        private const double OuterArchOutlineOffsetLength = 500.0;
        private const double HoleArchOutlineOffsetLength = 500.0;        
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }

        public ThStruOuterVerticalComponentRecognizer(ThOuterVertialComponentData inputData)
        {
            if(inputData is ThStruOuterVertialComponentData data)
                InputData = data;
            OuterOutlineBufferDic = Buffer(InputData.OuterOutlines, -OuterArchOutlineOffsetLength);
            InnerOutlineBufferDic = Buffer(InputData.InnerOutlines, HoleArchOutlineOffsetLength);
        }
        public override void Recognize()
        {
            //为了创建悬臂梁的索引，先构建悬臂梁的轮廓与悬臂梁本身的字典
            //创建悬臂梁所有的轮廓线的DBObjectionCollection，以便创建索引
            Dictionary<DBObject, ThBeamLink> OverhangingBeamDict = new Dictionary<DBObject, ThBeamLink>();
            DBObjectCollection OverhangingBeamOutline = new DBObjectCollection();
            InputData.OverhangingPrimaryBeams.ForEach(o =>
            {
                o.Beams.ForEach(p =>
                {
                    OverhangingBeamDict.Add(p.Outline, o);
                    OverhangingBeamOutline.Add(p.Outline);
                });
            });
            //实现方法
            //外轮廓线内缩500，生成一个内缩框线，外轮廓线和内缩框线形成一个回形区域
            //a、对于直接处于内缩框线外的竖向构件，则直接判定为外圈竖向构件
            //b、对于回形区域内能选中的竖向构件，直接判定为外圈竖向构件
            //c、对于回形区域能选中主梁，其向内缩框线内的一侧连接的竖向构件判定为外圈竖向构件
            //d、对于c有一个例外，即主梁远离内缩框线的一侧连接的竖向构件如果已经判定为外圈竖向构件，
            //且不能存在满足a/b/c的情况,则不应将其向内缩框线内的一侧连接的竖向构件判定为外圈构件，意思是a/b/c的优先级要大于d

            //对结构内外轮廓线分别构建框线对

            //创建结构轮廓线和建筑轮廓线的字典


            //构建索引
            var ColumnSpatialIndex = new ThCADCoreNTSSpatialIndex(InputData.Columns);
            var ShearWallSpatialIndex = new ThCADCoreNTSSpatialIndex(InputData.Shearwalls);
            var OverhangingBeamSpatialIndex = new ThCADCoreNTSSpatialIndex(OverhangingBeamOutline);
            //对结构外框线进行处理
            OuterLineHandleOverhangingBeam(OverhangingBeamSpatialIndex, ShearWallSpatialIndex, ColumnSpatialIndex, OverhangingBeamDict, OuterOutlineBufferDic);
            OuterLineHandleColumn(ColumnSpatialIndex, OuterOutlineBufferDic);
            OuterLineHandleShearWall(ShearWallSpatialIndex, OuterOutlineBufferDic);
            //对内框线（洞口）进行处理
            InnerLineHandleOverhangingBeam(OverhangingBeamSpatialIndex, ShearWallSpatialIndex, ColumnSpatialIndex, OverhangingBeamDict, InnerOutlineBufferDic);
            InnerLineHandleColumn(ColumnSpatialIndex, InnerOutlineBufferDic);
            InnerLineHandleShearWall(ShearWallSpatialIndex, InnerOutlineBufferDic);
            //至此认为外圈构件创建完成，创建其他构建
            OtherColumns = DBObjectCollectionSubtraction(InputData.Columns, OuterColumns);
            OtherShearwalls = DBObjectCollectionSubtraction(InputData.Shearwalls, OuterShearwalls);
        }
        private void OuterLineHandleOverhangingBeam(ThCADCoreNTSSpatialIndex overhangingBeamSpatialIndex, 
                                            ThCADCoreNTSSpatialIndex shearWallSpatialIndex, 
                                            ThCADCoreNTSSpatialIndex columnSpatialIndex,
                                            Dictionary<DBObject, ThBeamLink> overhangingBeamDict,
                                            Dictionary<Polyline,Polyline> bufferres)
        {
            foreach(var item in bufferres)
            {
                //Polyline BiggerOuterLine = BufferPolyline(item.Key);先尝试不buffer的
                DBObjectCollection SelectedOverhangingBeam =
                    DBObjectCollectionSubtraction(overhangingBeamSpatialIndex.SelectCrossingPolygon(item.Key),
                       overhangingBeamSpatialIndex.SelectWindowPolygon(item.Value));
                foreach (DBObject obj in SelectedOverhangingBeam)
                {
                    var beam = overhangingBeamDict[obj];
                    List<ThIfcBuildingElement> BeamLineElement = new List<ThIfcBuildingElement>();
                    BeamLineElement.AddRange(beam.Start);
                    BeamLineElement.AddRange(beam.End);
                    BeamLineElement.ForEach(o =>
                    {
                        //仅对位于内框线以内的元素进行收集
                        if (o is ThIfcWall wall && shearWallSpatialIndex.SelectWindowPolygon(item.Value).Contains(wall.Outline) && !OuterShearwalls.Contains(wall.Outline))
                        {
                            OuterShearwalls.Add(wall.Outline);
                            //麻烦的来了，由于结构轮廓线和建筑轮廓线并不一致，所以必须为墙和柱找到对应的建筑轮廓线
                            //OuterShearWallBelongedArchOutlineID.Add(wall.Outline, OuterArchOutlineID[item.Key]);
                        }
                            
                        else if (o is ThIfcColumn column && columnSpatialIndex.SelectWindowPolygon(item.Value).Contains(column.Outline) && !OuterColumns.Contains(column.Outline))
                        {
                            OuterColumns.Add(column.Outline);
                            //OuterColumnBelongedArchOutlineID.Add(column.Outline, OuterArchOutlineID[item.Key]);
                        }
                            
                    });
                }
            }

        }
        //内侧的逻辑可能是不对的！！！！！！！！！！！！
        //TODO: TEST INNER
        private void InnerLineHandleOverhangingBeam(ThCADCoreNTSSpatialIndex overhangingBeamSpatialIndex, 
                                            ThCADCoreNTSSpatialIndex shearWallSpatialIndex,
                                            ThCADCoreNTSSpatialIndex columnSpatialIndex,
                                            Dictionary<DBObject, ThBeamLink> overhangingBeamDict,
                                            Dictionary<Polyline,Polyline> bufferres)
        {
            foreach (var item in bufferres)
            {
                DBObjectCollection SelectedOverhangingBeam =
                    DBObjectCollectionSubtraction(overhangingBeamSpatialIndex.SelectCrossingPolygon(item.Value),
                       overhangingBeamSpatialIndex.SelectWindowPolygon(item.Key));
                foreach (DBObject obj in SelectedOverhangingBeam)
                {
                    var beam = overhangingBeamDict[obj];
                    List<ThIfcBuildingElement> BeamLineElement = new List<ThIfcBuildingElement>();
                    BeamLineElement.AddRange(beam.Start);
                    BeamLineElement.AddRange(beam.End);
                    BeamLineElement.ForEach(o =>
                    {
                        //仅对位于内框线以内的元素进行收集。同时，判断去重。
                        if (o is ThIfcWall wall && !shearWallSpatialIndex.SelectWindowPolygon(item.Key).Contains(wall.Outline) && !OuterShearwalls.Contains(wall.Outline))
                            OuterShearwalls.Add(wall.Outline);
                        else if (o is ThIfcColumn column && !columnSpatialIndex.SelectWindowPolygon(item.Key).Contains(column.Outline) && !OuterColumns.Contains(column.Outline))
                            OuterColumns.Add(column.Outline);
                    });
                }
            }
        }
        private void FilterOutOfOutlines(List<Polyline> outlines)
        {
            // 对于直接处于内缩框线外的竖向构件，则直接判定为外圈竖向构件            
            var collector = new DBObjectCollection();
            outlines.ForEach(o =>
            {
                foreach (DBObject obj in SpatialIndex.SelectWindowPolygon(o))
                {
                    collector.Add(obj);
                }
            });
            foreach(DBObject temp in InputData.Columns)
            {
                if(!collector.Contains(temp))
                {
                    OuterColumns.Add(temp);
                }
            }
            foreach(DBObject temp in InputData.Shearwalls)
            {
                if(!collector.Contains(temp))
                {
                    OuterShearwalls.Add(temp);
                }
            }
        }
        private void FilterMPolygonArea(MPolygon polygon)
        {
        }
        private Polyline BufferPolyline(Polyline pol, double distance=5.0)
        {
            var ObjCollection = pol.Buffer(distance);
            //拿面积最大的Polyline
            double MaxArea = double.MinValue;
            if(ObjCollection.Count>0)
            {
                var result = new Polyline();
                foreach(DBObject dBObject in ObjCollection)
                {
                    if (dBObject is Polyline polyline && polyline.Area>MaxArea)
                    {
                        result = polyline;
                        MaxArea = polyline.Area;
                    }
                }
                return result;
            }
            return new Polyline();
        }
        private bool PolylineContainsOverhangingPrimaryBeam(Polyline polyline, ThBeamLink hangingbeam)
        {
            foreach(var temp in hangingbeam.Beams)
            {
                if(polyline.Contains((Polyline)temp.Outline))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 返回两个DBobjectCollection的差集
        /// </summary>
        /// <param name="Polylinecollection_1">集合1</param>
        /// <param name="Polylinecollection_2">集合2</param>
        /// <returns>集合1-集合2</returns>
    }
}
