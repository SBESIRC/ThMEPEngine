﻿using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThMEPWSS.Pipe;
using Dreambuild.AutoCAD;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Engine;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using ThMEPWSS.Pipe.Layout;
using ThMEPWSS.Pipe.Output;
using ThMEPWSS.Pipe.Tools;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS
{
    public class ThPipeCmds
    { 
        public class ThWRoofDeviceParameters
        {
            public Polyline d_boundary = null;
            public List<BlockReference> gravityWaterBucket = new List<BlockReference>();
            public List<BlockReference> sideWaterBucket = new List<BlockReference>();
            public List<Polyline> roofRainPipe = new List<Polyline>();
            public ThWWaterBucketEngine engine = new ThWWaterBucketEngine();
            public List<Polyline> baseCircles = new List<Polyline>();
            public Point3dCollection baseCenter0 = new Point3dCollection();
            public Point3dCollection waterbuckets1 = new Point3dCollection();
            public Point3dCollection waterbuckets2 = new Point3dCollection();
            public List<Entity> roofDeviceEntity = new List<Entity>();
        }
        public class ThWRoofParameters
        {
            public Polyline r_boundary = null;
            public List<BlockReference> gravityWaterBucket1 = new List<BlockReference>();
            public List<BlockReference> sideWaterBucket1 = new List<BlockReference>();
            public List<Polyline> roofRainPipe1 = new List<Polyline>();
            public ThWWaterBucketEngine engine1 = new ThWWaterBucketEngine();
            public Point3dCollection baseCenter1 = new Point3dCollection();
            public List<Polyline> roofRoofRainPipes = new List<Polyline>();
            public List<Entity> roofEntity=new List<Entity>();
        }
        public class ThWTopParameters
        {
            public List<BlockReference> tfloordrain = new List<BlockReference>();
            public Point3dCollection baseCenter2 = new Point3dCollection();
            public List<Entity> copypipes = new List<Entity>();//要复制的特征
            public List<Entity> copyroofpipes = new List<Entity>();//要复制的屋顶雨水管
            public List<Entity> copyrooftags = new List<Entity>();
            public List<Entity> normalCopys = new List<Entity>();//要复制到其他标准层的立管                                                         //标注变量
            public List<Polyline> fpipe = new List<Polyline>();
            public List<Polyline> tpipe = new List<Polyline>();
            public List<Polyline> wpipe = new List<Polyline>();
            public List<Polyline> ppipe = new List<Polyline>();
            public List<Polyline> dpipe = new List<Polyline>();
            public List<Polyline> npipe = new List<Polyline>();
            public List<Polyline> rain_pipe = new List<Polyline>();
            public Polyline pboundary = null;
            public List<Line> divideLines = new List<Line>();
            public List<Polyline> roofrain_pipe = new List<Polyline>();
            public List<Entity> standardEntity = new List<Entity>();
        }
        public class ThWTopCompositeParameters
        {
            public List<BlockReference> tfloordrain_ = new List<BlockReference>();
            public Polyline boundary = null;
            public Polyline outline = null;
            public BlockReference basinline = null;
            public Polyline pype = null;
            public Polyline boundary1 = null;
            public Polyline outline1 = null;
            public Polyline closestool = null;
            public BlockReference floordrain = null;
        }
        public class ThWTopBalconyParameters
        {
            public Polyline roofrainpipe = null;
            public Polyline tboundary = null;
            public Polyline bboundary = null;
            public Polyline downspout = null;
            public BlockReference washingmachine = null;
            public Polyline device = null;
            public Polyline condensepipe = null;
            public Polyline device_other = null;
            public BlockReference floordrain = null;
            public BlockReference bbasinline = null;
            public List<Polyline> condensepipes = new List<Polyline>();
            public List<BlockReference> bfloordrain = new List<BlockReference>();
            public List<BlockReference> devicefloordrain = new List<BlockReference>();
            public List<Polyline> rainpipe = new List<Polyline>();
        }
        public static Line CreateLine(Point3d point1, Point3d point2)
        {
            Line line = new Line(point1, point2);  
            return line;
        }
        public static List<Line> GetCreateLines(Point3dCollection points, Point3dCollection point1s,string W_RAIN_NOTE1)
        {
            var lines = new List<Line>();
            for (int i = 0; i < points.Count; i++)
            {
                Line s = CreateLine(points[i], point1s[4 * i]);
                s.Layer = W_RAIN_NOTE1;
                lines.Add(s);              
            }
            return lines;
        }
        public static List<Line> GetCreateLines1(Point3dCollection points, Point3dCollection point1s, string W_RAIN_NOTE1)
        {
            var lines = new List<Line>();
            for (int i = 0; i < points.Count; i++)
            {
                Line s = CreateLine(point1s[4 * i], point1s[4 * i + 1]);
                s.Layer = W_RAIN_NOTE1;
                lines.Add(s);
            }
            return lines;
        }
        public static Circle CreateCircle(Point3d point1)
        {
            return new Circle()
            {
                Radius = 50,
                Center = point1,
                Layer = ThWPipeCommon.W_RAIN_EQPM,
            };
        }                    
        public class InputObstacles
        {
            public List<Curve> ObstacleParameters = new List<Curve>();
            public void Recognize(ThWCompositeFloorRecognitionEngine FloorEngines)
            {
                var inputInfo = new InputObstacles();
                ObstacleParameters = FloorEngines.AllObstacles;              
            }
            public void Do(ThWCompositeFloorRecognitionEngine FloorEngines)
            {
                var obstacle_key = new PromptKeywordOptions("\n障碍物");
                obstacle_key.Keywords.Add("有", "Y", "有(Y)");
                obstacle_key.Keywords.Add("没有", "N", "没有(N)");
                obstacle_key.Keywords.Default = "没有";
                var result = Active.Editor.GetKeywords(obstacle_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                if (result.StringResult == "有")
                {
                    var obstacleParameters_key = new PromptKeywordOptions("\n障碍物选择");
                    obstacleParameters_key.Keywords.Add("全部", "Y", "全部(Y)");
                    obstacleParameters_key.Keywords.Add("非全部", "N", "非全部(N)");
                    result = Active.Editor.GetKeywords(obstacleParameters_key);
                    if (result.StringResult == "全部")
                    {
                        ObstacleParameters = GetObstacleParameters("全部障碍物",FloorEngines.AllObstacles);
                    }
                    else
                    {
                        GetObstacleParameters("空间名称",FloorEngines.TagNameFrames).ForEach(o=> ObstacleParameters.Add(o));
                        GetObstacleParameters("楼梯", FloorEngines.StairFrames).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("结构柱", FloorEngines.Columns).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("墙", FloorEngines.Walls).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("门", FloorEngines.Doors).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("窗", FloorEngines.Windows).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("设备", FloorEngines.Devices).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("建筑标高", FloorEngines.ElevationFrames).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("轴向圆圈标注", FloorEngines.AxialCircleTags).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("轴向横线标注", FloorEngines.AxialAxisTags).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("外部尺寸标注", FloorEngines.ExternalTags).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("本图管井", FloorEngines.Wells).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("本图标注", FloorEngines.DimensionTags).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("本图水管", FloorEngines.RainPipes).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("本图定位尺寸", FloorEngines.PositionTags).ForEach(o => ObstacleParameters.Add(o));
                    }
                }
            }

        }
        public static List<Curve> GetObstacleParameters(string s,List<Curve> curves)
        {
            var obstacle_key = new PromptKeywordOptions(s);
            obstacle_key.Keywords.Add("有", "Y", "有(Y)");
            obstacle_key.Keywords.Add("没有", "N", "没有(N)");
            var result = Active.Editor.GetKeywords(obstacle_key);
            if(result.StringResult == "有")
            {
                return curves;
            }
            return new List<Curve>();
        }
      
        [CommandMethod("TIANHUACAD", "THLGBZ", CommandFlags.Modal)]
        public void THLGBZ()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var FloorEngines = new ThWCompositeFloorRecognitionEngine())
            {                 
                FloorEngines.Recognize(acadDatabase.Database, new Point3dCollection());
                if (FloorEngines.RoofTopFloors.Count == 0 && FloorEngines.RoofFloors.Count == 0 && FloorEngines.TopFloors.Count == 0)
                {
                    return;
                }
                string W_RAIN_NOTE1 = ThWPipeOutputFunction.Get_Layers1(FloorEngines.Layers, ThWPipeCommon.W_RAIN_NOTE);
                string W_DRAI_EQPM= ThWPipeOutputFunction.Get_Layers2(FloorEngines.Layers, ThWPipeCommon.W_DRAI_EQPM);
                string W_DRAI_FLDR = ThWPipeOutputFunction.Get_Layers3(FloorEngines.Layers, ThWPipeCommon.W_DRAI_FLDR);
                string W_RAIN_PIPE= ThWPipeOutputFunction.Get_Layers4(FloorEngines.Layers, ThWPipeCommon.W_RAIN_PIPE);
                //第一类屋顶设备层布置   
                var parameters2 = new ThWRoofDeviceParameters();
                if (FloorEngines.RoofTopFloors.Count > 0)//存在屋顶设备层
                {
                    ThWLayoutRoofDeviceFloorEngine.LayoutRoofDeviceFloor(FloorEngines, parameters2, acadDatabase, ThTagParametersService.ScaleFactor, W_RAIN_NOTE1);
                }
                //第二类屋顶层布置
                var parameters1 = new ThWRoofParameters();
                if (FloorEngines.RoofFloors.Count > 0)//存在屋顶层
                {
                    ThWRoofFloorOutPutEngine.LayoutRoofFloor(FloorEngines, parameters2, parameters1, acadDatabase, ThTagParametersService.ScaleFactor, W_RAIN_NOTE1);
                }
                //第三类顶层布置   
                var parameters0 = new ThWTopParameters();
                if (FloorEngines.TopFloors.Count > 0) //存在顶层
                {
                    var basecircle2 = FloorEngines.TopFloors[0].BaseCircles[0].Boundary.GetCenter();                    
                    parameters0.baseCenter2.Add(basecircle2);
                    var layoutTopFloor = new ThWTopFloorOutPutEngine();
                    layoutTopFloor.LayoutTopFloor(FloorEngines, parameters0, acadDatabase, W_DRAI_EQPM, W_DRAI_FLDR, W_RAIN_PIPE);
                }
                var PipeindexEngine = new ThWInnerPipeIndexEngine();
                var composite_Engine = new ThWCompositeIndexEngine(PipeindexEngine);
                //开始标注 
                var layoutTag = new ThWCompositeTagOutPutEngine();
                layoutTag.LayoutTag(FloorEngines, parameters0, parameters1, parameters2,acadDatabase, PipeindexEngine,composite_Engine, FloorEngines.AllObstacles, ThTagParametersService.ScaleFactor, ThTagParametersService.PipeLayer, W_DRAI_EQPM, W_RAIN_NOTE1);               
            }
        }
        [CommandMethod("TIANHUACAD", "THLGLC", CommandFlags.Modal)]
        public void THLGLC()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptPointResult result;
                var tpipe = new List<Point3d>();
                do
                {
                    result = Active.Editor.GetPoint("\n选择要插入的基点位置");
                    if (result.Status == PromptStatus.OK)
                    {
                        tpipe.Add(result.Value);
                    }
                } while (result.Status == PromptStatus.OK);
                ThInsertStoreyFrameService.Insert(tpipe);
            }
        }
        [CommandMethod("TIANHUACAD", "THLGYY", CommandFlags.Modal)]
        public static void THLGYY()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {              
                if(!(GetBlockReferences(acadDatabase.Database, ThTagParametersService.sourceFloor).Count>0))
                {
                    PromptPointOptions sf = new PromptPointOptions("\n 来源楼层中没有立管图块");
                    return;
                }
                else
                {
                    var application = new ThTagParametersService();
                    application.Read();
                    ThApplicationPipesEngine.Application(ThTagParametersService.sourceFloor, ThTagParametersService.targetFloors);
                }
            }
        }
        private static  List<BlockReference> GetBlockReferences(Database db, string blockName)
        {
            List<BlockReference> blocks = new List<BlockReference>();
            var trans = db.TransactionManager;
            BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);                
            blocks = (from b in db.GetEntsInDatabase<BlockReference>()
                      where (b.GetBlockName().Contains(blockName)&& b.GetBlockName().Contains("标准层"))
                      select b).ToList();
            return blocks;
        }
    }
}
