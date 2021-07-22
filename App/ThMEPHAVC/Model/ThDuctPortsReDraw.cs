using ThCADCore.NTS;

namespace ThMEPHVAC.Model
{
    
    public class ThDuctPortsReDraw
    {
        private ThCADCoreNTSSpatialIndex spatial_index;
        public ThDuctPortsReDraw()
        {
            var comps = ThDuctPortsReadComponent.Read_all_component();
            spatial_index = new ThCADCoreNTSSpatialIndex(comps);
        }
    }
}
