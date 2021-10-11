using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.DrainageSystemAG.Models
{
    class PointLabelInfo
    {
        public Point3d BasePoint { get; }
        public int OrderNum { get; }
        public string UpText { get; set; }
        public string BottomText { get; set; }
        public string BelongId { get; }
        public string Tag { get; set; }
        public string Type { get; set; }
        public PointLabelInfo(Point3d point, string belongId, int order, string upTxt)
        {
            this.BasePoint = point;
            this.BelongId = belongId;
            this.OrderNum = order;
            this.UpText = upTxt;
        }
    }
    class CheckDirection
    {
        public Vector3d direction { get; }
        public Vector3d outDirection { get; }
        public double minDistance { get; }
        public double maxDistance { get; }
        public double dirSetp { get; set; }
        public CheckDirection(Vector3d direction, Vector3d outDirection, double startDis, double maxDis, double step)
        {
            this.direction = direction;
            this.outDirection = outDirection;
            this.minDistance = startDis <= 0 ? 0 : startDis;
            this.maxDistance = maxDis >= this.minDistance ? maxDis : this.minDistance;
            this.dirSetp = step <= 0 ? 5 : step;
        }
    }
}
