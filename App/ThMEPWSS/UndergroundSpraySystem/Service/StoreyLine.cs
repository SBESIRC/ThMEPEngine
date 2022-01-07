using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundSpraySystem.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThCADCore.NTS;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class StoreyLine
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            var floors = sprayIn.FloorRectDic;
            double length = sprayIn.FloorLength;
            double height = sprayIn.FloorHeight;
            Point3d insertPt = sprayOut.InsertPoint.Cloned();
            Point3d loopStPt = sprayIn.LoopStartPt._pt;
            floors.OrderByDescending(e => e.Key);

            foreach(var fNumber in floors.Keys)
            {
                if(floors[fNumber].Contains(loopStPt))
                {
                    sprayOut.PipeInsertPoint = insertPt.OffsetXY(1000, height - 800);
                    sprayOut.CurrentFloor = fNumber;
                }
                //sprayOut.FloorLine.Add(new Line(insertPt, insertPt.OffsetX(length)));
                string text = fNumber + "F(建筑)";
                sprayOut.Texts.Add(new Block.Text(text, insertPt.OffsetY(200), "W-NOTE"));
                sprayIn.floorNumberYDic.Add(fNumber, insertPt.Y);
                insertPt = insertPt.OffsetY(height);

            }
            //sprayOut.FloorLine.Add(new Line(insertPt, insertPt.OffsetX(length)));
            string text1 = " ";
            if(floors.Keys.Last().Contains("B1"))
            {
                text1 = "地库顶板";
            }
            sprayOut.Texts.Add(new Block.Text(text1, insertPt.OffsetY(200), "W-NOTE"));
        }

        public static void Get2(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            //绘制楼板线
            var floors = sprayIn.FloorRectDic;
            double fHeight = sprayIn.FloorHeight;
            Point3d insertPt = sprayOut.InsertPoint.Cloned();
            foreach (var fNumber in floors.Keys)
            {
                sprayOut.FloorLine.Add(new Line(insertPt, new Point3d(spraySystem.MaxOffSetX + 6700, insertPt.Y, 0)));
                insertPt = insertPt.OffsetY(fHeight);
            }
            sprayOut.FloorLine.Add(new Line(insertPt, new Point3d(spraySystem.MaxOffSetX + 6700, insertPt.Y, 0)));
        }
    }
}
