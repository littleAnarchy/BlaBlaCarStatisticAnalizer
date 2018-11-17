using Newtonsoft.Json;

namespace BlaBlaCarStatisticAnalizer.Models
{
    public class BlaBlaCarResponseModel
    {
        [JsonProperty("pager")]
        public PagerModel Pager { get; set; }
        [JsonProperty("trips")]
        public TripModel[] Trips { get; set; }
    }

    public class PagerModel
    {
        [JsonProperty("page")]
        public int Page { get; set; }
        [JsonProperty("pages")]
        public int Pages { get; set; }
    }

    public class TripModel
    {
        [JsonProperty("departure_date")]
        public string DepartureDate { get; set; }
        [JsonProperty("seats")]
        public int Seats { get; set; }
        [JsonProperty("seats_left")]
        public int SeatsLeft { get; set; }
        [JsonProperty("permanent_id")]
        public string TripId { get; set; }

        public override string ToString()
        {
            return $"{TripId}\nDepartureDate: {DepartureDate}\nSeats: {Seats}\nSeatsLeft: {DepartureDate}\n ";
        }
    }
}
