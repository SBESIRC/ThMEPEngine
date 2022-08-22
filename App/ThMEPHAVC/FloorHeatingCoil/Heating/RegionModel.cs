using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class RegionModel
    {
    }

    class SingleRegion
    {
        //自身属性
        public int RegionId = -1;
        public Polyline OriginalPl = new Polyline();
        public double SuggestDist = 200;
        public Polyline ClearedPl = new Polyline();
        public double UsedPipeLength = 0;
        
        //固定类型判别
        public int RegionType = -1;    //0:过道     1:附属房间    2:大房间;
        public int IsPublicRegion = 0; //0:非公区   1:小的公区    2:公区;

        //可变类型判别
        public int IsDeadRoom = 0; //是否是要单独连一根的房间

        //拓扑属性
        public int Level = -1;
        public List<SingleRegion> FatherRegion = new List<SingleRegion>();
        public List<SingleRegion> ChildRegion = new List<SingleRegion>();
        public Dictionary<SingleRegion, SingleDoor> EntranceMap = new Dictionary<SingleRegion, SingleDoor>();
        public Dictionary<SingleRegion, SingleDoor> ExportMap = new Dictionary<SingleRegion, SingleDoor>();
        public SingleDoor MainEntrance ;


        //管道属性
        public List<int> PassingPipeList = new List<int>();
        public List<int> MainPipe = new List<int>();

        public SingleRegion(int regionId, Polyline originalPl,double suggestDist)
        {
            RegionId = regionId;
            OriginalPl = originalPl;
            SuggestDist = suggestDist;
        }

        //public SingleRegion ShallowCopy()
        //{
        //    return (SingleRegion) this.MemberwiseClone();
        //}

    }

    class SingleDoor
    {
        //
        public int DoorId = -1;
        public Polyline OriginalPl = new Polyline();
        //public Polyline ClearedPl = new Polyline();
        public Vector3d FlowDir = new Vector3d(0, 0, 0);
        public int DoorType = -1;         //0：正常门  1：分割线门

        //
        public SingleRegion UpstreamRegion;
        public SingleRegion DownstreamRegion;

        //
        public int UpLineIndex = -1;
        public Point3d UpFirst = new Point3d();
        public Point3d UpSecond = new Point3d();

        public int DownLineIndex = -1;
        public Point3d DownFirst = new Point3d();
        public Point3d DownSecond = new Point3d();

        //逆时针
        public List<int> PipeIdList = new List<int>();

        //DoorPolyline
        public Point3d Center = new Point3d();
        public Vector3d LongSide = new Vector3d();
        public Vector3d ShortSide = new Vector3d();
        public Vector3d ShortDir = new Vector3d();

        //拓扑相关信息
        public double CCWDistance = 0;
        public double CWDistance = 0;
        public int DoorNum = 0;
        public int LeftDoorNum = 0;

        public SingleDoor(int doorId, SingleRegion up, SingleRegion down,Polyline originalPl,int doorType) 
        {
            DoorId = doorId;
            UpstreamRegion = up;
            DownstreamRegion = down;
            OriginalPl = originalPl;
            DoorType = doorType;
        }

        public void SetRecInfo() 
        {
            PolylineProcessService.GetRecInfo(OriginalPl, ref Center, ref ShortDir, ref LongSide, ref ShortSide);
        }
    }



    class SinglePipe
    {
        public int PipeId = -1;
        public double TotalLength = -1;
        public double Independent = 0;

        //
        public List<int> DoorList = new List<int>();

        public List<int> PassedRegionList = new List<int>();
        public List<int> DomaintRegionList = new List<int>();

        //逆时针
        Dictionary<SingleDoor, List<Point3d>> EntrancePointMap = new Dictionary<SingleDoor, List<Point3d>>();
        Dictionary<SingleDoor, List<Point3d>> ExportPointMap = new Dictionary<SingleDoor, List<Point3d>>();

        public SinglePipe(int index)
        {
            PipeId = index;
        }
    }
}
