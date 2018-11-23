using System;
using System.Collections.Generic;
using BlaBlaCarStatisticAnalizer.Models;
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace BlaBlaCarStatisticAnalizer.Windows
{
    /// <summary>
    /// Логика взаимодействия для ApiDataWindow.xaml
    /// </summary>
    public partial class ApiDataWindow : MetroWindow
    {
        public ApiDataWindow(Dictionary<DateTime, List<TripModel>> tripsOnDay)
        {
            ThemeManager.ChangeAppStyle(this,
                ThemeManager.GetAccent("Blue"),
                ThemeManager.GetAppTheme("BaseDark"));
            var context = new ApiDataViewModel(tripsOnDay);
            DataContext = context;
            InitializeComponent();
            DatesList.SelectionChanged += context.OnDateSelect;
            DatesList.SelectedIndex = tripsOnDay.Count-1;
        }
    }
}
