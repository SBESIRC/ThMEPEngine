using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.ParkingGroup
{
    /// <summary>
    /// 车位分组group 的生成器 入口
    /// </summary>
    public class ParkingGroupGenerator
    {
        private List<Polyline> m_polylines;

        public List<ParkingRelatedGroup> ParkingGroups
        {
            get;
            set;
        } = new List<ParkingRelatedGroup>();


        public static List<ParkingRelatedGroup> MakeParkingGroupGenerator(List<Polyline> polylines)
        {
            var parkingGenerator = new ParkingGroupGenerator(polylines);
            parkingGenerator.DoGroup();
            return parkingGenerator.ParkingGroups;
        }

        public ParkingGroupGenerator(List<Polyline> polylines)
        {
            m_polylines = polylines;
        }

        public void DoGroup()
        {
            // 原始的相连车位
            var nearParksLst = ParkingNearGroup.MakeParkingNearGroup(m_polylines);
            //foreach (var nearParks in nearParksLst)
            //    DrawUtils.DrawProfileDebug(nearParks.Polylines.Polylines2Curves(), "nearParksGroup");


            // polylineNodes infos
            var nearParksPolylineNodeLst = ParkingPolyTrans.MakeParkingPolyTrans2LineSegment(nearParksLst);

            // 一组车位信息
            ParkingGroups = NearParksPolylineNodeCalculator.MakeParkingRelatedGroup(nearParksPolylineNodeLst);
        }
    }
}
