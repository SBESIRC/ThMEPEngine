namespace ThMEPWSS.Pipe.Model
{
    public class ThWCompositeRoom : ThWRoom
    {
        public ThWToiletRoom Toilet { get; set; }
        public ThWKitchenRoom Kitchen { get; set; }

        public ThWCompositeRoom(ThWKitchenRoom kitchenRoom)
        {
            Kitchen = kitchenRoom;
        }

        public ThWCompositeRoom(ThWKitchenRoom kitchenRoom, ThWToiletRoom toilet)
        {
            Kitchen = kitchenRoom;
            Toilet = toilet;
        }
    }
}