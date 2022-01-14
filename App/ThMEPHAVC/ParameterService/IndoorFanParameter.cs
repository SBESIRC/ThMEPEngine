using ThMEPHVAC.IndoorFanModels;

namespace ThMEPHVAC.ParameterService
{
    public class IndoorFanParameter
    {
        IndoorFanParameter()
        {
            PlaceModel = new IndoorFanPlaceModel();
            PlaceModel.HisVentCount = 1;
            ChangeLayoutModel = new IndoorFanLayoutModel();
            PlaceModel = new IndoorFanPlaceModel();
            CheckModel = new IndoorFanCheckModel();
            ExportModel = new IndoorFanExportModel();
        }
        public static IndoorFanParameter Instance = new IndoorFanParameter();
        public IndoorFanLayoutModel LayoutModel { get; set; }
        public IndoorFanLayoutModel ChangeLayoutModel { get; set; }
        public IndoorFanPlaceModel PlaceModel { get; set; }
        public IndoorFanCheckModel CheckModel { get; set; }
        public IndoorFanExportModel ExportModel { get; set; }
    }
}
