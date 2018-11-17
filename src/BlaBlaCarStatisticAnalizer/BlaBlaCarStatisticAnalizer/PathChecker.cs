using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlaBlaCarStatisticAnalizer.Common;
using BlaBlaCarStatisticAnalizer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BlaBlaCarStatisticAnalizer
{
    public class PathChecker : ReactiveObject
    {
        private Timer _timer;
        private readonly ApiGetter _apiGetter;
        private readonly string _folderPath;

        public event EventHandler OnMessage;

        [Reactive]
        public int Id { get; set; }
        public BlaBlaCarRequestModel Request { get; }

        [Reactive]
        public TimeSpan TimeToHandling { get; set; }

        [Reactive]
        public string State { get; set; }

        [Reactive]
        public int TotalToday { get; set; }

        public PathChecker(int id, BlaBlaCarRequestModel request)
        {
            Id = id;
            Request = request;
            _apiGetter = new ApiGetter();
            _apiGetter.OnMessage += OnApiGetterMessage;
            _folderPath = Directory.GetCurrentDirectory() + "/" + "Trips" + "/" + Request;
            Directory.CreateDirectory(_folderPath);
        }

        public void OnApiGetterMessage(object sender, EventArgs args)
        {
            State = sender as string;
        }

        public void SetReady(TimeSpan time)
        {
            TimeToHandling = time;
            _timer = new Timer(state => OnStep(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void OnStep()
        {
            TimeToHandling += TimeSpan.FromSeconds(1);
        }

        public async Task GetStatistic()
        {
            try
            {
                State = "Start getting statistic";
                var trips = await _apiGetter.Get(Request);
                var message = "";
                foreach (var trip in trips)
                {
                    message += "\n";
                    message += trip;
                }
                new Task(async () => await SaveData(trips)).Start();
                OnMessage?.Invoke(message, null);
                State = "Statistic get";
            }
            catch (Exception e)
            {
                State = "Exception";
                OnMessage?.Invoke(e.Message, null);
                await Task.Delay(TimeSpan.FromSeconds(1));
                await GetStatistic();
            }
            finally
            {
                _timer.Dispose();
            }
        }

        public async Task SaveData(List<TripModel> trips)
        {
            var path = $"{_folderPath}/{DateTime.Today.Date:yyyy-MM-dd}.txt";
            var inFile = new List<TripModel>();
            if (CheckFileExists(path))
            {
                using (var sr = new StreamReader(path))
                {
                    var data = await sr.ReadToEndAsync();

                    var jsonData = JArray.Parse(data);

                    inFile = jsonData.Root.ToObject<TripModel[]>().ToList();
                }
            }
            var t = trips.Union(inFile).Distinct(new TripComparer()).ToList();
            TotalToday = t.Select(x => x.Seats).Sum() - t.Select(y => y.SeatsLeft).Sum();
            using (var sw = new StreamWriter(path))
            {
                await sw.WriteAsync(JsonConvert.SerializeObject(t));
            }

            State = "Data saved";
        }


        private bool CheckFileExists(string path)
        {
            if (File.Exists(path)) return true;
            using (File.Create(path))
            {
                State = $"File {path} created";
                return false;
            }
        }

        public override string ToString()
        {
            return $"{Id}) {Request}";
        }
    }
}
