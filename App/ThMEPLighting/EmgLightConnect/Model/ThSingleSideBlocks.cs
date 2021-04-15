﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.EmgLightConnect.Model
{
    public class ThSingleSideBlocks
    {
        private List<(Line, int)> m_laneSide;
        private List<Point3d> m_secBlk;
        private List<Point3d> m_mainBlk;
        private Dictionary<Point3d, Point3d> m_groupBlk;
        private List<Point3d> m_addMainBlk;
        private List<Point3d> m_tolMainBlk;
        private List<Point3d> m_tolBlk;
        private List<Point3d> m_reMainBlk;
        private List<Point3d> m_reSecBlk;

        private Matrix3d m_matrix;
        private List<(Point3d, Point3d)> m_ptConnect;

        #region properties
        public int laneSideNo { get; set; }

        public List<Point3d> mainBlk
        {
            get
            {
                return m_mainBlk;
            }
        }

        public List<Point3d> secBlk
        {
            get
            {
                return m_secBlk;
            }
        }

        public Dictionary<Point3d, Point3d> groupBlock
        {
            get
            {
                return m_groupBlk;
            }
        }

        public List<Point3d> addMainBlock
        {
            get
            {
                return m_addMainBlk;
            }
        }

        public int Count
        {
            get
            {
                return m_mainBlk.Count + m_secBlk.Count + m_groupBlk.Count + m_addMainBlk.Count;
            }
        }

        public List<(Line, int)> laneSide
        {
            get
            {
                return m_laneSide;
            }
        }

        public Matrix3d Matrix
        {
            get
            {
                if (m_matrix == new Matrix3d())
                {

                    var dir = (laneSide.Last().Item1.EndPoint - laneSide.First().Item1.StartPoint).GetNormal();
                    var rotationangle = Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
                    m_matrix = Matrix3d.Displacement(laneSide.First().Item1.StartPoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                }
                return m_matrix;
            }

        }

        public List<Point3d> reMainBlk
        {
            get
                {
                return m_reMainBlk;
            }
            
        }

        public List<Point3d> reSecBlk
        {
            get { return m_reSecBlk; }
            
        }

        public List<(Point3d,Point3d)> ptLink
        {
            get { return m_ptConnect; }
        }

        #endregion

        public ThSingleSideBlocks(List<Point3d> mainBlock, List<(Line, int)> laneSide)
        {
            this.m_laneSide = laneSide;
            this.m_mainBlk = mainBlock;
            this.m_secBlk = new List<Point3d>();
            this.m_groupBlk = new Dictionary<Point3d, Point3d>();
            this.m_addMainBlk = new List<Point3d>();
            this.m_tolMainBlk = new List<Point3d>();
            this.m_tolBlk = new List<Point3d>();
            this.m_ptConnect = new List<(Point3d, Point3d)>();

            this.m_reMainBlk = new List<Point3d>();
            this.m_reSecBlk = new List<Point3d>();
        }

        //public void setSecondaryBlock(List<Point3d> secBlock)
        //{
        //    m_secBlk = secBlock;
        //}
        
        public void setGroupGroup(Dictionary<Point3d, Point3d> groupBlock)
        {
            m_groupBlk = groupBlock;
        }


        public List<Point3d> getTotalMainBlock()
        {
            m_tolMainBlk.Clear();
            m_tolMainBlk.AddRange(m_mainBlk);
            m_tolMainBlk.AddRange(m_addMainBlk);

            return m_tolMainBlk;

        }

        public List<Point3d> getTotalBlock()
        {

            m_tolBlk.Clear();
            m_tolBlk.AddRange(m_mainBlk);
            m_tolBlk.AddRange(m_addMainBlk);
            m_tolBlk.AddRange(m_secBlk);

            return m_tolBlk;

        }

        public void orderLane()
        {
            if (m_laneSide.Count > 1)
            {
                var orderSide = new List<(Line, int)>();
                var thisLane = m_laneSide[0];
                var pt = thisLane.Item1.StartPoint;

                while (true)
                {
                    var pre = m_laneSide.Where(x => x != thisLane && (x.Item1.StartPoint.IsEqualTo(pt, new Tolerance(1, 1)) || x.Item1.EndPoint.IsEqualTo(pt, new Tolerance(1, 1)))).FirstOrDefault();

                    if (pre.Equals(default(ValueTuple<Line, int>)) == false)
                    {
                        if (pre.Item1.StartPoint.IsEqualTo(pt, new Tolerance(1, 1)))
                        {
                            pt = pre.Item1.EndPoint;
                        }
                        else
                        {
                            pt = pre.Item1.StartPoint;
                        }
                        thisLane = pre;
                    }
                    else
                    {
                        break;
                    }
                }

                while (true)
                {
                    orderSide.Add(thisLane);

                    if (thisLane.Item1.StartPoint.IsEqualTo(pt, new Tolerance(1, 1)))
                    {
                        pt = thisLane.Item1.EndPoint;
                    }
                    else
                    {
                        pt = thisLane.Item1.StartPoint;
                    }

                    var next = m_laneSide.Where(x => x != thisLane && (x.Item1.StartPoint.IsEqualTo(pt, new Tolerance(1, 1)) || x.Item1.EndPoint.IsEqualTo(pt, new Tolerance(1, 1)))).FirstOrDefault();
                    if (next.Equals(default(ValueTuple<Line, int>)) == false)
                    {

                        thisLane = next;
                    }
                    else
                    {
                        break;
                    }
                }
                m_laneSide = orderSide;
            }
        }

        //public void orderMainBlock()
        //{
        //    m_mainBlk = m_mainBlk.OrderBy(x => x.TransformBy(Matrix.Inverse()).X).ToList();
        //}

        public void orderReMainBlk()
        {
            m_reMainBlk = m_reMainBlk.OrderBy(x => x.TransformBy(Matrix.Inverse()).X).ToList();
        }

        public void setReMainBlk(List<Point3d> regroupMain)
        {
            m_reMainBlk = regroupMain;
        }

        public void setReSecBlk(List<Point3d> regroupSec)
        {
            m_reSecBlk = regroupSec;
        }

        
        public int blkConnectNo(Point3d pt)
        {
            var tol = new Tolerance(1, 1);
            var no = 0;
            no = m_ptConnect.Where(x => x.Item1.IsEqualTo(pt, tol) || x.Item2.IsEqualTo(pt, tol)).Count();
            return no;

        }

        public List<Point3d> blkConnectTo(Point3d pt)
        {
            var tol = new Tolerance(1, 1);
            var connList = new List<Point3d>();
            var connList1 = m_ptConnect.Where(x => x.Item1.IsEqualTo(pt, tol)).Select(y => y.Item2).ToList();
            var connList2 = m_ptConnect.Where(x => x.Item2.IsEqualTo(pt, tol)).Select(y => y.Item1).ToList();

            connList.AddRange(connList1);
            connList.AddRange(connList2);

            return connList;
        }

        public bool alreadyConnect(Point3d pt1, Point3d pt2)
        {
            var tol = new Tolerance(1, 1);
            var connect1 = m_ptConnect.Where(x => x.Item1.IsEqualTo(pt1, tol) && x.Item2.IsEqualTo(pt2, tol)).Count();
            var connect2 = m_ptConnect.Where(x => x.Item2.IsEqualTo(pt1, tol) && x.Item1.IsEqualTo(pt2, tol)).Count();

            bool bConn = (connect1 + connect2) > 0 ? true : false;
            return bConn;

        }

        public void connectPt(Point3d pt1, Point3d pt2)
        {
            if (alreadyConnect(pt1,pt2)==false)
            {
                m_ptConnect.Add((pt1, pt2));
            }
        }


        private  List<Point3d> getAllMainAndReMain()
        {
            var returnList = new List<Point3d>();
            returnList.AddRange(this.reMainBlk);
            returnList.AddRange(this.addMainBlock);
            returnList = returnList.Distinct().ToList();

            return returnList;
        }


    }

}
