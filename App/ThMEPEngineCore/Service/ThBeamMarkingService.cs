using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.BeamInfo.Model;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public class ThBeamMarkingService
    {
        private Beam BeamEnt { get; set; }
        public double BeamWidth { get; set; }
        public Point3d Pt1 { get; set; }
        public Point3d Pt2 { get; set; }
        public Point3d Pt3 { get; set; }
        public Point3d Pt4 { get; set; }
        public ThBeamMarkingService(Beam beam)
        {
            BeamEnt = beam;
            BeamWidth = GetBeamWidth();
            GetBeamTextRangePt();
        }
        public List<DBText> Match(ThCADCoreNTSSpatialIndex dbtextSpatialIndex)
        {
            List<DBText> searchDbTexts = new List<DBText>();
            List<Point3d> rangePts = ThGeometryTool.CalBoundingBox(new List<Point3d>() { Pt1, Pt2, Pt3, Pt4 });
            DBObjectCollection searchTexts = dbtextSpatialIndex.SelectCrossingWindow(rangePts[0], rangePts[1]);
            foreach (var text in searchTexts)
            {
                DBText dbtext = text as DBText;
                var textNormal = Vector3d.XAxis.RotateBy(dbtext.Rotation, Vector3d.ZAxis);
                if(textNormal.IsParallelToEx(BeamEnt.BeamNormal))
                {
                    searchDbTexts.Add(dbtext);
                }
            }
            searchDbTexts = FilterDbTexts(searchDbTexts);
            return searchDbTexts;
        }

        /// <summary>
        /// 获取含有规格的文字
        /// </summary>
        /// <param name="dbTexts"></param>
        /// <returns></returns>
        private List<DBText> FilterDbTexts(List<DBText> dbTexts)
        {
            return dbTexts.Where(o => ThStructureUtils.ValidateSpec(o.TextString)).ToList();
        }
        /// <summary>
        /// 获取梁的宽度
        /// </summary>
        /// <returns></returns>
        private double GetBeamWidth()
        {
            List<double> widths = new List<double>();
            for (int i = 0; i < BeamEnt.BeamBoundary.NumberOfVertices; i++)
            {
                SegmentType st = BeamEnt.BeamBoundary.GetSegmentType(i);
                if (st == SegmentType.Line)
                {
                    LineSegment3d lineSegment = BeamEnt.BeamBoundary.GetLineSegmentAt(i);
                    widths.Add(lineSegment.Length);
                }
            }
            return widths.Where(o => o > 50.0).OrderBy(o => o).FirstOrDefault();
        }
        /// <summary>
        /// 获取
        /// </summary>
        private void GetBeamTextRangePt()
        {
            Vector3d uprightDir = Vector3d.ZAxis.CrossProduct(BeamEnt.BeamNormal);
            Vector3d ptDir = (BeamEnt.UpStartPoint - BeamEnt.DownEndPoint).GetNormal();
            double times = 1.0;
            if (uprightDir.DotProduct(ptDir) > 0)
            {
                this.Pt1 = BeamEnt.UpStartPoint + uprightDir * BeamWidth * times;
                this.Pt2 = BeamEnt.UpEndPoint + uprightDir * BeamWidth * times;
                this.Pt3 = BeamEnt.DownEndPoint - uprightDir * BeamWidth * times;
                this.Pt4 = BeamEnt.DownStartPoint - uprightDir * BeamWidth * times;
            }
            else
            {
                this.Pt1 = BeamEnt.UpStartPoint - uprightDir * BeamWidth * times;
                this.Pt2 = BeamEnt.UpEndPoint - uprightDir * BeamWidth * times;
                this.Pt3 = BeamEnt.DownEndPoint + uprightDir * BeamWidth * times;
                this.Pt4 = BeamEnt.DownStartPoint + uprightDir * BeamWidth * times;
            }
        }
    }
}
