using System;
using Linq2Acad;
using AcHelper.Commands;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Layout;
using ThMEPWSS.Pipe.Output;
using ThMEPWSS.Pipe.Tools;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Command
{
    public class ThPipeCreateCmd : IAcadCommand, IDisposable
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
            public List<Entity> roofEntity = new List<Entity>();
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
        public void Dispose()
        {
            //
        }
        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var FloorEngines = new ThWCompositeFloorRecognitionEngine())
            {
                FloorEngines.Recognize(acadDatabase.Database, ThTagParametersService.framePoints);
                if (FloorEngines.RoofTopFloors.Count == 0 && FloorEngines.RoofFloors.Count == 0 && FloorEngines.TopFloors.Count == 0)
                {
                    return;
                }
                string W_RAIN_NOTE1 = ThWPipeOutputFunction.Get_Layers1(FloorEngines.Layers, ThWPipeCommon.W_RAIN_NOTE);
                string W_DRAI_EQPM = ThWPipeOutputFunction.Get_Layers2(FloorEngines.Layers, ThWPipeCommon.W_DRAI_EQPM);
                string W_DRAI_FLDR = ThWPipeOutputFunction.Get_Layers3(FloorEngines.Layers, ThWPipeCommon.W_DRAI_FLDR);
                string W_RAIN_PIPE = ThWPipeOutputFunction.Get_Layers4(FloorEngines.Layers, ThWPipeCommon.W_RAIN_PIPE);
                ThWPipeCommon.W_RAIN_EQPM = ThWPipeOutputFunction.Get_Layers5(FloorEngines.Layers, ThWPipeCommon.W_RAIN_EQPM);
                ThWPipeCommon.W_DRAI_NOTE = ThWPipeOutputFunction.Get_Layers6(FloorEngines.Layers, ThWPipeCommon.W_DRAI_NOTE);
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
                layoutTag.LayoutTag(FloorEngines, parameters0, parameters1, parameters2, acadDatabase, PipeindexEngine, composite_Engine, FloorEngines.AllObstacles, ThTagParametersService.ScaleFactor, ThTagParametersService.PipeLayer, W_DRAI_EQPM, W_RAIN_NOTE1);
            }
        }
    }
}
