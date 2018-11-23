using System;
using System.Collections.Generic;
using System.Linq;
using BlaBlaCarStatisticAnalizer.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BlaBlaCarStatisticAnalizer.Windows
{
    public class ApiDataViewModel : ReactiveObject
    {
        [Reactive]
        public Dictionary<DateTime, List<TripModel>> TripsOnDay { get; set; }

        [Reactive]
        public string ViewedTripsBlock { get; set; }

        [Reactive]
        public int TotalUsers { get; set; }

        [Reactive]
        public int UsersForPath { get; set; }

        public DateTime SelectedDate { get; set; }

        public ApiDataViewModel(Dictionary<DateTime, List<TripModel>> tripsOnDay)
        {
            TripsOnDay = tripsOnDay;
            ViewedTripsBlock = FormViewedBlock(TripsOnDay.Keys.Last(), TripsOnDay[TripsOnDay.Keys.Last()]);

            foreach (var trips in TripsOnDay.Values)
            {
                foreach (var trip in trips)
                {
                    TotalUsers += trip.Seats - trip.SeatsLeft;
                }
            }
        }

        public void OnDateSelect(object sender, EventArgs args)
        {
            ViewedTripsBlock = FormViewedBlock(SelectedDate, TripsOnDay[SelectedDate]);
        }

        private string FormViewedBlock(DateTime date, IEnumerable<TripModel> trips)
        {
            var dataString = "";

            dataString += $"\nDate: {date}\n";
            UsersForPath = 0;
            foreach (var trip in trips)
            {
                dataString +=
                    $"\nTripId: {trip.TripId}\nDepartureDate: {trip.DepartureDate}\nSeats: {trip.Seats}\nSeatsLeft: {trip.SeatsLeft}\n";
                UsersForPath += trip.Seats - trip.SeatsLeft;
            }

            return dataString;
        }
    }
}
