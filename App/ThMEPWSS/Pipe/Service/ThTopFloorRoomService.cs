using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThTopFloorRoomService 
    {
        private List<ThIfcRoom> Spaces { get; set; }
        private List<ThIfcRoom> BaseCircles { get; set; }
        private List<ThWCompositeRoom> CompositeRooms { get; set; }
        private List<ThWCompositeBalconyRoom> CompositeBalconyRooms { get; set; }
        private List<Line> DivisionLines { get; set; }      
        public List<ThWTopFloorRoom> Rooms { get; private set; }

        private ThTopFloorRoomService(
            List<ThIfcRoom> spaces,
            List<ThIfcRoom> baseCircles,          
            List<ThWCompositeRoom> compositeRooms,
            List<ThWCompositeBalconyRoom> compositeBalconyRooms,
            List<Line> divisionLines)
        {
            Spaces = spaces;
            BaseCircles = baseCircles;
            CompositeRooms = compositeRooms;           
            CompositeBalconyRooms = compositeBalconyRooms;
            DivisionLines = divisionLines;
            Rooms = new List<ThWTopFloorRoom>();
        }
        public static List<ThWTopFloorRoom> Build(List<ThIfcRoom> spaces, List<ThIfcRoom> baseCircles, List<ThWCompositeRoom> compositeRooms, List<ThWCompositeBalconyRoom> compositeBalconyRooms, List<Line> divisionLines)
        {
            var firstFloorContainerService = new ThTopFloorRoomService(spaces, baseCircles, compositeRooms, compositeBalconyRooms, divisionLines);
            firstFloorContainerService.Build();
            return firstFloorContainerService.Rooms;            
        }
        public void Dispose()
        {
        }
        private void Build()
        {
            //找主体空间 空间框线包含“顶层设备空间”
            var firstFloorSpaces = GetFirstFloorSpace();
            firstFloorSpaces.ForEach(o =>
            {
                Rooms.Add(CreateFirstFloorContainer(o));
            });
        }
        private ThWTopFloorRoom CreateFirstFloorContainer(ThIfcRoom firstFloorSpace)
        {
            return new ThWTopFloorRoom()
            {                
                BaseCircles = BaseCircles,
                Boundary = firstFloorSpace.Boundary,
                CompositeRooms = ThTopFloorCompositeRoomService.Find(firstFloorSpace, CompositeRooms),
                CompositeBalconyRooms = ThTopFloorCompositeBalconyRoomService.Find(firstFloorSpace, CompositeBalconyRooms),
                DivisionLines = ThTopFloorDivisionLineService.Find(firstFloorSpace, DivisionLines),
            };
        }
        private List<ThIfcRoom> GetFirstFloorSpace()
        {
            var FirSpace = new List<ThIfcRoom>();
            string pattern = @"\d+$";
            string pattern1 = @"^\d";
            var spaces = new List<Tuple<ThIfcRoom, double>>();
            Spaces.ForEach(m =>
            {
                m.Tags.ForEach(n =>
                {
                    var match = Regex.Match(n, pattern);
                    var match_start = Regex.Match(n, pattern1);
                    if (!string.IsNullOrEmpty(match.Value) && (!string.IsNullOrEmpty(match_start.Value) || n.StartsWith("B")))
                    {
                        if (!n.Contains(":"))
                        {
                            spaces.Add(Tuple.Create(m, double.Parse(match.Value)));
                        }
                    }
                });
            });
            if (spaces.Count > 0)
            {
                FirSpace.Add(spaces.OrderByDescending(o => o.Item2).First().Item1);
                foreach (var space in spaces.OrderByDescending(o => o.Item2))
                {
                    if (space.Item1 != FirSpace[0])
                    {
                        FirSpace.Add(space.Item1);
                    }
                }
            }
            return FirSpace;
        }
    }
}
