using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.FloorHeatingCoil.Heating;

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

        //拓扑属性
        public int Level = -1;
        public List<SingleRegion> FatherRegion = new List<SingleRegion>();
        public List<SingleRegion> ChildRegion = new List<SingleRegion>();
        public Dictionary<SingleRegion, SingleDoor> EntranceMap = new Dictionary<SingleRegion, SingleDoor>();
        public Dictionary<SingleRegion, SingleDoor> ExportMap = new Dictionary<SingleRegion, SingleDoor>();
        public SingleDoor MainEntrance;

        //管道属性
        public List<int> PassingPipeList = new List<int>();
        public List<int> MainPipe = new List<int>();

        public SingleRegion(int regionId, Polyline originalPl,double suggestDist)
        {
            RegionId = regionId;
            OriginalPl = originalPl;
            SuggestDist = suggestDist;
        }

        public SingleRegion ShallowCopy()
        {
            return (SingleRegion) this.MemberwiseClone();
        }

    }

    class SingleDoor
    {
        //
        public int DoorId = -1;
        public Polyline OriginalPl = new Polyline();
        //public Polyline ClearedPl = new Polyline();
        public Vector3d FlowDir = new Vector3d(0, 0, 0);

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

        public SingleDoor(int doorId, SingleRegion up, SingleRegion down,Polyline originalPl) 
        {
            DoorId = doorId;
            UpstreamRegion = up;
            DownstreamRegion = down;
            OriginalPl = originalPl;
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
