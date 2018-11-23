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
    public class ApiGetter
    {
        private int _tryNum;

        public event EventHandler OnMessage;
        public event EventHandler OnChangeOwnerState;

        public async Task<List<TripModel>> Get(BlaBlaCarRequestModel model)
        {
            try
            {
                ApiKeyController.SkipKey();
                model.ApiKey = ApiKeyController.CurrentKey;
                OnChangeOwnerState?.Invoke("Getting page 1", null);
                var startPage = await SendRequest(model);
                var trips = startPage.Trips.ToList();

                if (startPage.Pager.Pages == 1) return trips;

                for (var i = 2; i <= startPage.Pager.Pages; i++)
                {
                    ApiKeyController.SkipKey();
                    model.ApiKey = ApiKeyController.CurrentKey;
                    OnChangeOwnerState?.Invoke($"Getting page {i}/{startPage.Pager.Pages}", null);
                    trips.AddRange((await SendRequest(model, i)).Trips.ToList());
                }

                return trips;
            }
            catch (Exception e)
            {
                _tryNum = 0;
                ApiKeyController.SkipKey();
                OnChangeOwnerState?.Invoke(e.Message, null);
                OnMessage?.Invoke(e.Message, null);
                throw;
            }
        }

        private async Task<BlaBlaCarResponseModel> SendRequest(BlaBlaCarRequestModel model, int page = 1)
        {
            try
            {
                
                var request = WebRequest.Create("https://public-api.blablacar.com/api/v2/trips?fn=" + model.PlaceFrom
                                                                                                    + "&tn=" + model.PlaceTo +
                                                                                                    "&locale=" + model.Locale + "&_format=json" + "&limit=100" + 
                                                                                                    "&page=" + page+
                                                                                                    $"&db={DateTime.Today.Date:yyyy-MM-dd}");
                request.Headers["key"] = model.ApiKey != "" ? model.ApiKey : ApiKeyController.CurrentKey;
                request.Method = "GET";
                var response = await request.GetResponseAsync();
                var data = await new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException()).ReadToEndAsync();
                var jsonData = JObject.Parse(data);

                _tryNum = 0;

                return jsonData.Root.ToObject<BlaBlaCarResponseModel>();
            }
            catch (Exception e)
            {
                _tryNum++;
                if (_tryNum == 5) throw new Exception("Fatal request error");

                OnChangeOwnerState?.Invoke("Exception", null);
                OnMessage?.Invoke(e.Message, null);
                ApiKeyController.SkipKey();
                model.ApiKey = ApiKeyController.CurrentKey;
                return await SendRequest(model, page);
            }
        }
    }
}
