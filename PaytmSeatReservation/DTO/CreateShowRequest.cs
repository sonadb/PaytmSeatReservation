//namespace PaytmSeatReservation.DTOs
//{
//    public class CreateShowRequest
//    {
//        public string Name { get; set; } = string.Empty;
//        public List<string> Seats { get; set; } = new();
//        public int PricePaise { get; set; }
//        public int PerUserLimit { get; set; } = 4;
//    }
//}

using System.Text.Json.Serialization;

namespace PaytmSeatReservation.DTOs
{
    public class CreateShowRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("seats")]
        public List<string> Seats { get; set; } = new();

        [JsonPropertyName("price_paise")]
        public int PricePaise { get; set; }

        [JsonPropertyName("per_user_limit")]
        public int PerUserLimit { get; set; } = 4;
    }
}