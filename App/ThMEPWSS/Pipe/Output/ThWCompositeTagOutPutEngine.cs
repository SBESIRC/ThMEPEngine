using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Geom;
using ThMEPWSS.Pipe.Tools;
using static ThMEPWSS.ThPipeCmds;

namespace ThMEPWSS.Pipe.Output
{
    public  class ThWCompositeTagOutPutEngine
    {
        public void LayoutTag(ThWCompositeFloorRecognitionEngine FloorEngines, ThWTopParameters parameters0, ThWRoofParameters parameters1, 
            ThWRoofDeviceParameters parameters2, AcadDatabase acadDatabase, ThWInnerPipeIndexEngine PipeindexEngine,
            ThWCompositeIndexEngine composite_Engine,List<Curve> obstacleParameters,int scaleFactor,string PipeLayer,string W_DRAI_EQPM,string W_RAIN_NOTE1)
        {          
            var tag_frames = new List<Polyline>();//用来收纳所有文字框
            var f_pipes= ThWPipeOutputFunction.GetNewPipes(parameters0.fpipe);
            var rain_pipes = ThWPipeOutputFunction.GetNewPipes(parameters0.rain_pipe);//雨水管去重复
            var npipes = ThWPipeOutputFunction.GetNewPipes(parameters0.npipe);//冷凝管去重
            var roofrain_pipes = ThWPipeOutputFunction.GetNewPipes(parameters0.roofrain_pipe);//屋顶雨水管去重                                            
            double x1 = ThWPipeOutputFunction.GetBalconyRoom_x(FloorEngines.TopFloors[0].CompositeBalconyRooms);
            double y1 = ThWPipeOutputFunction.GetBalconyRoom_y(FloorEngines.TopFloors[0].CompositeBalconyRooms);
            double x = ThWPipeOutputFunction.GetToilet_x(FloorEngines.TopFloors[0].CompositeRooms);
            double y = ThWPipeOutputFunction.GetToilet_y(FloorEngines.TopFloors[0].CompositeRooms);
            //卫生间空间形心
            Point3d toiletpoint = new Point3d(x / FloorEngines.TopFloors[0].CompositeRooms.Count,
                y / FloorEngines.TopFloors[0].CompositeRooms.Count, 0);
            //阳台空间形心
            Point3d balconypoint = new Point3d(x1 / FloorEngines.TopFloors[0].CompositeBalconyRooms.Count,
            y1 / FloorEngines.TopFloors[0].CompositeBalconyRooms.Count, 0);
            //定义障碍              
            ThCADCoreNTSSpatialIndex obstacle = null;       
            obstacle = new ThCADCoreNTSSpatialIndex(ThWPipeOutputFunction.GetObstacle(obstacleParameters));
            composite_Engine.Run(f_pipes, parameters0.tpipe, parameters0.wpipe, parameters0.ppipe, parameters0.dpipe, npipes,
                rain_pipes, parameters0.pboundary, parameters0.divideLines, roofrain_pipes, toiletpoint, balconypoint, obstacle, scaleFactor);
            //首先得到比对的第一行重复标注  
            GetFpipeindex(composite_Engine, tag_frames, parameters0, PipeindexEngine, obstacle, acadDatabase, scaleFactor);
            GetTpipeindex(composite_Engine, tag_frames, parameters0, PipeindexEngine, obstacle, acadDatabase, scaleFactor);
            GetWpipeindex(composite_Engine, tag_frames, parameters0,PipeindexEngine, obstacle, acadDatabase, scaleFactor);
            GetPpipeindex(composite_Engine, tag_frames, parameters0,PipeindexEngine, obstacle, acadDatabase, scaleFactor);
            GetDpipeindex(composite_Engine, tag_frames, parameters0, PipeindexEngine, obstacle, acadDatabase, scaleFactor);
            GetNpipeindex(composite_Engine, tag_frames, parameters0, PipeindexEngine, obstacle, acadDatabase, scaleFactor, W_DRAI_EQPM);
            GetRainPipeindex(composite_Engine, tag_frames, parameters0, PipeindexEngine, obstacle, acadDatabase, scaleFactor, W_RAIN_NOTE1);
            GetRoofRainPipeindex(composite_Engine, tag_frames, parameters0, PipeindexEngine, obstacle, acadDatabase, scaleFactor, W_RAIN_NOTE1);
            GetCopiedPipeindex(FloorEngines, parameters0, acadDatabase, obstacle, parameters1, parameters2, composite_Engine, toiletpoint, balconypoint, scaleFactor, W_RAIN_NOTE1);
            var record0 = BlockTools.AddBlockTableRecord(acadDatabase.Database, $"标准层{FloorEngines.TopFloors[0].Space.Tags[0]}", parameters0.standardEntity);
            BlockReference standardBlock = new BlockReference(new Point3d(0, 0, 0), record0);
            acadDatabase.ModelSpace.Add(standardBlock);//管井信息打印到标准层 
            var record = BlockTools.AddBlockTableRecord(acadDatabase.Database, "大屋面", parameters1.roofEntity);
            BlockReference roofBlock = new BlockReference(new Point3d(0, 0, 0), record);
            acadDatabase.ModelSpace.Add(roofBlock);//管井复制到屋顶层
            var record1 = BlockTools.AddBlockTableRecord(acadDatabase.Database, "小屋面", parameters2.roofDeviceEntity);
            BlockReference roofDevBlock = new BlockReference(new Point3d(0, 0, 0), record1);
            acadDatabase.ModelSpace.Add(roofDevBlock);//管井复制到屋顶设备层 
        }
        public static void LayoutToiletPipe(ThWCompositePipeEngine compositeEngine, ThWTopParameters parameters0, AcadDatabase acadDatabase,string W_DRAI_EQPM)
        {
            for (int i = 0; i < compositeEngine.ToiletPipes.Count; i++)
            {
                var toilet = compositeEngine.ToiletPipes[i];
                var radius = compositeEngine.ToiletPipeEngine.Parameters.Identifier[i].Item2 / 2.0;
                ThWPipeOutputFunction.GetListFpipes(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.fpipe.Add(o));
                ThWPipeOutputFunction.GetListPpipes(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.ppipe.Add(o));
                ThWPipeOutputFunction.GetListWpipes(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.wpipe.Add(o));
                ThWPipeOutputFunction.GetListTpipes(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.tpipe.Add(o));
                ThWPipeOutputFunction.GetListDpipes(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.dpipe.Add(o));
                ThWPipeOutputFunction.GetListCopypipes(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.copypipes.Add(o));
                ThWPipeOutputFunction.GetListNormalCopypipes(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.normalCopys.Add(o));
                //在顶层打印                              
                ThWPipeOutputFunction.GetEntityPolyline(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.standardEntity.Add(o));
            }
        }
        public static void LayoutToiletPipe1(ThWCompositePipeEngine compositeEngine, ThWTopParameters parameters0, AcadDatabase acadDatabase,string W_DRAI_EQPM)
        {
            for (int i = 0; i < compositeEngine.ToiletPipes.Count; i++)
            {
                var toilet = compositeEngine.ToiletPipes[i];
                var radius = compositeEngine.ToiletPipeEngine.Parameters.Identifier[i].Item2 / 2.0;
                ThWPipeOutputFunction.GetListFpipes1(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.fpipe.Add(o));
                ThWPipeOutputFunction.GetListPpipes1(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.ppipe.Add(o));
                ThWPipeOutputFunction.GetListWpipes1(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.wpipe.Add(o));
                ThWPipeOutputFunction.GetListTpipes1(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.tpipe.Add(o));
                ThWPipeOutputFunction.GetListDpipes1(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.dpipe.Add(o));
                ThWPipeOutputFunction.GetListCopypipes1(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.copypipes.Add(o));
                ThWPipeOutputFunction.GetListNormalCopypipes1(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.normalCopys.Add(o));
                //在顶层打印                              
                ThWPipeOutputFunction.GetEntityPolyline1(toilet, compositeEngine.ToiletPipes, W_DRAI_EQPM).ForEach(o => parameters0.standardEntity.Add(o));
            }
        }
       private static void GetFpipeindex(ThWCompositeIndexEngine composite_Engine,List<Polyline> tag_frames, ThWTopParameters parameters0,
           ThWInnerPipeIndexEngine PipeindexEngine, ThCADCoreNTSSpatialIndex obstacle,AcadDatabase acadDatabase,int scaleFactor)
        {
            for (int j = 0; j < composite_Engine.PipeEngine.Fpipeindex.Count; j++)
            {
                Point3d dublicatePoint = Point3d.Origin;
                for (int i = 0; i < composite_Engine.PipeEngine.Fpipeindex[j].Count; i++)
                {
                    ThCADCoreNTSSpatialIndex obstacle_tag = new ThCADCoreNTSSpatialIndex(ThWPipeOutputFunction.GetFont(tag_frames));
                    double Yoffset = 0.0;
                    //此处添加同行调整后如果碰撞的情况
                    if (composite_Engine.FpipeDublicated.Count > 0)
                    {
                        dublicatePoint = ThWPipeOutputFunction.GetdublicatePoint(composite_Engine.FpipeDublicated[j], PipeindexEngine.Fpipeindex[j][i]);
                        Yoffset = ThWPipeOutputFunction.GetOffset(composite_Engine.FpipeDublicated[j], PipeindexEngine.Fpipeindex[j][i]);
                    }
                    Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                    var Matrix = Matrix3d.Displacement(s);
                    var matrix1 = Matrix3d.Displacement(PipeindexEngine.Fpipeindex_tag[j][3 * i].
                        GetVectorTo(ThWPipeOutputFunction.GetRadialPoint(dublicatePoint, obstacle, scaleFactor)));
                    Point3d tag1 = Point3d.Origin;
                    Point3d tag2 = Point3d.Origin;
                    Point3d tag3 = Point3d.Origin;
                    if (Yoffset >= 0 && PipeindexEngine.Fpipeindex[j][i].X == PipeindexEngine.Fpipeindex_tag[j][3 * i].X)
                    {
                        var fontBox = obstacle.SelectCrossingPolygon(ThWPipeOutputFunction.GetBoundary(175* scaleFactor * 7,
                        PipeindexEngine.Fpipeindex_tag[j][3 * i + 2].TransformBy(matrix1).TransformBy(Matrix), scaleFactor));//新生成的仍要考虑躲避障碍                                                              
                        tag1 = ThWPipeOutputFunction.GetTag(fontBox, PipeindexEngine.Fpipeindex_tag[j], 3 * i, matrix1, Matrix, obstacle_tag,scaleFactor, PipeindexEngine.Fpipeindex[j]);
                        tag2 = PipeindexEngine.Fpipeindex_tag[j][3 * i + 1] + PipeindexEngine.Fpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Fpipeindex_tag[j][3 * i + 2] + PipeindexEngine.Fpipeindex_tag[j][3 * i].GetVectorTo(tag1);                      
                    }
                    else
                    {
                        tag1 = ThWPipeOutputFunction.GetTag1(PipeindexEngine.Fpipeindex_tag[j], 3 * i, obstacle_tag, scaleFactor, PipeindexEngine.Fpipeindex[j]);
                        tag2 = PipeindexEngine.Fpipeindex_tag[j][3 * i + 1] + PipeindexEngine.Fpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Fpipeindex_tag[j][3 * i + 2] + PipeindexEngine.Fpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    Line tag_Yline = CreateLine(PipeindexEngine.Fpipeindex[j][i], tag1);
                    tag_Yline.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Yline);
                    parameters0.copypipes.Add(tag_Yline);
                    parameters0.normalCopys.Add(tag_Yline);
                    Line tag_Xline = CreateLine(tag1, tag2);
                    tag_Xline.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Xline);
                    parameters0.copypipes.Add(tag_Xline);
                    parameters0.normalCopys.Add(tag_Xline);           
                    DBText taggingtext = ThWPipeOutputFunction.Taggingtext(tag3, $"FL{j / 2}-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext1 = ThWPipeOutputFunction.Taggingtext(tag3, $"FL-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext1.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext2 = ThWPipeOutputFunction.Taggingtext(tag3, $"FL{j / 2}-{i + 1}‘", scaleFactor, acadDatabase.Database);
                    taggingtext2.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext3 = ThWPipeOutputFunction.Taggingtext(tag3, $"FL-{i + 1}’", scaleFactor, acadDatabase.Database);
                    taggingtext3.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    if (j == 0)
                    {
                        parameters0.standardEntity.Add(taggingtext1);
                        parameters0.copypipes.Add(taggingtext1);
                        parameters0.normalCopys.Add(taggingtext1);
                    }
                    else if (j == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext3);
                        parameters0.copypipes.Add(taggingtext3);
                        parameters0.normalCopys.Add(taggingtext3);
                    }
                    else if (j % 2 == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext2);
                        parameters0.copypipes.Add(taggingtext2);
                        parameters0.normalCopys.Add(taggingtext2);
                    }
                    else
                    {
                        parameters0.standardEntity.Add(taggingtext);
                        parameters0.copypipes.Add(taggingtext);
                        parameters0.normalCopys.Add(taggingtext);
                    }
                    tag_frames.Add(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor, tag3, scaleFactor));
                }
            }
        }
        private static void GetTpipeindex(ThWCompositeIndexEngine composite_Engine, List<Polyline> tag_frames, ThWTopParameters parameters0,
           ThWInnerPipeIndexEngine PipeindexEngine, ThCADCoreNTSSpatialIndex obstacle, AcadDatabase acadDatabase,int scaleFactor)
        {
            for (int j = 0; j < composite_Engine.PipeEngine.Tpipeindex.Count; j++)
            {
                Point3d dublicatePoint = Point3d.Origin;
                for (int i = 0; i < composite_Engine.PipeEngine.Tpipeindex[j].Count; i++)
                {
                    ThCADCoreNTSSpatialIndex obstacle_tag = new ThCADCoreNTSSpatialIndex(ThWPipeOutputFunction.GetFont(tag_frames));
                    double Yoffset = 0.0;
                    if (composite_Engine.FpipeDublicated.Count > 0)
                    {
                        if (j < composite_Engine.FpipeDublicated.Count)
                        {
                            dublicatePoint = ThWPipeOutputFunction.GetdublicatePoint(composite_Engine.FpipeDublicated[j], PipeindexEngine.Tpipeindex[j][i]);
                            Yoffset = ThWPipeOutputFunction.GetOffset(composite_Engine.FpipeDublicated[j], PipeindexEngine.Tpipeindex[j][i]);
                        }
                    }
                    Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                    var Matrix = Matrix3d.Displacement(s);
                    var matrix1 = Matrix3d.Displacement(PipeindexEngine.Tpipeindex_tag[j][3 * i].
                        GetVectorTo(ThWPipeOutputFunction.GetRadialPoint(dublicatePoint, obstacle, scaleFactor)));
                    Point3d tag1 = Point3d.Origin;
                    Point3d tag2 = Point3d.Origin;
                    Point3d tag3 = Point3d.Origin;
                    if (Yoffset >= 0 && PipeindexEngine.Tpipeindex[j][i].X == PipeindexEngine.Tpipeindex_tag[j][3 * i].X)
                    {
                        var fontBox = obstacle.SelectCrossingPolygon(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor,
                        PipeindexEngine.Tpipeindex_tag[j][3 * i + 2].TransformBy(matrix1).TransformBy(Matrix), scaleFactor));//新生成的仍要考虑躲避障碍
                        tag1 = ThWPipeOutputFunction.GetTag(fontBox, PipeindexEngine.Tpipeindex_tag[j], 3 * i, matrix1, Matrix, obstacle_tag, scaleFactor, PipeindexEngine.Tpipeindex[j]);
                        tag2 = PipeindexEngine.Tpipeindex_tag[j][3 * i + 1] + PipeindexEngine.Tpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Tpipeindex_tag[j][3 * i + 2] + PipeindexEngine.Tpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    else
                    {
                        tag1 = ThWPipeOutputFunction.GetTag1(PipeindexEngine.Tpipeindex_tag[j], 3 * i, obstacle_tag, scaleFactor, PipeindexEngine.Tpipeindex[j]);
                        tag2 = PipeindexEngine.Tpipeindex_tag[j][3 * i + 1] + PipeindexEngine.Tpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Tpipeindex_tag[j][3 * i + 2] + PipeindexEngine.Tpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    Line tag_Yline = CreateLine(PipeindexEngine.Tpipeindex[j][i], tag1);
                    tag_Yline.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Yline);
                    parameters0.normalCopys.Add(tag_Yline);
                    Line tag_Xline = CreateLine(tag1, tag2);
                    tag_Xline.Layer= ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Xline);
                    parameters0.normalCopys.Add(tag_Xline);
                    DBText taggingtext = ThWPipeOutputFunction.Taggingtext(tag3, $"TL{j / 2}-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext1 = ThWPipeOutputFunction.Taggingtext(tag3, $"TL-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext1.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext2 = ThWPipeOutputFunction.Taggingtext(tag3, $"TL{j / 2}-{i + 1}‘", scaleFactor, acadDatabase.Database);
                    taggingtext2.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext3 = ThWPipeOutputFunction.Taggingtext(tag3, $"TL-{i + 1}’", scaleFactor, acadDatabase.Database);
                    taggingtext3.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    if (j == 0)
                    {
                        parameters0.standardEntity.Add(taggingtext1);
                        parameters0.normalCopys.Add(taggingtext1);
                    }
                    else if (j == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext3);
                        parameters0.normalCopys.Add(taggingtext3);
                    }
                    else if (j % 2 == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext2);
                        parameters0.normalCopys.Add(taggingtext2);
                    }
                    else
                    {
                        parameters0.standardEntity.Add(taggingtext);
                        parameters0.normalCopys.Add(taggingtext);
                    }
                    tag_frames.Add(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor, tag3, scaleFactor));
                }
            }
        }
        private static void GetWpipeindex(ThWCompositeIndexEngine composite_Engine, List<Polyline> tag_frames, ThWTopParameters parameters0,
        ThWInnerPipeIndexEngine PipeindexEngine, ThCADCoreNTSSpatialIndex obstacle, AcadDatabase acadDatabase,int scaleFactor)
        {
            for (int j = 0; j < composite_Engine.PipeEngine.Wpipeindex.Count; j++)
            {
                Point3d dublicatePoint = Point3d.Origin;
                for (int i = 0; i < composite_Engine.PipeEngine.Wpipeindex[j].Count; i++)
                {
                    ThCADCoreNTSSpatialIndex obstacle_tag = new ThCADCoreNTSSpatialIndex(ThWPipeOutputFunction.GetFont(tag_frames));
                    double Yoffset = 0.0;
                    if (composite_Engine.FpipeDublicated.Count > 0)
                    {
                        dublicatePoint = ThWPipeOutputFunction.GetdublicatePoint
                            (composite_Engine.FpipeDublicated[j], PipeindexEngine.Wpipeindex[j][i]);
                        Yoffset = ThWPipeOutputFunction.GetOffset(composite_Engine.FpipeDublicated[j], PipeindexEngine.Wpipeindex[j][i]);
                    }
                    Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                    var Matrix = Matrix3d.Displacement(s);
                    var matrix1 = Matrix3d.Displacement(PipeindexEngine.Wpipeindex_tag[j][3 * i].GetVectorTo
                        (ThWPipeOutputFunction.GetRadialPoint(dublicatePoint, obstacle, scaleFactor)));
                    Point3d tag1 = Point3d.Origin;
                    Point3d tag2 = Point3d.Origin;
                    Point3d tag3 = Point3d.Origin;
                    if (Yoffset >= 0 && PipeindexEngine.Wpipeindex[j][i].X == PipeindexEngine.Wpipeindex_tag[j][3 * i].X)
                    {
                        var fontBox = obstacle.SelectCrossingPolygon(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor,
                        PipeindexEngine.Wpipeindex_tag[j][3 * i + 2].TransformBy(matrix1).TransformBy(Matrix), scaleFactor));//新生成的仍要考虑躲避障碍
                        tag1 = ThWPipeOutputFunction.GetTag(fontBox, PipeindexEngine.Wpipeindex_tag[j], 3 * i, matrix1, Matrix, obstacle_tag, scaleFactor, PipeindexEngine.Wpipeindex[j]);
                        tag2 = PipeindexEngine.Wpipeindex_tag[j][3 * i + 1] + PipeindexEngine.Wpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Wpipeindex_tag[j][3 * i + 2] + PipeindexEngine.Wpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    else
                    {
                        tag1 = ThWPipeOutputFunction.GetTag1(PipeindexEngine.Wpipeindex_tag[j], 3 * i, obstacle_tag, scaleFactor, PipeindexEngine.Wpipeindex[j]);
                        tag2 = PipeindexEngine.Wpipeindex_tag[j][3 * i + 1] + PipeindexEngine.Wpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Wpipeindex_tag[j][3 * i + 2] + PipeindexEngine.Wpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    Line tag_Yline = CreateLine(PipeindexEngine.Wpipeindex[j][i], tag1);
                    tag_Yline.Layer= ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Yline);
                    parameters0.copypipes.Add(tag_Yline);
                    parameters0.normalCopys.Add(tag_Yline);
                    Line tag_Xline = CreateLine(tag1, tag2);
                    tag_Xline.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Xline);
                    parameters0.copypipes.Add(tag_Xline);
                    parameters0.normalCopys.Add(tag_Xline);
                    DBText taggingtext = ThWPipeOutputFunction.Taggingtext(tag3, $"WL{j / 2}-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext1 = ThWPipeOutputFunction.Taggingtext(tag3, $"WL-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext1.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext2 = ThWPipeOutputFunction.Taggingtext(tag3, $"WL{j / 2}-{i + 1}‘", scaleFactor, acadDatabase.Database);
                    taggingtext2.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext3 = ThWPipeOutputFunction.Taggingtext(tag3, $"WL-{i + 1}’", scaleFactor, acadDatabase.Database);
                    taggingtext3.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    if (j == 0)
                    {
                        parameters0.standardEntity.Add(taggingtext1);
                        parameters0.copypipes.Add(taggingtext1);
                        parameters0.normalCopys.Add(taggingtext1);
                    }
                    else if (j == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext3);
                        parameters0.copypipes.Add(taggingtext3);
                        parameters0.normalCopys.Add(taggingtext3);
                    }
                    else if (j % 2 == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext2);
                        parameters0.copypipes.Add(taggingtext2);
                        parameters0.normalCopys.Add(taggingtext2);
                    }
                    else
                    {
                        parameters0.standardEntity.Add(taggingtext);
                        parameters0.copypipes.Add(taggingtext);
                        parameters0.normalCopys.Add(taggingtext);
                    }
                    tag_frames.Add(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor, tag3, scaleFactor));
                }
            }
        }
        private static void GetPpipeindex(ThWCompositeIndexEngine composite_Engine, List<Polyline> tag_frames, ThWTopParameters parameters0,
       ThWInnerPipeIndexEngine PipeindexEngine, ThCADCoreNTSSpatialIndex obstacle, AcadDatabase acadDatabase,int scaleFactor)
        {
            for (int j = 0; j < composite_Engine.PipeEngine.Ppipeindex.Count; j++)
            {
                Point3d dublicatePoint = Point3d.Origin;
                for (int i = 0; i < composite_Engine.PipeEngine.Ppipeindex[j].Count; i++)
                {
                    ThCADCoreNTSSpatialIndex obstacle_tag = new ThCADCoreNTSSpatialIndex(ThWPipeOutputFunction.GetFont(tag_frames));
                    double Yoffset = 0.0;
                    if (composite_Engine.FpipeDublicated.Count > 0)
                    {
                        if (j < composite_Engine.FpipeDublicated.Count)
                        {
                            dublicatePoint = ThWPipeOutputFunction.GetdublicatePoint(composite_Engine.FpipeDublicated[j], PipeindexEngine.Ppipeindex[j][i]);
                            Yoffset = ThWPipeOutputFunction.GetOffset(composite_Engine.FpipeDublicated[j], PipeindexEngine.Ppipeindex[j][i]);
                        }
                    }
                    Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                    var Matrix = Matrix3d.Displacement(s);
                    var matrix1 = Matrix3d.Displacement(PipeindexEngine.Ppipeindex_tag[j][3 * i].
                        GetVectorTo(ThWPipeOutputFunction.GetRadialPoint(dublicatePoint, obstacle, scaleFactor)));
                    Point3d tag1 = Point3d.Origin;
                    Point3d tag2 = Point3d.Origin;
                    Point3d tag3 = Point3d.Origin;
                    if (Yoffset >= 0.0 && PipeindexEngine.Ppipeindex[j][i].X == PipeindexEngine.Ppipeindex_tag[j][3 * i].X)
                    {
                        var fontBox = obstacle.SelectCrossingPolygon(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor,
                        PipeindexEngine.Ppipeindex_tag[j][3 * i + 2].TransformBy(matrix1).TransformBy(Matrix), scaleFactor));//新生成的仍要考虑躲避障碍
                        tag1 = ThWPipeOutputFunction.GetTag(fontBox, PipeindexEngine.Ppipeindex_tag[j], 3 * i, matrix1, Matrix, obstacle_tag, scaleFactor, PipeindexEngine.Ppipeindex[j]);
                        tag2 = PipeindexEngine.Ppipeindex_tag[j][3 * i + 1] + PipeindexEngine.Ppipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Ppipeindex_tag[j][3 * i + 2] + PipeindexEngine.Ppipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    else
                    {             
                        tag1 = ThWPipeOutputFunction.GetTag1(PipeindexEngine.Ppipeindex_tag[j], 3 * i, obstacle_tag, scaleFactor, PipeindexEngine.Ppipeindex[j]);
                        tag2 = PipeindexEngine.Ppipeindex_tag[j][3 * i + 1] + PipeindexEngine.Ppipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Ppipeindex_tag[j][3 * i + 2] + PipeindexEngine.Ppipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    Line tag_Yline = CreateLine(PipeindexEngine.Ppipeindex[j][i], tag1);
                    tag_Yline.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Yline);
                    parameters0.copypipes.Add(tag_Yline);
                    parameters0.normalCopys.Add(tag_Yline);
                    Line tag_Xline = CreateLine(tag1, tag2);
                    tag_Xline.Layer= ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Xline);
                    parameters0.copypipes.Add(tag_Xline);
                    parameters0.normalCopys.Add(tag_Xline);
                    DBText taggingtext = ThWPipeOutputFunction.Taggingtext(tag3, $"PL{j / 2}-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext.Layer= ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext1 = ThWPipeOutputFunction.Taggingtext(tag3, $"PL-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext1.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext2 = ThWPipeOutputFunction.Taggingtext(tag3, $"PL{j / 2}-{i + 1}‘", scaleFactor, acadDatabase.Database);
                    taggingtext2.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext3 = ThWPipeOutputFunction.Taggingtext(tag3, $"PL-{i + 1}’", scaleFactor, acadDatabase.Database);
                    taggingtext3.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    if (j == 0)
                    {
                        parameters0.standardEntity.Add(taggingtext1);
                        parameters0.copypipes.Add(taggingtext1);
                        parameters0.normalCopys.Add(taggingtext1);
                    }
                    else if (j == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext3);
                        parameters0.copypipes.Add(taggingtext3);
                        parameters0.normalCopys.Add(taggingtext3);
                    }
                    else if (j % 2 == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext2);
                        parameters0.copypipes.Add(taggingtext2);
                        parameters0.normalCopys.Add(taggingtext2);
                    }
                    else
                    {
                        parameters0.standardEntity.Add(taggingtext);
                        parameters0.copypipes.Add(taggingtext);
                        parameters0.normalCopys.Add(taggingtext);
                    }
                    tag_frames.Add(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor, tag3, scaleFactor));
                }
            }
        }
        private static void GetDpipeindex(ThWCompositeIndexEngine composite_Engine, List<Polyline> tag_frames, ThWTopParameters parameters0,
       ThWInnerPipeIndexEngine PipeindexEngine, ThCADCoreNTSSpatialIndex obstacle, AcadDatabase acadDatabase,int scaleFactor)
        {
            for (int j = 0; j < composite_Engine.PipeEngine.Dpipeindex.Count; j++)
            {
                Point3d dublicatePoint = Point3d.Origin;
                for (int i = 0; i < composite_Engine.PipeEngine.Dpipeindex[j].Count; i++)
                {
                    ThCADCoreNTSSpatialIndex obstacle_tag = new ThCADCoreNTSSpatialIndex(ThWPipeOutputFunction.GetFont(tag_frames));
                    double Yoffset = 0.0;
                    if (composite_Engine.FpipeDublicated.Count > 0)
                    {
                        dublicatePoint = ThWPipeOutputFunction.GetdublicatePoint(composite_Engine.FpipeDublicated[j], PipeindexEngine.Dpipeindex[j][i]);
                        Yoffset = ThWPipeOutputFunction.GetOffset(composite_Engine.FpipeDublicated[j], PipeindexEngine.Dpipeindex[j][i]);
                    }
                    Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                    var Matrix = Matrix3d.Displacement(s);
                    var matrix1 = Matrix3d.Displacement(PipeindexEngine.Dpipeindex_tag[j][3 * i].GetVectorTo
                        (ThWPipeOutputFunction.GetRadialPoint(dublicatePoint, obstacle, scaleFactor)));
                    Point3d tag1 = Point3d.Origin;
                    Point3d tag2 = Point3d.Origin;
                    Point3d tag3 = Point3d.Origin;
                    if (Yoffset >= 0.0 && PipeindexEngine.Dpipeindex[j][i].X == PipeindexEngine.Dpipeindex_tag[j][3 * i].X)
                    {
                        var fontBox = obstacle.SelectCrossingPolygon(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor,
                        PipeindexEngine.Dpipeindex_tag[j][3 * i + 2].TransformBy(matrix1).TransformBy(Matrix), scaleFactor));//新生成的仍要考虑躲避障碍
                        tag1 = ThWPipeOutputFunction.GetTag(fontBox, PipeindexEngine.Dpipeindex_tag[j], 3 * i, matrix1, Matrix, obstacle_tag, scaleFactor, PipeindexEngine.Dpipeindex[j]);
                        tag2 = PipeindexEngine.Dpipeindex_tag[j][3 * i + 1] + PipeindexEngine.Dpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Dpipeindex_tag[j][3 * i + 2] + PipeindexEngine.Dpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    else
                    {
                        tag1 = ThWPipeOutputFunction.GetTag1(PipeindexEngine.Dpipeindex_tag[j], 3 * i, obstacle_tag, scaleFactor, PipeindexEngine.Dpipeindex[j]);
                        tag2 = PipeindexEngine.Dpipeindex_tag[j][3 * i + 1] + PipeindexEngine.Dpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Dpipeindex_tag[j][3 * i + 2] + PipeindexEngine.Dpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    Line tag_Yline = CreateLine(PipeindexEngine.Dpipeindex[j][i], tag1);
                    tag_Yline.Layer= ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Yline);
                    parameters0.normalCopys.Add(tag_Yline);
                    Line tag_Xline = CreateLine(tag1, tag2);
                    tag_Xline.Layer= ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Xline);
                    parameters0.normalCopys.Add(tag_Xline);
                    DBText taggingtext = ThWPipeOutputFunction.Taggingtext(tag3, $"DL{j / 2}-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext1 = ThWPipeOutputFunction.Taggingtext(tag3, $"DL-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext1.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext2 = ThWPipeOutputFunction.Taggingtext(tag3, $"DL{j / 2}-{i + 1}‘", scaleFactor, acadDatabase.Database);
                    taggingtext2.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext3 = ThWPipeOutputFunction.Taggingtext(tag3, $"DL-{i + 1}’", scaleFactor, acadDatabase.Database);
                    taggingtext3.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    if (j == 0)
                    {
                        parameters0.standardEntity.Add(taggingtext1);
                        parameters0.normalCopys.Add(taggingtext1);
                    }
                    else if (j == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext3);
                        parameters0.normalCopys.Add(taggingtext3);
                    }
                    else if (j % 2 == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext2);
                        parameters0.normalCopys.Add(taggingtext2);
                    }
                    else
                    {
                        parameters0.standardEntity.Add(taggingtext);
                        parameters0.normalCopys.Add(taggingtext);
                    }
                    tag_frames.Add(ThWPipeOutputFunction.GetBoundary(175 * 7*scaleFactor, tag3, scaleFactor));
                }
            }
        }
        private static void GetNpipeindex(ThWCompositeIndexEngine composite_Engine, List<Polyline> tag_frames, ThWTopParameters parameters0,
      ThWInnerPipeIndexEngine PipeindexEngine, ThCADCoreNTSSpatialIndex obstacle, AcadDatabase acadDatabase,int scaleFactor,string W_DRAI_EQPM)
        {
            for (int j = 0; j < composite_Engine.PipeEngine.Npipeindex.Count; j++)
            {
                Point3d dublicatePoint = Point3d.Origin;
                for (int i = 0; i < composite_Engine.PipeEngine.Npipeindex[j].Count; i++)
                {
                    ThCADCoreNTSSpatialIndex obstacle_tag = new ThCADCoreNTSSpatialIndex(ThWPipeOutputFunction.GetFont(tag_frames));
                    double Yoffset = 0.0;
                    if (composite_Engine.FpipeDublicated.Count > 0)
                    {
                        if (j < composite_Engine.FpipeDublicated.Count)
                        {
                            dublicatePoint = ThWPipeOutputFunction.GetdublicatePoint(composite_Engine.FpipeDublicated[j], PipeindexEngine.Npipeindex[j][i]);
                            Yoffset = ThWPipeOutputFunction.GetOffset(composite_Engine.FpipeDublicated[j], PipeindexEngine.Npipeindex[j][i]);
                        }
                    }
                    Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                    var Matrix = Matrix3d.Displacement(s);
                    var matrix1 = Matrix3d.Displacement(PipeindexEngine.Npipeindex_tag[j][3 * i].GetVectorTo
                        (ThWPipeOutputFunction.GetRadialPoint(dublicatePoint, obstacle, scaleFactor)));
                    Point3d tag1 = Point3d.Origin;
                    Point3d tag2 = Point3d.Origin;
                    Point3d tag3 = Point3d.Origin;
                    if (Yoffset >= 0 && PipeindexEngine.Npipeindex[j][i].X == PipeindexEngine.Npipeindex_tag[j][3 * i].X)
                    {
                        var fontBox = obstacle.SelectCrossingPolygon(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor,
                        PipeindexEngine.Npipeindex_tag[j][3 * i + 2].TransformBy(matrix1).TransformBy(Matrix), scaleFactor));//新生成的仍要考虑躲避障碍
                        tag1 = ThWPipeOutputFunction.GetTag(fontBox, PipeindexEngine.Npipeindex_tag[j], 3 * i, matrix1, Matrix, obstacle_tag, scaleFactor, PipeindexEngine.Npipeindex[j]);
                        tag2 = PipeindexEngine.Npipeindex_tag[j][3 * i + 1] + PipeindexEngine.Npipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Npipeindex_tag[j][3 * i + 2] + PipeindexEngine.Npipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    else
                    {
                        tag1 = ThWPipeOutputFunction.GetTag1(PipeindexEngine.Npipeindex_tag[j], 3 * i, obstacle_tag, scaleFactor, PipeindexEngine.Npipeindex[j]);
                        tag2 = PipeindexEngine.Npipeindex_tag[j][3 * i + 1] + PipeindexEngine.Npipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Npipeindex_tag[j][3 * i + 2] + PipeindexEngine.Npipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    Circle circle = CreateCircle(PipeindexEngine.Npipeindex[j][i]);
                    circle.Layer = W_DRAI_EQPM;
                    parameters0.standardEntity.Add(circle);
                    parameters0.normalCopys.Add(circle);
                    Line tag_Yline = CreateLine(PipeindexEngine.Npipeindex[j][i], tag1);
                    tag_Yline.Layer= ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Yline);
                    parameters0.normalCopys.Add(tag_Yline);
                    Line tag_Xline = CreateLine(tag1, tag2);
                    tag_Xline.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    parameters0.standardEntity.Add(tag_Xline);
                    parameters0.normalCopys.Add(tag_Xline);
                    DBText taggingtext = ThWPipeOutputFunction.Taggingtext(tag3, $"NL{j / 2}-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext.Layer= ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext1 = ThWPipeOutputFunction.Taggingtext(tag3, $"NL-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext1.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext2 = ThWPipeOutputFunction.Taggingtext(tag3, $"NL{j / 2}-{i + 1}‘", scaleFactor, acadDatabase.Database);
                    taggingtext2.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    DBText taggingtext3 = ThWPipeOutputFunction.Taggingtext(tag3, $"NL-{i + 1}’", scaleFactor, acadDatabase.Database);
                    taggingtext3.Layer = ThWPipeCommon.W_DRAI_NOTE;
                    if (j == 0)
                    {
                        parameters0.standardEntity.Add(taggingtext1);
                        parameters0.normalCopys.Add(taggingtext1);
                    }
                    else if (j == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext3);
                        parameters0.normalCopys.Add(taggingtext3);
                    }
                    else if (j % 2 == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext2);
                        parameters0.normalCopys.Add(taggingtext2);
                    }
                    else
                    {
                        parameters0.standardEntity.Add(taggingtext);
                        parameters0.normalCopys.Add(taggingtext);
                    }
                    tag_frames.Add(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor, tag3, scaleFactor));
                }
            }
        }
        private static void GetRainPipeindex(ThWCompositeIndexEngine composite_Engine, List<Polyline> tag_frames, ThWTopParameters parameters0,
      ThWInnerPipeIndexEngine PipeindexEngine, ThCADCoreNTSSpatialIndex obstacle, AcadDatabase acadDatabase,int scaleFactor,string W_RAIN_NOTE1)
        {
            for (int j = 0; j < composite_Engine.PipeEngine.Rainpipeindex.Count; j++)
            {
                Point3d dublicatePoint = Point3d.Origin;
                for (int i = 0; i < composite_Engine.PipeEngine.Rainpipeindex[j].Count; i++)
                {
                    ThCADCoreNTSSpatialIndex obstacle_tag = new ThCADCoreNTSSpatialIndex(ThWPipeOutputFunction.GetFont(tag_frames));
                    double Yoffset = 0.0;
                    if (composite_Engine.FpipeDublicated.Count > 0)
                    {
                        if (j < composite_Engine.FpipeDublicated.Count)
                        {
                            dublicatePoint = ThWPipeOutputFunction.GetdublicatePoint(composite_Engine.FpipeDublicated[j], PipeindexEngine.Rainpipeindex[j][i]);
                            Yoffset = ThWPipeOutputFunction.GetOffset(composite_Engine.FpipeDublicated[j], PipeindexEngine.Rainpipeindex[j][i]);
                        }
                    }
                    Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                    var Matrix = Matrix3d.Displacement(s);
                    var matrix1 = Matrix3d.Displacement(PipeindexEngine.Rainpipeindex_tag[j][3 * i].GetVectorTo(ThWPipeOutputFunction.GetRadialPoint(dublicatePoint, obstacle, scaleFactor)));
                    Point3d tag1 = Point3d.Origin;
                    Point3d tag2 = Point3d.Origin;
                    Point3d tag3 = Point3d.Origin;
                    if (Yoffset >= 0 && PipeindexEngine.Rainpipeindex[j][i].X == PipeindexEngine.Rainpipeindex_tag[j][3 * i].X)
                    {
                        var fontBox = obstacle.SelectCrossingPolygon(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor,
                        PipeindexEngine.Rainpipeindex_tag[j][3 * i + 2].TransformBy(matrix1).TransformBy(Matrix), scaleFactor));//新生成的仍要考虑躲避障碍
                        tag1 = ThWPipeOutputFunction.GetTag(fontBox, PipeindexEngine.Rainpipeindex_tag[j], 3 * i, matrix1, Matrix, obstacle_tag, scaleFactor, PipeindexEngine.Rainpipeindex[j]);
                        tag2 = PipeindexEngine.Rainpipeindex_tag[j][3 * i + 1] + PipeindexEngine.Rainpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Rainpipeindex_tag[j][3 * i + 2] + PipeindexEngine.Rainpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    else
                    {
                        tag1 = ThWPipeOutputFunction.GetTag1(PipeindexEngine.Rainpipeindex_tag[j], 3 * i, obstacle_tag, scaleFactor, PipeindexEngine.Rainpipeindex[j]);
                        tag2 = PipeindexEngine.Rainpipeindex_tag[j][3 * i + 1] + PipeindexEngine.Rainpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.Rainpipeindex_tag[j][3 * i + 2] + PipeindexEngine.Rainpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    Circle circle = CreateCircle(PipeindexEngine.Rainpipeindex[j][i]);
                    circle.Layer = ThWPipeCommon.W_RAIN_EQPM;
                    parameters0.standardEntity.Add(circle);
                    parameters0.normalCopys.Add(circle);
                    Line tag_Yline = CreateLine(PipeindexEngine.Rainpipeindex[j][i], tag1);
                    tag_Yline.Layer= W_RAIN_NOTE1;
                    parameters0.standardEntity.Add(tag_Yline);
                    parameters0.normalCopys.Add(tag_Yline);
                    Line tag_Xline = CreateLine(tag1, tag2);
                    tag_Xline.Layer= W_RAIN_NOTE1;
                    parameters0.standardEntity.Add(tag_Xline);
                    parameters0.normalCopys.Add(tag_Xline);
                    DBText taggingtext = ThWPipeOutputFunction.Taggingtext(tag3, $"Y2L{j / 2}-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext.Layer= W_RAIN_NOTE1;
                    DBText taggingtext1 = ThWPipeOutputFunction.Taggingtext(tag3, $"Y2L-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext1.Layer = W_RAIN_NOTE1;
                    DBText taggingtext2 = ThWPipeOutputFunction.Taggingtext(tag3, $"Y2L{j / 2}-{i + 1}‘", scaleFactor, acadDatabase.Database);
                    taggingtext2.Layer = W_RAIN_NOTE1;
                    DBText taggingtext3 = ThWPipeOutputFunction.Taggingtext(tag3, $"Y2L-{i + 1}’", scaleFactor, acadDatabase.Database);
                    taggingtext3.Layer = W_RAIN_NOTE1;
                    if (j == 0)
                    {
                        parameters0.standardEntity.Add(taggingtext1);
                        parameters0.normalCopys.Add(taggingtext1);
                    }
                    else if (j == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext3);
                        parameters0.normalCopys.Add(taggingtext3);
                    }
                    else if (j % 2 == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext2);
                        parameters0.normalCopys.Add(taggingtext2);
                    }
                    else
                    {
                        parameters0.standardEntity.Add(taggingtext);
                        parameters0.normalCopys.Add(taggingtext);
                    }
                    tag_frames.Add(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor, tag3, scaleFactor));
                }
            }
        }
        private static void GetRoofRainPipeindex(ThWCompositeIndexEngine composite_Engine, List<Polyline> tag_frames, ThWTopParameters parameters0,
      ThWInnerPipeIndexEngine PipeindexEngine, ThCADCoreNTSSpatialIndex obstacle, AcadDatabase acadDatabase,int scaleFactor,string W_RAIN_NOTE1)
        {
            for (int j = 0; j < composite_Engine.PipeEngine.RoofRainpipeindex.Count; j++)
            {
                ThCADCoreNTSSpatialIndex obstacle_tag = new ThCADCoreNTSSpatialIndex(ThWPipeOutputFunction.GetFont(tag_frames));
                Point3d dublicatePoint = Point3d.Origin;
                for (int i = 0; i < composite_Engine.PipeEngine.RoofRainpipeindex[j].Count; i++)
                {
                    double Yoffset = 0.0;
                    if (composite_Engine.FpipeDublicated.Count > 0)
                    {
                        if (j < composite_Engine.FpipeDublicated.Count)
                        {
                            dublicatePoint = ThWPipeOutputFunction.GetdublicatePoint(composite_Engine.FpipeDublicated[j], PipeindexEngine.RoofRainpipeindex[j][i]);
                            Yoffset = ThWPipeOutputFunction.GetOffset(composite_Engine.FpipeDublicated[j], PipeindexEngine.RoofRainpipeindex[j][i]);
                        }
                    }
                    Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                    var Matrix = Matrix3d.Displacement(s);
                    var matrix1 = Matrix3d.Displacement(PipeindexEngine.RoofRainpipeindex_tag[j][3 * i].GetVectorTo
                        (ThWPipeOutputFunction.GetRadialPoint(dublicatePoint, obstacle, scaleFactor)));
                    Point3d tag1 = Point3d.Origin;
                    Point3d tag2 = Point3d.Origin;
                    Point3d tag3 = Point3d.Origin;
                    if (Yoffset >= 0 && PipeindexEngine.RoofRainpipeindex[j][i].X == PipeindexEngine.RoofRainpipeindex_tag[j][3 * i].X)
                    {
                        var fontBox = obstacle.SelectCrossingPolygon(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor,
                        PipeindexEngine.RoofRainpipeindex_tag[j][3 * i + 2].TransformBy(matrix1).TransformBy(Matrix), scaleFactor));//新生成的仍要考虑躲避障碍
                        tag1 = ThWPipeOutputFunction.GetTag(fontBox, PipeindexEngine.RoofRainpipeindex_tag[j], 3 * i, matrix1, Matrix, obstacle_tag, scaleFactor, PipeindexEngine.RoofRainpipeindex[j]);
                        tag2 = PipeindexEngine.RoofRainpipeindex_tag[j][3 * i + 1] + PipeindexEngine.RoofRainpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.RoofRainpipeindex_tag[j][3 * i + 2] + PipeindexEngine.RoofRainpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    else
                    {
                        tag1 = ThWPipeOutputFunction.GetTag1(PipeindexEngine.RoofRainpipeindex_tag[j], 3 * i, obstacle_tag, scaleFactor, PipeindexEngine.RoofRainpipeindex[j]);
                        tag2 = PipeindexEngine.RoofRainpipeindex_tag[j][3 * i + 1]+ PipeindexEngine.RoofRainpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                        tag3 = PipeindexEngine.RoofRainpipeindex_tag[j][3 * i + 2]+ PipeindexEngine.RoofRainpipeindex_tag[j][3 * i].GetVectorTo(tag1);
                    }
                    Circle circle = CreateCircle(PipeindexEngine.RoofRainpipeindex[j][i]);
                    circle.Layer = ThWPipeCommon.W_RAIN_EQPM;
                    parameters0.standardEntity.Add(circle);
                    Line tag_Yline = CreateLine(PipeindexEngine.RoofRainpipeindex[j][i], tag1);
                    tag_Yline.Layer = W_RAIN_NOTE1;
                    parameters0.standardEntity.Add(tag_Yline);
                    parameters0.copyrooftags.Add(tag_Yline);
                    parameters0.normalCopys.Add(tag_Yline);
                    parameters0.normalCopys.Add(circle);
                    Line tag_Xline = CreateLine(tag1, tag2);
                    tag_Xline.Layer= W_RAIN_NOTE1;
                    parameters0.standardEntity.Add(tag_Xline);
                    parameters0.copyrooftags.Add(tag_Xline);
                    parameters0.normalCopys.Add(tag_Xline);
                    DBText taggingtext = ThWPipeOutputFunction.Taggingtext(tag3, $"Y1L{j / 2}-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext.Layer= W_RAIN_NOTE1;
                    DBText taggingtext1 = ThWPipeOutputFunction.Taggingtext(tag3, $"Y1L-{i + 1}", scaleFactor, acadDatabase.Database);
                    taggingtext1.Layer = W_RAIN_NOTE1;
                    DBText taggingtext2 = ThWPipeOutputFunction.Taggingtext(tag3, $"Y1L{j / 2}-{i + 1}‘", scaleFactor, acadDatabase.Database);
                    taggingtext2.Layer = W_RAIN_NOTE1;
                    DBText taggingtext3 = ThWPipeOutputFunction.Taggingtext(tag3, $"Y1L-{i + 1}’", scaleFactor, acadDatabase.Database);
                    taggingtext3.Layer = W_RAIN_NOTE1;
                    if (j == 0)
                    {
                        parameters0.standardEntity.Add(taggingtext1);
                        parameters0.copyrooftags.Add(taggingtext1);
                        parameters0.normalCopys.Add(taggingtext1);
                    }
                    else if (j == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext3);
                        parameters0.copyrooftags.Add(taggingtext3);
                        parameters0.normalCopys.Add(taggingtext3);
                    }
                    else if (j % 2 == 1)
                    {
                        parameters0.standardEntity.Add(taggingtext2);
                        parameters0.copyrooftags.Add(taggingtext2);
                        parameters0.normalCopys.Add(taggingtext2);
                    }
                    else
                    {
                        parameters0.standardEntity.Add(taggingtext);
                        parameters0.copyrooftags.Add(taggingtext);
                        parameters0.normalCopys.Add(taggingtext);
                    }
                    tag_frames.Add(ThWPipeOutputFunction.GetBoundary(175 * 7* scaleFactor, tag3, scaleFactor));
                }
            }
        }
        private static void GetCopiedPipeindex(ThWCompositeFloorRecognitionEngine FloorEngines, ThWTopParameters parameters0, AcadDatabase acadDatabase, ThCADCoreNTSSpatialIndex obstacle,
      ThWRoofParameters parameters1, ThWRoofDeviceParameters parameters2, ThWCompositeIndexEngine composite_Engine,Point3d toiletpoint, Point3d balconypoint,int scaleFactor,string W_RAIN_NOTE1)
        {
            if (FloorEngines.RoofFloors.Count > 0)
            {
                //
                var spacePredicateService = new ThSpaceSpatialPredicateService(FloorEngines.Spaces);

                foreach (var ent in parameters0.copypipes)
                {
                    if (parameters0.baseCenter2.Count > 0)
                    {
                        var offset = Matrix3d.Displacement(parameters0.baseCenter2[0].GetVectorTo(parameters1.baseCenter1[0]));
                        parameters1.roofEntity.Add(ent.GetTransformedCopy(offset));
                        //一定要对屋顶雨水管重排序
                        var PipeindexEngine1 = new ThWInnerPipeIndexEngine();
                        var composite_Engine1 = new ThWCompositeIndexEngine(PipeindexEngine1);
                        List<Line> divideLines1 = new List<Line>();
                        foreach (Line line in parameters0.divideLines)
                        {
                            divideLines1.Add(new Line(line.StartPoint + parameters0.baseCenter2[0].GetVectorTo(parameters1.baseCenter1[0]),
                            line.EndPoint + parameters0.baseCenter2[0].GetVectorTo(parameters1.baseCenter1[0])));
                        }
                        Polyline pboundary1 = null;
                        pboundary1 = FloorEngines.RoofFloors[0].Space.Boundary as Polyline;
                        List<Polyline> noline = new List<Polyline>();
                        composite_Engine1.Run(noline, noline, noline, noline, noline, noline, noline, pboundary1, divideLines1, parameters1.roofRoofRainPipes, toiletpoint, balconypoint, obstacle, scaleFactor);
                        //对顶层屋顶雨水管重新排序
                        for (int j = 0; j < composite_Engine1.PipeEngine.RoofRainpipeindex.Count; j++)
                        {
                            int count = 0;
                            if(j< composite_Engine.PipeEngine.RoofRainpipeindex.Count)
                            {
                                count = composite_Engine.PipeEngine.RoofRainpipeindex[j].Count;
                            }    
                            for (int i = 0; i < composite_Engine1.PipeEngine.RoofRainpipeindex[j].Count; i++)
                            {
                                double Yoffset = 0.0;
                                if (composite_Engine.FpipeDublicated.Count > 0)
                                {
                                    if (j < composite_Engine.FpipeDublicated.Count)
                                    {
                                        Yoffset = ThWPipeOutputFunction.GetOffset(composite_Engine.FpipeDublicated[j], PipeindexEngine1.RoofRainpipeindex[j][i]);
                                    }
                                }
                                Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                                var Matrix = Matrix3d.Displacement(s);
                                var tag1 = PipeindexEngine1.RoofRainpipeindex_tag[j][3 * i].TransformBy(Matrix);
                                var tag2 = PipeindexEngine1.RoofRainpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                                var tag3 = PipeindexEngine1.RoofRainpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                                Circle circle = new Circle
                                {
                                    Center = PipeindexEngine1.RoofRainpipeindex[j][i],
                                    Radius = 50,
                                    Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                                    Layer = ThWPipeCommon.W_RAIN_EQPM
                                };
                                parameters1.roofEntity.Add(circle);
                                Line ent_line = new Line(PipeindexEngine1.RoofRainpipeindex[j][i], tag1);
                                Line ent_line1 = new Line(tag1, tag2);
                                ent_line.Layer = W_RAIN_NOTE1; 
                                ent_line1.Layer = W_RAIN_NOTE1;
                                //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);                            
                                parameters1.roofEntity.Add(ent_line);
                                parameters1.roofEntity.Add(ent_line1);
                                DBText taggingtext = ThWPipeOutputFunction.Taggingtext(tag3, $"Y1L{j / 2}-{i + 1 + count}", scaleFactor, acadDatabase.Database);
                                taggingtext.Layer= W_RAIN_NOTE1;
                                DBText taggingtext1 = ThWPipeOutputFunction.Taggingtext(tag3, $"Y1L-{i + 1 + count}", scaleFactor, acadDatabase.Database);
                                taggingtext1.Layer = W_RAIN_NOTE1;
                                DBText taggingtext2 = ThWPipeOutputFunction.Taggingtext(tag3, $"Y1L{j / 2}-{i + 1 + count}‘", scaleFactor, acadDatabase.Database);
                                taggingtext2.Layer = W_RAIN_NOTE1;
                                DBText taggingtext3 = ThWPipeOutputFunction.Taggingtext(tag3, $"Y1L-{i + 1 + count}’", scaleFactor, acadDatabase.Database);
                                taggingtext3.Layer = W_RAIN_NOTE1;
                                if (j == 0)
                                {
                                    parameters1.roofEntity.Add(taggingtext1);
                                }
                                else if (j == 1)
                                {
                                    parameters1.roofEntity.Add(taggingtext3);
                                }
                                else if (j % 2 == 1)
                                {
                                    parameters1.roofEntity.Add(taggingtext2);
                                }
                                else
                                {
                                    parameters1.roofEntity.Add(taggingtext);
                                }
                            }
                        }
                        if (parameters2.baseCenter0.Count > 0)
                        {
                            var offset1 = Matrix3d.Displacement(parameters0.baseCenter2[0].GetVectorTo(parameters2.baseCenter0[0]));
                            Line s1 = ent as Line;
                            Polyline s2 = ent as Polyline;
                            Circle s3 = ent as Circle;
                            DBText s4 = ent as DBText;
                            foreach (var bound in spacePredicateService.Contains(FloorEngines.RoofTopFloors[0].Space))
                            {
                                Polyline boundary = bound.Boundary as Polyline;
                                if ((s1 != null && GeomUtils.PtInLoop(boundary, s1.StartPoint)) || (s2 != null && GeomUtils.PtInLoop(boundary, s2.StartPoint))
                                    || (s3 != null && GeomUtils.PtInLoop(boundary, s3.Center)) || (s4 != null && GeomUtils.PtInLoop(boundary, s4.Position)))
                                {
                                    parameters2.roofDeviceEntity.Add(ent.GetTransformedCopy(offset1));//管井复制到屋顶设备层
                                }
                            }
                        }
                    }
                }
                //标注顶层雨水管
                foreach (Circle ent in parameters0.copyroofpipes)
                {
                    ent.Layer = ThWPipeCommon.W_RAIN_EQPM;
                    Polyline bucket = ent.Tessellate(50);
                    bucket.Layer = ThWPipeCommon.W_RAIN_EQPM;
                    Point3d center = Point3d.Origin;
                    Point3d center1 = Point3d.Origin;
                    if (parameters0.baseCenter2.Count > 0)
                    {
                        int num = 0;
                        var offset = Matrix3d.Displacement(parameters0.baseCenter2[0].GetVectorTo(parameters1.baseCenter1[0]));
                        center = bucket.GetCenter() + parameters0.baseCenter2[0].GetVectorTo(parameters1.baseCenter1[0]);
                        int s = 0;
                        foreach (var gravitybucket in parameters1.gravityWaterBucket1)
                        {
                            if (gravitybucket.Position.DistanceTo(center) < 2)
                            {
                                s = 1;
                                break;
                            }
                        }
                        Circle alert = new Circle() { Center = center, Radius = 100 };
                        Polyline alertresult = alert.Tessellate(100);
                        alertresult.Layer = ThWPipeCommon.W_RAIN_EQPM;
                        foreach (Point3d bucket_1 in parameters2.waterbuckets2)
                        {
                            var thWPipeOutputFunction = new ThWPipeOutputFunction();
                            if (thWPipeOutputFunction.Checkbucket(center, bucket_1, parameters1.r_boundary))
                            {
                                s += 1;
                                break;
                            }
                        }
                        if (s == 0)
                        {
                            parameters1.roofEntity.Add(ent.GetTransformedCopy(offset));//管井复制到屋顶层                                                         
                        }
                        if (s == 0)
                        {
                            parameters1.roofEntity.Add(alertresult);//生成错误提示    
                        }
                        if (parameters2.baseCenter0.Count > 0)
                        {
                            var offset1 = Matrix3d.Displacement(parameters0.baseCenter2[0].GetVectorTo(parameters2.baseCenter0[0]));
                            foreach (var bound in spacePredicateService.Contains(FloorEngines.RoofTopFloors[0].Space))
                            {
                                Polyline boundary = bound.Boundary as Polyline;
                                if (GeomUtils.PtInLoop(boundary, ent.Center + parameters0.baseCenter2[0].GetVectorTo(parameters2.baseCenter0[0])))
                                {
                                    num = 1;
                                    break;
                                }
                            }
                            if (num == 1)
                            {
                                center1 = bucket.GetCenter() + parameters0.baseCenter2[0].GetVectorTo(parameters2.baseCenter0[0]);
                                int s1 = 0;
                                foreach (var gravitybucket in parameters2.gravityWaterBucket)
                                {
                                    if (gravitybucket.Position.DistanceTo(center1) < 2)
                                    {
                                        s1 = 1;
                                        break;
                                    }
                                }
                                Circle alert1 = new Circle() { Center = center1, Radius = 100 };
                                Polyline alertresult1 = alert1.Tessellate(100);
                                foreach (Point3d bucket_1 in parameters2.waterbuckets1)
                                {
                                    var thWPipeOutputFunction = new ThWPipeOutputFunction();
                                    if (thWPipeOutputFunction.Checkbucket(center1, bucket_1, parameters2.d_boundary))
                                    {
                                        s1 += 1;
                                        break;
                                    }
                                }
                                if (s1 == 0)
                                {
                                    parameters2.roofDeviceEntity.Add(ent.GetTransformedCopy(offset1));//管井复制到屋顶设备层                                                                  
                                }
                                if (s1 == 0)
                                {
                                    parameters2.roofDeviceEntity.Add(alertresult1);//生成错误提示
                                }
                            }
                        }
                    }
                }
                //标注顶层雨水管标注
                for (int i = 0; i < parameters0.copyrooftags.Count; i += 3)
                {
                    Line bucket = parameters0.copyrooftags[i] as Line;
                    Point3d center = Point3d.Origin;
                    Point3d center1 = Point3d.Origin;
                    int num = 0;
                    if (parameters0.baseCenter2.Count > 0)
                    {
                        var offset = Matrix3d.Displacement(parameters0.baseCenter2[0].GetVectorTo(parameters1.baseCenter1[0]));
                        center = bucket.StartPoint + parameters0.baseCenter2[0].GetVectorTo(parameters1.baseCenter1[0]);
                        int s = 0;
                        foreach (var gravitybucket in parameters1.gravityWaterBucket1)
                        {
                            if (gravitybucket.Position.DistanceTo(center) < 2)
                            {
                                s = 1;
                                break;
                            }
                        }
                        Circle alert = new Circle() { Center = center, Radius = 100 };
                        Polyline alertresult = alert.Tessellate(100);
                        foreach (Point3d bucket_1 in parameters2.waterbuckets2)
                        {
                            var thWPipeOutputFunction = new ThWPipeOutputFunction();
                            if (thWPipeOutputFunction.Checkbucket(center, bucket_1, parameters1.r_boundary))
                            {
                                ++s;
                                break;
                            }
                        }
                        if (s == 0)
                        {
                            parameters1.roofEntity.Add(parameters0.copyrooftags[i].GetTransformedCopy(offset));//管井复制到屋顶层  
                            parameters1.roofEntity.Add(parameters0.copyrooftags[i + 1].GetTransformedCopy(offset));
                            parameters1.roofEntity.Add(parameters0.copyrooftags[i + 2].GetTransformedCopy(offset));
                        }
                        if (s == 0)
                        {
                            parameters1.roofEntity.Add(alertresult);//生成错误提示    
                        }
                        if (parameters2.baseCenter0.Count > 0)
                        {
                            var offset1 = Matrix3d.Displacement(parameters0.baseCenter2[0].GetVectorTo(parameters2.baseCenter0[0]));
                            foreach (var bound in spacePredicateService.Contains(FloorEngines.RoofTopFloors[0].Space))
                            {
                                Polyline boundary = bound.Boundary as Polyline;
                                if (GeomUtils.PtInLoop(boundary, bucket.StartPoint + parameters0.baseCenter2[0].GetVectorTo(parameters2.baseCenter0[0])))
                                {
                                    num = 1;
                                    break;
                                }
                            }
                            if (num == 1)
                            {
                                center1 = bucket.StartPoint + parameters0.baseCenter2[0].GetVectorTo(parameters2.baseCenter0[0]);
                                int s1 = 0;
                                foreach (var gravitybucket in parameters2.gravityWaterBucket)
                                {
                                    if (gravitybucket.Position.DistanceTo(center1) < 2)
                                    {
                                        s1 = 1;
                                        break;
                                    }
                                }
                                Circle alert1 = new Circle() { Center = center1, Radius = 100 };
                                Polyline alertresult1 = alert1.Tessellate(100);
                                foreach (Point3d bucket_1 in parameters2.waterbuckets1)
                                {
                                    var thWPipeOutputFunction = new ThWPipeOutputFunction();
                                    if (thWPipeOutputFunction.Checkbucket(center1, bucket_1, parameters2.d_boundary))
                                    {
                                        ++s1;
                                        break;
                                    }
                                }
                                if (s1 == 0)
                                {
                                    parameters2.roofDeviceEntity.Add(parameters0.copyrooftags[i].GetTransformedCopy(offset1));//管井复制到屋顶设备层 
                                    parameters2.roofDeviceEntity.Add(parameters0.copyrooftags[i + 1].GetTransformedCopy(offset1));
                                    parameters2.roofDeviceEntity.Add(parameters0.copyrooftags[i + 2].GetTransformedCopy(offset1));
                                }
                                if (s1 == 0)
                                {
                                    parameters2.roofDeviceEntity.Add(alertresult1);//生成错误提示
                                }
                            }
                        }
                    }
                }                                                   
                if (FloorEngines.NormalFloors.Count > 0)//复制所有管井到标准层
                {
                    for (int i = 0; i < FloorEngines.NormalFloors.Count; i++)
                    {                       
                        var normalEntity = new List<Entity>();
                        var offset = Matrix3d.Displacement(parameters0.baseCenter2[0].GetVectorTo(FloorEngines.NormalFloors[0].BaseCircles[i + 1].Boundary.GetCenter()));
                        foreach (var ent in parameters0.normalCopys)
                        {
                            normalEntity.Add(ent.GetTransformedCopy(offset));
                        }
                        var record3 = BlockTools.AddBlockTableRecord(acadDatabase.Database, $"标准层{FloorEngines.NormalFloors[i].Space.Tags[0]}", normalEntity);
                        BlockReference normalBlock = new BlockReference(new Point3d(0, 0, 0), record3);
                        acadDatabase.ModelSpace.Add(normalBlock);//管井复制到标准层
                    }    
                }
            }
        }
    }
}
