using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Model;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Service;
using Dreambuild.AutoCAD;
using AcHelper;

namespace ThMEPStructure.Reinforcement.Draw
{
    public class ThColumnReinforceTableBuilder
    {
        private Extents2d extents = new Extents2d();
        private string elevation = "";
        private double tblRowHeight;
        private string drawingScale = "";

        public ThColumnReinforceTableBuilder(Extents2d extents,string elevation, 
            string drawingScale,double tblRowHeight)
        {
            this.extents = extents;
            this.elevation = elevation;
            this.drawingScale = drawingScale;
            this.tblRowHeight = tblRowHeight;
        }
       
        public DBObjectCollection Build(List<ThColumnComponent> components)
        {
            DBObjectCollection objectCollection = new DBObjectCollection();
            DBObjectCollection tmpColllection = new DBObjectCollection();
            //��������Ŀ�
            double width = extents.MaxPoint.X - extents.MinPoint.X;
            double startY = extents.MaxPoint.Y;
            double startX = extents.MinPoint.X;
            //��ȡ�Ŵ�ߴ�
            double scale = Helper.CalScale(this.drawingScale);
            List<ThColumnComponent> tmpComponents = new List<ThColumnComponent>();
            //��Ҫ��������У���������һ�����������ĳ��Ϳ�����������ĳߴ��ֹͣ��ӵ�����������ĳ��Ϳ���Ϊ���Ľ���ߴ�
            double firstRowH = 0;
            double tmpH, tmpW;
            List<double> widths = new List<double>();
            double widthSum = 0;
            int cnt = 0;
            foreach (var component in components)
            {
                component.InitAndCalTableSize(this.elevation, this.tblRowHeight, scale, out tmpH, out tmpW);
                widthSum += tmpW;
                //���������Χ�ͻ���
                if (widthSum > width)
                {
                    //����֮ǰ���ÿһ��
                    foreach (var componentRow in tmpComponents)
                    {
                        tmpColllection = componentRow.Draw(firstRowH, widths[cnt], new Point3d(startX, startY, 0));
                        foreach (DBObject dBObject in tmpColllection)
                        {
                            objectCollection.Add(dBObject);
                        }
                        startX += widths[cnt];
                        cnt++;
                    }
                    cnt = 0;
                    widthSum = 0;
                    widths.Clear();
                    tmpComponents.Clear();
                    startY -= firstRowH + 4 * this.tblRowHeight;
                    startX = extents.MinPoint.X;
                    firstRowH = tmpH;
                    widthSum = tmpW;

                }
                widths.Add(tmpW);
                tmpComponents.Add(component);
                if (firstRowH < tmpH)
                {
                    firstRowH = tmpH;
                }
            }
            foreach (var componentRow in tmpComponents)
            {
                tmpColllection = componentRow.Draw(firstRowH, widths[cnt], new Point3d(startX, startY, 0));
                foreach (DBObject dBObject in tmpColllection)
                {
                    objectCollection.Add(dBObject);
                }
                startX += widths[cnt];
                cnt++;
            }

            return objectCollection;
            throw new NotImplementedException();
        }
    }
}
