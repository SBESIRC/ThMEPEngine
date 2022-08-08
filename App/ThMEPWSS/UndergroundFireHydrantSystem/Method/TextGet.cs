using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public class TextGet
    {
        public static void GetText(Point3dEx tpt, FireHydrantSystemIn fireHydrantSysIn, ref FireHydrantSystemOut fireHydrantSysOut, Point3d pt4, Point3d pt6)
        {
            double floorHeight = fireHydrantSysIn.FloorHeight;
            var textWidth = fireHydrantSysIn.TextWidth;
            string pipeNumber1 = "";
            string pipeNumber12 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber;//立管标号
                pipeNumber12 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber2;//立管标号
                textWidth = fireHydrantSysIn.TermPointDic[tpt].TextWidth;
            }

            if (pipeNumber1 != "" && pipeNumber1.IsCurrentFloor())
            {
                var textPt1 = new Point3d(pt4.X - textWidth, pt4.Y - floorHeight * 0.17, 0);
                var textPt2 = new Point3d(pt4.X, pt4.Y - floorHeight * 0.17, 0);
                var textLine = ThTextSet.ThTextLine(textPt1, textPt2);
                if(pipeNumber1.Trim().Count()!=0)
                {
                    fireHydrantSysOut.TextLine.Add(textLine);
                }

                var text = ThTextSet.ThText(textPt1, pipeNumber1.Trim());
                fireHydrantSysOut.TextList.Add(text);
                if (!pipeNumber12.Equals(""))
                {
                    text = ThTextSet.ThText(new Point3d(textPt1.X, textPt1.Y - 400, 0), pipeNumber12.Trim());
                    fireHydrantSysOut.TextList.Add(text);
                }
            }
            fireHydrantSysOut.FireHydrant.Add(pt6);
            if(fireHydrantSysIn.VerticalHasReelHydrant.Contains(tpt))
            {
                fireHydrantSysOut.VerticalHasReelHydrant.Add(pt6);
            }
            var strDN = "DN65";
            if(fireHydrantSysIn.TermDnDic.ContainsKey(tpt))
            {
                strDN = fireHydrantSysIn.TermDnDic[tpt];
            }
            var DN = ThTextSet.ThText(new Point3d(pt4.X + 350, pt4.Y - floorHeight * 0.4, 0), Math.PI / 2, strDN);
            fireHydrantSysOut.DNList.Add(DN);
        }
    }
}
