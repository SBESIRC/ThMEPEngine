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

        #region properties
        public Point3d cenPt
        {
            get
            {
                if (m_cenPt == Point3d.Origin )
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

        public List<Point3d> getConnectPt()
        {
            if (m_connPts == null)
            {
                m_connPts = new List<Point3d>() { leftConnPt, bottomConnPt, rightConnpt, topConnPt };
            }

            return m_connPts;
        }
        #endregion

        public ThBlock(BlockReference blk)
        {
            m_blk = blk;
        }



    }
}
