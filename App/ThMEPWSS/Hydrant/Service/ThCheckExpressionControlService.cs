using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThCheckExpressionControlService
    {
        public readonly static string CheckExpressionLayer = "AI-Hydrant";
        public static void ShowCheckExpression()
        {
            using (var dockLock = AcHelper.Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                if (acadDb.Layers.Contains(CheckExpressionLayer))
                {
                    var ltr = acadDb.Layers.Element(CheckExpressionLayer);
                    ltr.UpgradeOpen();
                    ltr.IsOff = false;
                    ltr.DowngradeOpen();
                }
            }
        }

        public static void CloseCheckExpression()
        {
            using (var dockLock= AcHelper.Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                if (acadDb.Layers.Contains(CheckExpressionLayer))
                {
                    var ltr = acadDb.Layers.Element(CheckExpressionLayer);
                    ltr.UpgradeOpen();
                    ltr.IsOff = true;
                    ltr.DowngradeOpen();
                }
            }
        }
        public static void EraseCheckExpression()
        {
            using (var dockLock = AcHelper.Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                OpenLayer();
                var ents = acadDb.ModelSpace
                    .OfType<Entity>()
                    .Where(e => e.Layer == CheckExpressionLayer)
                    .ToList();
                ents.ForEach(o => o.Erase());
            }
        }
        private static void OpenLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                if (acadDb.Layers.Contains(CheckExpressionLayer))
                {
                    var ltr = acadDb.Layers.Element(CheckExpressionLayer);
                    ltr.IsOff = false;
                    ltr.IsLocked = false;
                    ltr.IsFrozen = false;
                }
            }
        }
    }
}
