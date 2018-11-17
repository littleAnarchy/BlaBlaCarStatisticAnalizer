using System;
using System.Threading;
using System.Threading.Tasks;
using BlaBlaCarStatisticAnalizer.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BlaBlaCarStatisticAnalizer
{
    public class PathChecker : ReactiveObject
    {
        private Timer _timer;

        public event EventHandler OnMessage;

        [Reactive]
        public int Id { get; set; }
        public BlaBlaCarRequestModel Request { get; }

        [Reactive]
        public TimeSpan TimeToHandling { get; set; }

        [Reactive]
        public string State { get; set; }

        public PathChecker(int id, BlaBlaCarRequestModel request)
        {
            Id = id;
            Request = request;
        }

        public void SetReady(TimeSpan time)
        {
            TimeToHandling = time;
            _timer = new Timer(state => OnStep(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void OnStep()
        {
            TimeToHandling -= TimeSpan.FromSeconds(1);
            if (TimeToHandling == TimeSpan.FromSeconds(0))
            {
                _timer.Dispose();
                TimeToHandling = TimeSpan.Zero;
            }
        }

        public async Task GetStatistic()
        {
            try
            {
                State = "Start getting statistic";
                var trips = await ApiGetter.Get(Request);
                var message = "";
                foreach (var trip in trips)
                {
                    message += trip;
                }
                OnMessage?.Invoke(message, null);
                State = "Statistic get";
            }
            catch (Exception e)
            {
                OnMessage?.Invoke(e.Message, null);
                State = "Exception";
            }
        }

        public override string ToString()
        {
            return $"{Id}) {Request}";
        }
    }
}
