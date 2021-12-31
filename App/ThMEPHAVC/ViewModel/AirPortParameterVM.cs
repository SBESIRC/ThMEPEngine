using AcHelper;
using Linq2Acad;
using ThMEPHVAC.Model;
using ThMEPHVAC.Service;

namespace ThMEPHVAC.ViewModel
{
    public class AirPortParameterVM
    {
        public ThAirPortParameter Parameter { get; set; }
        
        public AirPortParameterVM()
        {
            Parameter = new ThAirPortParameter();
        }

        public void ReadRoomAirVolume()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                ThMEPHAVCCommon.FocusToCAD();
                var selector = new ThRoomSelector();
                selector.Select();
                if (selector.Rooms.Count==1)
                {
                    var readVolumeService = new ThQueryRoomAirVolumeService();
                    var keyWord = ThMEPHAVCDataManager.GetAirVolumeQueryKeyword(Parameter.SystemType);
                    var volume = readVolumeService.Query(selector.Rooms[0], keyWord);
                    Parameter.TotalAirVolume = readVolumeService.ConvertToDouble(volume);
                }
            }  
        } 
    }
}