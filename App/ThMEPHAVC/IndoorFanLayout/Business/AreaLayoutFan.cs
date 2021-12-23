using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPHVAC.IndoorFanLayout.Models;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class AreaLayoutFan
    {
        RectAreaLayoutFan rectangleAreaLayout;
        ArcAreaLayoutFan arcAreaLayout;
        public ArcAreaLayoutFanByVertical arcAreaLayoutFanByVertical;
        List<AreaLayoutGroup> _allGroups;
        public AreaLayoutFan(Dictionary<string, List<string>> divisionAreaNearIds, Vector3d xAxis, Vector3d yAxis) 
        {
            rectangleAreaLayout = new RectAreaLayoutFan(divisionAreaNearIds,xAxis,yAxis);
            arcAreaLayout = new ArcAreaLayoutFan(divisionAreaNearIds, xAxis, yAxis);
            arcAreaLayoutFanByVertical = new ArcAreaLayoutFanByVertical(divisionAreaNearIds, xAxis, yAxis);
            _allGroups = new List<AreaLayoutGroup>();
        }
        public void InitRoomData(List<AreaLayoutGroup> layoutAreas, Polyline roomOutPLine, List<Polyline> innerPLines, double roomLoad)
        {
            _allGroups.Clear();
            foreach (var item in layoutAreas)
                _allGroups.Add(item);
            rectangleAreaLayout.InitRoomData(roomOutPLine, innerPLines, roomLoad);
            arcAreaLayout.InitRoomData(roomOutPLine, innerPLines, roomLoad);
            arcAreaLayoutFanByVertical.InitRoomData(roomOutPLine, innerPLines, roomLoad);
        }
        public List<DivisionRoomArea> GetLayoutFanResult(FanRectangle fanRectangle) 
        {
            ClearHisData();
            var layoutRes =new List<DivisionRoomArea>();
            foreach (var group in _allGroups) 
            {

                if (group.IsArcGroup)
                {
                    var rectRes = new List<DivisionRoomArea>();
                    if (!group.ArcVertical)
                        rectRes = arcAreaLayout.GetRectangle(group, fanRectangle);
                    else
                        rectRes = arcAreaLayoutFanByVertical.GetRectangle(group, fanRectangle);
                    layoutRes.AddRange(rectRes);
                }
                else 
                {
                    var rectRes = rectangleAreaLayout.GetRectangle(group, fanRectangle);
                    layoutRes.AddRange(rectRes);
                }
            }
            return layoutRes;
        }
        void ClearHisData() 
        {
            foreach (var group in _allGroups) 
            {
                foreach (var layoutArea in group.GroupDivisionAreas) 
                {
                    layoutArea.FanLayoutAreaResult.Clear();
                    layoutArea.NeedFanCount = 0;
                }
            }
        }
    }
}
