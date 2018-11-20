using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public event EventHandler OnPathHandlingEnd;

        [Reactive]
        public int Id { get; set; }
        public BlaBlaCarRequestModel Request { get; }

        [Reactive]
        public TimeSpan TimeToHandling { get; set; }

        [Reactive]
        public string State { get; set; }

        [Reactive]
        public int TotalToday { get; set; }

        [Reactive]
        public bool IsActive { get; set; }

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
                IsActive = true;
                State = "Start getting statistic";
                var trips = await _apiGetter.Get(Request);
                var message = "";
                foreach (var trip in trips)
                {
                    message += "\n";
                    message += trip;
                }

                var task = new Task(async () => await SaveData(trips));
                task.Start();

                OnMessage?.Invoke(message, null);
                State = "Statistic get";
            }
            catch (Exception e)
            {
                State = "Exception";
                OnMessage?.Invoke(e.Message, null);
                ApiKeyController.SkipKey();
                Request.ApiKey = ApiKeyController.CurrentKey;
                await GetStatistic();
            }
            finally
            {
                OnPathHandlingEnd?.Invoke(TimeToHandling, null);
                _timer?.Dispose();
                IsActive = false;
            }
        }

        public async Task SaveData(List<TripModel> trips)
        {
            var path = $"{_folderPath}/{DateTime.Today.Date:yyyy-MM-dd}.txt";
            var inFile = new List<TripModel>();
            if (CheckFileExists(path))
                inFile = await ReadData(path);

            foreach (var trip in trips)
            {
               var analog = inFile.FirstOrDefault(x => x.TripId == trip.TripId);
                if (analog == null)
                {
                    if (DateTime.Parse(trip.DepartureDate).Date == DateTime.Today.Date)
                        inFile.Add(trip);
                }
                else
                {
                    var index = inFile.IndexOf(analog);
                    inFile[index] = trip;
                }
            }

            TotalToday = CalculateTotalSeats(inFile);
            using (var sw = new StreamWriter(path))
            {
                await sw.WriteAsync(JsonConvert.SerializeObject(inFile));
            }

            State = "Data saved";
        }

        private static async Task<List<TripModel>> ReadData(string path)
        {
            if (!File.Exists(path)) return new List<TripModel>();
            using (var sr = new StreamReader(path))
            {
                var data = await sr.ReadToEndAsync();

                var jsonData = JArray.Parse(data);

                return jsonData.Root.ToObject<TripModel[]>().ToList();
            }
        }

        //Todo path is field
        public async Task<List<TripModel>> GetTodayDataForPath()
        {
            return await ReadData($"{_folderPath}/{DateTime.Today.Date:yyyy-MM-dd}.txt");
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
                var date = file.Remove(0, file.LastIndexOf(@"\", StringComparison.Ordinal));
                var date2 = date.Remove(0, 1);
                var date3 = date2.Remove(date2.IndexOf(".", StringComparison.Ordinal));
                if (!DateTime.TryParse(date3, out var fileDate)) continue;

                data.Add(fileDate, CalculateTotalSeats(await ReadData(file)));
            }

            return data;
        }

        public async Task<Dictionary<DateTime, List<TripModel>>> GetAllTimeDataForPath()
        {
            var data = new Dictionary<DateTime, List<TripModel>>();
            foreach (var file in Directory.EnumerateFiles(_folderPath, "*", SearchOption.TopDirectoryOnly))
            {
                var date = file.Remove(0, file.LastIndexOf(@"2", StringComparison.Ordinal));
                var date2 = date.Remove(date.IndexOf(".", StringComparison.Ordinal));
                if (!DateTime.TryParse(date2, out var fileDate)) continue;

                data.Add(fileDate, await ReadData(file));
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
