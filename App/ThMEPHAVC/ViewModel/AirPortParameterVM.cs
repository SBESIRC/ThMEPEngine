using AcHelper;
using Linq2Acad;
using ThMEPHVAC.Model;
using ThMEPHVAC.Service;

namespace ThMEPHVAC.ViewModel
{
    public class AirPortParameterVM
    {
        public ThAirPortParameter Parameter { get; set; }
        private ThQueryRoomAirVolumeService ReadVolumeService;
        public AirPortParameterVM()
        {
            Parameter = new ThAirPortParameter();
            ReadVolumeService = new ThQueryRoomAirVolumeService();
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
                    var keyWord = ThMEPHAVCDataManager.GetAirVolumeQueryKeyword(Parameter.SystemType);
                    var volume = ReadVolumeService.Query(selector.Rooms[0], keyWord);
                    Parameter.TotalAirVolume = ReadVolumeService.ConvertToDouble(volume);
                }
            }  
        } 

        public void UpdateByTotalAirVolume()
        {
            CalculateSingleAirPortAirVolume();
        }

        public void UpdateByAirPortNum()
        {
            CalculateSingleAirPortAirVolume(); 
        }

        public void UpdateBySingleAirPortAirVolume()
        {
           var size = ThMEPHAVCDataManager.CalculateAirPortSize(
                Parameter.SingleAirPortAirVolume, Parameter.AirPortType);
            if(size!=null)
            {
                Parameter.Length = size.Item1;
                Parameter.Width = size.Item2;
            }
        }

        public void UpdateBySystemType()
        {
            var initAirPortType = ThMEPHAVCDataManager.GetInitAirportType(Parameter.SystemType);
            if (!string.IsNullOrEmpty(initAirPortType))
            {
                Parameter.AirPortType = initAirPortType;
            }
        }

        private void CalculateSingleAirPortAirVolume()
        {
            if (Parameter.AirPortNum != 0)
            {
                Parameter.SingleAirPortAirVolume = Parameter.TotalAirVolume / Parameter.AirPortNum;
            }
        }
    }
}