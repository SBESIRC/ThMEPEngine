using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThModelRoateStateOverride : IDisposable
    {
        private FanDataModel Model { get; set; }

        private Dictionary<ObjectId, short> States { get; set; }

        public ThModelRoateStateOverride(FanDataModel model)
        {
            Model = model;
            States = ThFanSelectionEngine.GetModelRotateState(Model);
            ThFanSelectionEngine.ResetModelRotateState(Model);
        }

        public void Dispose()
        {
            ThFanSelectionEngine.SetModelRotateState(Model, States);
        }
    }
}
