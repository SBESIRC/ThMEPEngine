using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public abstract class ThArchitectureDbExtension:ThDbExtension
    {
        protected ThArchitectureDbExtension(Database db) : base(db)
        {
        }
    }
}
