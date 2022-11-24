using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;
using ThMEPWSS.UndergroundSpraySystem.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThCADCore.NTS;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class StoreyLine
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            var floors = sprayIn.FloorRectDic;
            var b1m = "B1M";
            var b0 = "B0";
            bool hasB1M = floors.ContainsKey(b1m);
            if(hasB1M)
            {
                var rect = floors[b1m];
                floors.Remove(b1m);
                floors.Add(b0, rect);
            }

            double length = sprayIn.FloorLength;
            double height = sprayIn.FloorHeight;
            Point3d insertPt = sprayOut.InsertPoint.Cloned();
            Point3d loopStPt = sprayIn.LoopStartPt._pt;
            floors = floors.OrderByDescending(e => e.Key).ToDictionary(e => e.Key, e => e.Value);
            if (hasB1M)
            {
                var rect = floors[b0];
                floors.Remove(b0);
                floors.Add(b1m, rect);
            }
            foreach (var fNumber in floors.Keys)
            {
                if(floors[fNumber].Contains(loopStPt))
                {
                    sprayOut.PipeInsertPoint = insertPt.OffsetXY(1000, height - 800);
                    sprayOut.CurrentFloor = fNumber;
                }
                foreach(var pt in sprayIn.AlarmValveStPts)
                {
                    if (floors[fNumber].Contains(pt._pt))
                    {
                        spraySystem.AlarmValveStPtY.Add(pt, insertPt.Y+6200);
                    }
                }
                string text = fNumber + "F(建筑)";
                sprayOut.Texts.Add(new Block.Text(text, insertPt.OffsetY(200), "W-NOTE"));
                sprayIn.floorNumberYDic.Add(fNumber, insertPt.Y);
                insertPt = insertPt.OffsetY(height);

            }
            string text1 = "地库顶板";
            
            sprayOut.Texts.Add(new Block.Text(text1, insertPt.OffsetY(200), "W-NOTE"));
        }

        public static void Get(FireHydrantSystemIn fireHydrantSysIn, FireHydrantSystemOut fireHydrantSysOut, double pipeLen)
        {
            var floors = fireHydrantSysIn.FloorRect;
            var b1m = "B1M";
            var b0 = "B0";
            bool hasB1M = floors.ContainsKey(b1m);
            if (hasB1M)
            {
                var rect = floors[b1m];
                floors.Remove(b1m);
                floors.Add(b0, rect);
            }

            double length = pipeLen;
            double height = fireHydrantSysIn.FloorHeight;
            Point3d insertPt = fireHydrantSysOut.InsertPoint.OffsetY(-height + 1000);
            Point3d loopStPt = fireHydrantSysIn.StartEndPts[0]._pt;
            floors = floors.OrderByDescending(e => e.Key).ToDictionary(e => e.Key, e => e.Value);
            if (hasB1M)
            {
                var rect = floors[b0];
                floors.Remove(b0);
                floors.Add(b1m, rect);
            }
            foreach (var fNumber in floors.Keys)
            {
                string text = fNumber + "F(建筑)";
                var Text = new Block.Text(text, insertPt.OffsetY(200), "W-NOTE");

                fireHydrantSysOut.DNList.Add(Text.DbText);
                fireHydrantSysOut.StoreyLine.Add(new Line(insertPt, insertPt.OffsetX(pipeLen)));
                insertPt = insertPt.OffsetY(height);
            }
            string text1 = " ";

            text1 = "地库顶板";
            var Text2 = new Block.Text(text1, insertPt.OffsetY(200), "W-NOTE");
            fireHydrantSysOut.DNList.Add(Text2.DbText);
            fireHydrantSysOut.StoreyLine.Add(new Line(insertPt, insertPt.OffsetX(pipeLen)));
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
