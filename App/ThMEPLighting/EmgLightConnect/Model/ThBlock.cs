using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPLighting.EmgLightConnect.Model
{
    public class ThBlock
    {
        private BlockReference m_blk;
        private List<Point3d> m_connPts;
        private Point3d m_cenPt = new Point3d();
        private Dictionary<Point3d, List<Polyline>> m_connLineInfo;

        #region properties
        public Point3d cenPt
        {
            get
            {
                if (m_cenPt == Point3d.Origin)
                {
                    m_cenPt = outline.GetCentroidPoint();
                }
                return m_cenPt;
            }

        }
        public Point3d blkCenPt
        {
            get { return m_blk.Position; }
        }

        public BlockReference blk
        {
            get { return m_blk; }
        }

        public Point3d leftConnPt { get; set; }
        public Point3d bottomConnPt { get; set; }
        public Point3d rightConnpt { get; set; }
        public Point3d topConnPt { get; set; }
        public Polyline outline { get; set; }

        public Dictionary<Point3d, List<Polyline>> connInfo
        {
            get
            {
                return m_connLineInfo;
            }
        }

        #endregion
        public List<Point3d> getConnectPt()
        {
            if (m_connPts == null)
            {
                m_connPts = new List<Point3d>() { leftConnPt, bottomConnPt, rightConnpt, topConnPt };
            }

            return m_connPts;
        }



        public ThBlock(BlockReference blk)
        {
            m_blk = blk;
            m_connLineInfo = new Dictionary<Point3d, List<Polyline>>();
        }

        public void setBlkInfo(Dictionary<string, List<Point3d>> blkSizeDict, BlockReference groupBlk)
        {
            var ptList = blkSizeDict[blk.Name].Select(x => x).ToList();
            var connectPt = ptList.Select(x => x.TransformBy(blk.BlockTransform)).ToList();

            if (groupBlk != null)
            {
                var ptListGroup = blkSizeDict[groupBlk.Name];
                var connectPtGroup = ptListGroup.Select(x => x.TransformBy(groupBlk.BlockTransform)).ToList();
                var bottomPt = connectPtGroup[1];

                var inx = connectPt.IndexOf(connectPt.OrderBy(x => x.DistanceTo(bottomPt)).First());

                var ptNew = new Point3d(ptList[inx].X, ptList[inx].Y / Math.Abs(ptList[inx].Y) * (Math.Abs(ptList[inx].Y) + Math.Abs(ptListGroup[1].Y) + Math.Abs(ptListGroup[3].Y)), 0);
                ptList[inx] = ptNew;

                connectPt = ptList.Select(x => x.TransformBy(blk.BlockTransform)).ToList();
            }

            var blkOutline = new Polyline();

            blkOutline.AddVertexAt(0, new Point2d(ptList[0].X, ptList[3].Y), 0, 0, 0);
            blkOutline.AddVertexAt(1, new Point2d(ptList[0].X, ptList[1].Y), 0, 0, 0);
            blkOutline.AddVertexAt(2, new Point2d(ptList[2].X, ptList[1].Y), 0, 0, 0);
            blkOutline.AddVertexAt(3, new Point2d(ptList[2].X, ptList[3].Y), 0, 0, 0);
            blkOutline.TransformBy(blk.BlockTransform);
            blkOutline.Closed = true;

            outline = blkOutline;
            leftConnPt = connectPt[0];
            bottomConnPt = connectPt[1];
            rightConnpt = connectPt[2];
            topConnPt = connectPt[3];


            connInfo.Add(leftConnPt, new List<Polyline> { });
            connInfo.Add(bottomConnPt, new List<Polyline> { });
            connInfo.Add(rightConnpt, new List<Polyline> { });
            connInfo.Add(topConnPt, new List<Polyline> { });


        }



    }
}
