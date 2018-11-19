using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlaBlaCarStatisticAnalizer.Common;
using BlaBlaCarStatisticAnalizer.Models;
using BlaBlaCarStatisticAnalizer.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace BlaBlaCarStatisticAnalizer
{
    public class AnalizerViewModel : ReactiveObject
    {
        private readonly Window _owner;
        private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;
        private int _currentPath = 1;
        private bool _isAnalize;
        private TimeSpan _timeBuf = TimeSpan.Zero;

        public ObservableCollection<PathChecker> Paths { get; set; } = new ObservableCollection<PathChecker>();

        #region Commands

        public ICommand TestButtonCommand { get; set; }
        public ICommand AddPathButtonCommand { get; set; }
        public ICommand RemovePathCommand { get; set; }
        public ICommand ChangeAnalizingCommand { get; set; }
        public ICommand BuildChartForPathCommand { get; set; }
        public ICommand BuildTotalChartCommand { get; set; }
        public ICommand GetTodayDataForPathCommand { get; set; }
        public ICommand GetAllTimeDataForPathCommand { get; set; }

        #endregion

        #region BindingFields

        [Reactive]
        public string ResponseDataLog { get; set; }

        [Reactive]
        public string PlaceFrom { get; set; }

        [Reactive]
        public string PlaceTo { get; set; }

        [Reactive]
        public string Locale { get; set; } = "uk_UA";

        [Reactive]
        public PathChecker SelectedPath { get; set; }

        [Reactive]
        public string StartBtnText { get; set; } = "Start";

        [Reactive]
        public bool IsStartButtonEnable { get; set; } = true;

        [Reactive]
        public string ApiKey { get; set; }

        [Reactive]
        public SeriesCollection ChartCollection { get; set; } = new SeriesCollection
        {
            new LineSeries
            {
                Values = new ChartValues<double> { 3, 5, 7, 4 }
            }
        };

        [Reactive]
        public string[] Labels { get; set; } = { "Jan", "Feb", "Mar", "Apr", "May" };

        [Reactive]
        public string CurrentApiKey { get; set; } = "0";

        [Reactive]
        public TimeSpan LastCycleTime { get; set; } = TimeSpan.Zero; 

        #endregion

        public AnalizerViewModel(Window owner)
        {
            _owner = owner;

            TestButtonCommand = new CommandHandler(OnTestButtonClick, true);
            AddPathButtonCommand = new CommandHandler(AddPath, true);
            RemovePathCommand = new CommandHandler(RemovePath, true);
            ChangeAnalizingCommand = new CommandHandler(AnalizingStateChange, true);
            BuildChartForPathCommand = new CommandHandler(OnBuildChartBtnClick, true);
            BuildTotalChartCommand = new CommandHandler(BuildTotalChart, true);
            GetAllTimeDataForPathCommand = new CommandHandler(GetAllTimeDataForPath, true);
            GetTodayDataForPathCommand = new CommandHandler(GetTodayDataForPath, true);

            ApiKeyController.OnChangeCurrentKey += OnChangeApiCurrentKey;
        }

        private void AddPath()
        {
            try
            {
                if (Paths.Count == CommonSettings.MaxPathsCount) throw new Exception($"Cannot add new path. Max paths count is {CommonSettings.MaxPathsCount}");
                var checker = new PathChecker(Paths.Count + 1, FormRequest());
                checker.OnMessage += OnPathCheckerMessage;
                checker.OnPathHandlingEnd += OnPathHandlingEnd;
                _uiContext.Send(x => Paths.Add(checker), null);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void OnPathCheckerMessage(object sender, EventArgs args)
        {
            LogMessage(sender as string);
        }

        private void RemovePath()
        {
            _uiContext.Send(x =>
            {
                Paths.Remove(SelectedPath);
                for (var i = 0; i < Paths.Count; i++)
                {
                    Paths[i].Id = i + 1;
                }
            }, null);
        }

        private void OnChangeApiCurrentKey(object sender, EventArgs args)
        {
            CurrentApiKey = sender as string;
        }

        private async Task HandlePaths()
        {
            try
            {
                while (_isAnalize)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    if (Paths.Count == 0) return;
                    var checker = Paths.FirstOrDefault(path => path.Id == _currentPath);
                    _uiContext.Send(x =>
                    {
                        checker?.SetReady(TimeSpan.FromSeconds(0));
                    }, null);
                    if (checker != null) await checker.GetStatistic();
                    if (_currentPath == Paths.Count)
                    {
                        _currentPath = 1;
                        LastCycleTime = _timeBuf;
                        _timeBuf = TimeSpan.Zero;
                    }
                    else
                        _currentPath++;
                }

                IsStartButtonEnable = true;
            }
            catch (Exception e)
            {
                LogMessage(e.Message);
            }
        }

        private void OnPathHandlingEnd(object sender, EventArgs args)
        {
            _timeBuf += (TimeSpan) sender;
        }

        private async void OnBuildChartBtnClick()
        {
            await BuildChart(new List<PathChecker> {SelectedPath});
        }

        private async void BuildTotalChart()
        {
            foreach (var path in PathChecker.GetAllPaths())
            {
                var places = path.Split('-');
                if (places[0] == "C") places[0] = "";
                var checker = new PathChecker(Paths.Count + 1, new BlaBlaCarRequestModel
                {
                    ApiKey = ApiKeyController.CurrentKey,
                    Locale = "uk_UA",
                    PlaceFrom = places[0],
                    PlaceTo = places[1]
                });
                checker.OnMessage += OnPathCheckerMessage;
                checker.OnPathHandlingEnd += OnPathHandlingEnd;
                Paths.Add(checker);
            }

            await BuildChart(Paths);
        }

        private async Task BuildChart(IEnumerable<PathChecker> paths)
        {
            try
            {
                var allData = new Dictionary<string, int>();
                foreach (var path in paths)
                {
                    var data = await path.GetAllData();
                    foreach (var key in data.Keys)
                    {
                        if (allData.Keys.Contains(key.ToString("yyyy/MM/dd")))
                            allData[key.ToString("yyyy/MM/dd")] += data[key];
                        else
                            allData.Add(key.ToString("yyyy/MM/dd"), data[key]);
                    }
                }

                ChartCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Values = new ChartValues<int>(allData.Values)
                    }
                };

                Labels = allData.Keys.ToArray();
            }
            catch (Exception e)
            {
                LogMessage($"Exception: {e.Message}");
            }
        }

        private BlaBlaCarRequestModel FormRequest()
        {
            return new BlaBlaCarRequestModel { Locale = Locale ?? "", PlaceFrom = PlaceFrom ?? "", PlaceTo = PlaceTo ?? "", ApiKey = ApiKey };
        }

        private void OnTestButtonClick()
        {
            var checker = new PathChecker(1000, FormRequest());
            checker.OnMessage += OnPathCheckerMessage;
            var task = new Task(async () => await checker.GetStatistic());
            task.Start();
        }

        private void LogMessage(string message)
        {
            ClearLog();
            ResponseDataLog += $"\n{message}";
        }

        private void ClearLog()
        {
            ResponseDataLog = "";
        }

        private void AnalizingStateChange()
        {
            _isAnalize = !_isAnalize;
            if (_isAnalize)
            {
                new Task(async () => await HandlePaths()).Start();
                StartBtnText = "Stop";
            }
            else
            {
                StartBtnText = "Start";
                IsStartButtonEnable = false;
            }
        }

        private async void GetTodayDataForPath()
        {
            var data = await SelectedPath.GetTodayDataForPath();
            var dataString = "";

            foreach (var trip in data)
            {
                dataString +=
                    $"\nTripId: {trip.TripId}\nDepartureDate: {trip.DepartureDate}\nSeats: {trip.Seats}\nSeatsLeft: {trip.SeatsLeft}\n";
            }

            var window = new ApiDataWindow {Owner = _owner, DataBox = {Text = dataString}};
            window.Show();
        }

        private async void GetAllTimeDataForPath()
        {
            var data = await SelectedPath.GetAllTimeDataForPath();
            var dataString = "";

            foreach (var date in data.Keys)
            {
                foreach (var trip in data[date])
                {
                    dataString +=
                        $"\nDate: {date}\n\nTripId: {trip.TripId}\nDepartureDate: {trip.DepartureDate}\nSeats: {trip.Seats}\nSeatsLeft: {trip.SeatsLeft}\n";
                }
            }

            var window = new ApiDataWindow { Owner = _owner, DataBox = { Text = dataString } };
            window.Show();
        }
    }
}
