using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.Model;
using ThCADCore.NTS;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThToilateRoom
    {
        public Polyline outline { get; private set; }
        public List<Line> wallList { get; private set; }
        public List<ThTerminalToilate> toilate { get; private set; }
        public string name { get; private set; }
        public int type { get; private set; } //1:大空间 0:小空间 -1：没有厕所

        private List<string> typeName = new List<string>() { "工具间", "清洁间", "第三卫", "无障碍卫","儿童" };
        public List<Point3d> outlinePtList { get; private set; }
        public ThToilateRoom(Polyline outline, string name, List<ThTerminalToilate> tolilate)
        {
            this.outline = outline;
            this.name = name;
            this.toilate = tolilate;

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

            if (t == -2 && toilate.Count == 0)
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
