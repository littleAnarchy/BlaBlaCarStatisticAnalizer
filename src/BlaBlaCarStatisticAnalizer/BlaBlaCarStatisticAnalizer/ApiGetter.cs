using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlaBlaCarStatisticAnalizer.Models;
using Newtonsoft.Json.Linq;

namespace BlaBlaCarStatisticAnalizer
{
    public static class ApiGetter
    {
        public static async Task<List<TripModel>> Get(BlaBlaCarRequestModel model)
        {
            var startPage = await SendRequest(model);
            var trips = startPage.Trips.ToList();

            if (startPage.Pager.Pages == 1) return trips;

            for (int i = 2; i <= startPage.Pager.Pages; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                trips.AddRange((await SendRequest(model, i)).Trips.ToList());
            }


            return trips;
        }

        private static async Task<BlaBlaCarResponseModel> SendRequest(BlaBlaCarRequestModel model, int page = 1)
        {
            var request = WebRequest.Create("https://public-api.blablacar.com/api/v2/trips?fn=" + model.PlaceFrom
                                                                                                + "&tn=" + model.PlaceTo +
                                                                                                "&locale=" + model.Locale + "&_format=json" + "&limit=100" + 
                                                                                                "&page=" + page);
            request.Headers["key"] = "7f1c9401470b43d9b9bb5f75198e38b3";
            request.Method = "GET";
            var response = await request.GetResponseAsync();
            var data = await new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException()).ReadToEndAsync();
            var jsonData = JObject.Parse(data);

            return jsonData.Root.ToObject<BlaBlaCarResponseModel>();
        }
    }
}
