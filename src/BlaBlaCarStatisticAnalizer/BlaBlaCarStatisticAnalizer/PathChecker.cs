using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
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
            _apiGetter.OnChangeOwnerState += OnApiGetterStateGet;
            _folderPath = Directory.GetCurrentDirectory() + "/" + "Trips" + "/" + Request;
            Directory.CreateDirectory(_folderPath);
        }

        public void OnApiGetterMessage(object sender, EventArgs args)
        {
            OnMessage?.Invoke(sender as string, null);
        }

        public void OnApiGetterStateGet(object sender, EventArgs args)
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
                ApiKeyController.SkipKey();
                Request.ApiKey = ApiKeyController.CurrentKey;
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
                inFile = await ReadData(path);

            var t = trips.Union(inFile).Distinct(new TripComparer()).ToList();
            TotalToday = CalculateTotalSeats(t);
            using (var sw = new StreamWriter(path))
            {
                await sw.WriteAsync(JsonConvert.SerializeObject(t));
            }

            State = "Data saved";
        }

        private static async Task<List<TripModel>> ReadData(string path)
        {
            using (var sr = new StreamReader(path))
            {
                var data = await sr.ReadToEndAsync();

                var jsonData = JArray.Parse(data);

                return jsonData.Root.ToObject<TripModel[]>().ToList();
            }
        }

        private static int CalculateTotalSeats(IReadOnlyCollection<TripModel> trips)
        {
            return trips.Select(x => x.Seats).Sum() - trips.Select(y => y.SeatsLeft).Sum();
        }

        public async Task<Dictionary<DateTime, int>> GetAllData()
        {
            var data = new Dictionary<DateTime, int>();
            foreach (var file in Directory.EnumerateFiles(_folderPath, "*", SearchOption.TopDirectoryOnly))
            {
                var date = file.Remove(0, file.LastIndexOf(@"2", StringComparison.Ordinal));
                var date2 = date.Remove(date.IndexOf(".", StringComparison.Ordinal));
                if (!DateTime.TryParse(date2, out var fileDate)) continue;

                data.Add(fileDate, CalculateTotalSeats(await ReadData(file)));
            }

            return data;
        }

        public static List<string> GetAllPaths()
        {
            var returnList = new List<string>();
            var data = Directory.EnumerateDirectories(Directory.GetCurrentDirectory() + "/" + "Trips" + "/").ToList();
            foreach (var d in data)
            {
                var d1 = d.Remove(0, d.LastIndexOf(@"/", StringComparison.Ordinal));
                returnList.Add(d1.TrimStart('/'));
            }

            return returnList;
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
