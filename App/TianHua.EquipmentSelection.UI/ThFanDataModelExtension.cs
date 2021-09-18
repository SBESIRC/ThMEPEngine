using System;
using System.Collections.Generic;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.UI
{
    public static class ThFanDataModelExtension
    {
        public static FanDataModel GetSubModel(this List<FanDataModel> models, string identifier)
        {
            var _Fan = models.Find(p => p.ID == identifier);
            if (_Fan != null)
            {
                var _subFan = models.Find(p => p.PID == _Fan.ID);
                if (_subFan != null)
                {
                    return _subFan;
                }
            }
            return null;
        }

        public static List<FanDataModel> CloneModel(this List<FanDataModel> models, string identifier, ref string cloneIdentifier)
        {
            var _Fan = models.Find(p => p.ID == identifier);
            if (_Fan != null)
            {
                var _Json = FuncJson.Serialize(_Fan);
                var _Guid = Guid.NewGuid().ToString();
                var _Clones = new List<FanDataModel>();
                var _FanDataModel = FuncJson.Deserialize<FanDataModel>(_Json);

                _FanDataModel.PID = "0";
                _FanDataModel.ID = _Guid;
                _FanDataModel.IsErased = false;
                _FanDataModel.Name = _FanDataModel.Name;
                _FanDataModel.InstallFloor = models.SetFanDataModelByFloor(_FanDataModel);
                _Clones.Add(_FanDataModel);

                var _SonFan = models.Find(p => p.PID == _Fan.ID);
                if (_SonFan != null)
                {
                    var _SonJson = FuncJson.Serialize(_SonFan);
                    var _SonFanData = FuncJson.Deserialize<FanDataModel>(_SonJson);

                    _SonFanData.ID = Guid.NewGuid().ToString();
                    _SonFanData.PID = _Guid;
                    _SonFanData.IsErased = false;
                    _Clones.Add(_SonFanData);
                }

                cloneIdentifier = _Guid;
                return _Clones;
            }
            return new List<FanDataModel>();
        }

        public static string CloneModel(this List<FanDataModel> models, string identifier)
        {
            var _Fan = models.Find(p => p.ID == identifier);
            if (_Fan != null)
            {
                var _Json = FuncJson.Serialize(_Fan);
                var _Guid = Guid.NewGuid().ToString();
                var _Clones = new List<FanDataModel>();
                var _FanDataModel = FuncJson.Deserialize<FanDataModel>(_Json);

                _FanDataModel.PID = "0";
                _FanDataModel.ID = _Guid;
                _FanDataModel.IsErased = false;
                _FanDataModel.Name = _FanDataModel.Name;
                _FanDataModel.InstallFloor = models.SetFanDataModelByFloor(_FanDataModel);
                _Clones.Add(_FanDataModel);

                var _SonFan = models.Find(p => p.PID == _Fan.ID);
                if (_SonFan != null)
                {
                    var _SonJson = FuncJson.Serialize(_SonFan);
                    var _SonFanData = FuncJson.Deserialize<FanDataModel>(_SonJson);

                    _SonFanData.ID = Guid.NewGuid().ToString();
                    _SonFanData.PID = _Guid;
                    _SonFanData.IsErased = false;
                    _Clones.Add(_SonFanData);
                }

                if (_Clones.Count > 0)
                {
                    var _Index = models.IndexOf(_Fan);
                    models.InsertRange(_Index + 1, _Clones);
                }

                return _Guid;
            }
            return string.Empty;
        }

        public static string SetFanDataModelByFloor(this List<FanDataModel> models, FanDataModel _FanDataModel)
        {
            var _List = models.FindAll(p => p.InstallFloor.Contains(_FanDataModel.InstallFloor) && p.PID == _FanDataModel.PID && p.ID != _FanDataModel.ID);
            if (_List == null || _List.Count == 0) { return string.Format("{0}-副本", _FanDataModel.InstallFloor); }
            for (int i = 1; i < 10000; i++)
            {
                if (i == 1)
                {
                    var installFloor = string.Format("{0}-副本", _FanDataModel.InstallFloor);
                    var _ListTemp1 = models.FindAll(p => p.InstallFloor == installFloor && p.PID == _FanDataModel.PID && p.ID != _FanDataModel.ID);
                    if (_ListTemp1 == null || _ListTemp1.Count == 0) { return installFloor; }
                }
                else
                {
                    var installFloor = string.Format("{0}-副本({1})", _FanDataModel.InstallFloor, i);
                    var _ListTemp = models.FindAll(p => p.InstallFloor == installFloor && p.PID == _FanDataModel.PID && p.ID != _FanDataModel.ID);
                    if (_ListTemp == null || _ListTemp.Count == 0) { return installFloor; }
                }

            }
            return string.Empty;
        }
    }
}
