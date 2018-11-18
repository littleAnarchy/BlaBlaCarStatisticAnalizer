﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlaBlaCarStatisticAnalizer.Common;
using BlaBlaCarStatisticAnalizer.Models;
using LiveCharts;
using LiveCharts.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace BlaBlaCarStatisticAnalizer
{
    public class AnalizerViewModel : ReactiveObject
    {
        private readonly CartesianChart _chart;
        private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;
        private int _currentPath = 1;
        private bool _isAnalize;

        public ObservableCollection<PathChecker> Paths { get; set; } = new ObservableCollection<PathChecker>();

        #region Commands

        public ICommand TestButtonCommand { get; set; }
        public ICommand AddPathButtonCommand { get; set; }
        public ICommand RemovePathCommand { get; set; }
        public ICommand ChangeAnalizingCommand { get; set; }
        public ICommand BuildChartForPathCommand { get; set; }
        public ICommand BuildTotalChartCommand { get; set; }

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
        public string ApiKey { get; set; } = CommonSettings.ApiKey;

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

        #endregion

        public AnalizerViewModel(CartesianChart chart)
        {
            _chart = chart;

            TestButtonCommand = new CommandHandler(OnTestButtonClick, true);
            AddPathButtonCommand = new CommandHandler(AddPath, true);
            RemovePathCommand = new CommandHandler(RemovePath, true);
            ChangeAnalizingCommand = new CommandHandler(AnalizingStateChange, true);
            BuildChartForPathCommand = new CommandHandler(OnBuildChartBtnClick, true);
            BuildTotalChartCommand = new CommandHandler(BuildTotalChart, true);
        }

        private void AddPath()
        {
            try
            {
                if (Paths.Count == CommonSettings.MaxPathsCount) throw new Exception($"Cannot add new path. Max paths count is {CommonSettings.MaxPathsCount}");
                var checker = new PathChecker(Paths.Count + 1, FormRequest());
                checker.OnMessage += OnPathCheckerMessage;
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
                        _currentPath = 1;
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
                Paths.Add(new PathChecker(Paths.Count+1, new BlaBlaCarRequestModel
                {
                    ApiKey = ApiKeyController.CurrentKey,
                    Locale = "uk_UA",
                    PlaceFrom = places[0],
                    PlaceTo = places[1]
                }));
            }

            await BuildChart(Paths);
        }

        private async Task BuildChart(IEnumerable<PathChecker> paths)
        {
            try
            {
                var allData = new Dictionary<DateTime, int>();
                foreach (var path in paths)
                {
                    var data = await path.GetAllData();
                    foreach (var key in data.Keys)
                    {
                        if (allData.Keys.Contains(key))
                            allData[key] += data[key];
                        else
                            allData.Add(key, data[key]);
                    }
                }

                ChartCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Values = new ChartValues<int>(allData.Values)
                    }
                };

                Labels = allData.Keys.ToList().ConvertAll(x => x.ToString(CultureInfo.InvariantCulture)).ToArray();
            }
            catch (Exception e)
            {
                LogMessage($"Exception: {e.Message}");
            }
        }

        private BlaBlaCarRequestModel FormRequest()
        {
            return new BlaBlaCarRequestModel { Locale = Locale ?? "", PlaceFrom = PlaceFrom ?? "", PlaceTo = PlaceTo ?? "", ApiKey = ApiKey ?? CommonSettings.ApiKey };
        }

        private void OnTestButtonClick()
        {
            new Task(async () => await new PathChecker(1000, FormRequest()).GetStatistic()).Start();
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
    }
}
