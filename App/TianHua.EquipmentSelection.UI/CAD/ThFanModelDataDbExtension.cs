using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI.CAD
{
    public static class ThFanModelDataDbExtension
    {
        public static void AppendModelData(this Database database, FanDataModel model)
        {
            Debug.Assert(model != null);
            Debug.Assert(model.IsValid());
            var ds = ThFanModelDataDbSource.Create(database);
            ds.Models.RemoveAll(o => o.ID == model.ID);
            ds.Models.RemoveAll(o => o.PID == model.ID);
            ds.Models.Add(model);
            ds.Save(database);
        }

        public static void AppendModelData(this Database database, FanDataModel model, FanDataModel subModel)
        {
            Debug.Assert(model != null);
            Debug.Assert(model.IsValid());
            Debug.Assert(subModel != null);
            Debug.Assert(subModel.IsSubModel());
            Debug.Assert(subModel.PID == model.ID);
            var ds = ThFanModelDataDbSource.Create(database);
            ds.Models.RemoveAll(o => o.ID == model.ID);
            ds.Models.RemoveAll(o => o.PID == model.ID);
            ds.Models.AddRange(new FanDataModel[] { model, subModel });
            ds.Save(database);
        }

        public static void EraseModelData(this Database database, FanDataModel model)
        {
            Debug.Assert(model != null);
            Debug.Assert(model.IsValid());
            Debug.Assert(model.IsMainModel());
            var ds = new ThFanModelDataDbSource();
            ds.Erase(database, model.ID);
        }
    }
}
