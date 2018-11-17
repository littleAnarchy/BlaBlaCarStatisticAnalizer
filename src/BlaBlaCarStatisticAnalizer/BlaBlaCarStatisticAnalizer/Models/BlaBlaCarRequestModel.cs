namespace BlaBlaCarStatisticAnalizer.Models
{
    public class BlaBlaCarRequestModel
    {
        public string PlaceFrom { get; set; }
        public string PlaceTo { get; set; }
        public string Locale { get; set; }
        public string ApiKey { get; set; }

        public override string ToString()
        {
            return $"{PlaceFrom}-{PlaceTo}";
        }
    }
}
