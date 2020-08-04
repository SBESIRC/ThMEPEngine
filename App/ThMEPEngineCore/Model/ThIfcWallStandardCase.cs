using Xbim.Ifc4.SharedBldgElements;

namespace ThMEPEngineCore.Model
{
    public class ThIfcWallStandardCase : ThIfcWall
    {
        private IfcWallStandardCase Impl { get; set; }
        public ThIfcWallStandardCase(string name)
        {
            var model = ThIfcStoreService.Instance.Model;
            using (var tx = model.BeginTransaction())
            {
                Impl = model.Instances.New<IfcWallStandardCase>();
                Impl.Name = name;
                tx.Commit();
            }
        }
    }
}