using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPWSS.DrainageSystemDiagram.Service;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThToiletRoom
    {
        public Polyline outline { get; private set; }
        public List<Line> wallList { get; private set; }
        public List<ThTerminalToilet> toilet { get; private set; }
        public string name { get; private set; }
        public int type { get; private set; } //1:大空间 0:小空间 -1：没有厕所

        private List<string> typeName = new List<string>() { "工具间", "清洁间", "第三卫", "无障碍卫", "儿童" };
        public List<Point3d> outlinePtList { get; private set; }
        public ThToiletRoom(Polyline outline, string name, List<ThTerminalToilet> toilet)
        {
            this.outline = outline;
            this.name = name;
            this.toilet = toilet;

            outlinePtList = ThDrainageSDCommonService.getPT(this.outline);
            wallList = buildRoomWall();
            type = isLargeRoom();
        }

        private List<Line> buildRoomWall()
        {
            var buildWall = new List<Line>();
            for (int j = 0; j < outline.NumberOfVertices; j++)
            {
                var pt = outline.GetPoint3dAt(j % outline.NumberOfVertices);
                var ptNext = outline.GetPoint3dAt((j + 1) % outline.NumberOfVertices);

                var roomLine = new Line(pt, ptNext);
                buildWall.Add(roomLine);
            }

            return buildWall;

        }

        private int isLargeRoom()
        {
            int t = -2;

            if (t == -2 && toilet.Count == 0)
            {
                t = -1;
            }
            var typeList = typeName.Where(x => name.Contains(x));
            if (t == -2 && typeList.Count() > 0)
            {
                t = 0;
            }
            if (t == -2 && outline.Area < 7 * 1000 * 1000)
            {
                t = 0;
            }
            if (t == -2)
            {
                t = 1;
            }
            return t;
        }
    }
}
