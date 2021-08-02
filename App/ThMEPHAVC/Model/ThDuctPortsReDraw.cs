using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using ThCADCore.NTS;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsReDraw
    {
        private ThCADCoreNTSSpatialIndex spatial_index;
        public ThDuctPortsReDraw(ObjectId node_id)
        {
            var start_p = Get_start_point(node_id);
            var comps = ThDuctPortsReadComponent.Read_all_component();
            spatial_index = new ThCADCoreNTSSpatialIndex(comps);

        }
        private Point3d Get_start_point(ObjectId node_id)
        {
            using (var db = AcadDatabase.Active())
            {
                var node = db.Element<BlockReference>(node_id);
                return node.Position;
            }
        }
        private void Search_conn_comp()
        {
            var poly = new Polyline();
            //poly.CreatePolygon(search_point.ToPoint2D(), 4, 10);
        }
    }
}
