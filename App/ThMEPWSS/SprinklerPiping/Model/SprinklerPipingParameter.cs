using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.SprinklerConnect.Model;

namespace ThMEPWSS.SprinklerPiping.Model
{
    public class SprinklerPipingParameter
    {
        public Polyline frame;
        public List<SprinklerPoint> sprinklerPoints = new List<SprinklerPoint>();

        public Dictionary<Point3d, bool> assignList = new Dictionary<Point3d, bool>();
        public List<Point3d> pts = new List<Point3d>();
        public double dttol;
        //public List<Polyline> rooms = new List<Polyline>();
        //public List<Polyline> walls = new List<Polyline>();
        //public List<Polyline> shearWalls = new List<Polyline>();

        //墙线信息
        public ThSprinklerDataQueryService dataQuery; //ArchitectureWallList, ShearWallList, ColumnList, RoomList

        //支干管到喷淋点的最小间距
        public double minSpace = 300;
        //车位长度
        public double parkingLength = 6000;
        //小房间的面积阈值
        public double roomArea = 150000000;

        public Point3d startPoint;
        public Point3d endPoint;
        public Vector3d startDirection;
        public int startLen = 200;

        //车位排
        public List<Polyline> parkingRows = new List<Polyline>();
        public bool isParallel = false;
        public List<SprinklerParkingRow> sprinklerParkingRows = new List<SprinklerParkingRow>();

        public List<ThSprinklerNetGroup> netList = new List<ThSprinklerNetGroup>();

        public double explorationConstant = 1 / Math.Sqrt(2);
        public int initWeight = 10;

        public int timeLimit = -1;
        public int iterLimit = 300;
        public int searchLimit = 200;

        public int parkingCnt = 0;

        public Dictionary<Point3d, SprinklerPoint> ptDic = new Dictionary<Point3d, SprinklerPoint>();
    }
}
