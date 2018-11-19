using MahApps.Metro;
using MahApps.Metro.Controls;

namespace BlaBlaCarStatisticAnalizer.Windows
{
    /// <summary>
    /// Логика взаимодействия для ApiDataWindow.xaml
    /// </summary>
    public partial class ApiDataWindow : MetroWindow
    {
        public ApiDataWindow()
        {
            ThemeManager.ChangeAppStyle(this,
                ThemeManager.GetAccent("Emerald"),
                ThemeManager.GetAppTheme("BaseDark"));
            InitializeComponent();
        }
    }
}
