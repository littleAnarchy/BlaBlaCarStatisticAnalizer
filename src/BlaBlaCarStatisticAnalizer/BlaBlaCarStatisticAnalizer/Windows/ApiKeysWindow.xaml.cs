using System.Threading.Tasks;
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace BlaBlaCarStatisticAnalizer.Windows
{
    /// <summary>
    /// Логика взаимодействия для ApiKeysWindow.xaml
    /// </summary>
    public partial class ApiKeysWindow : MetroWindow
    {
        public ApiKeysWindow()
        {
            ThemeManager.ChangeAppStyle(this,
                ThemeManager.GetAccent("Blue"),
                ThemeManager.GetAppTheme("BaseDark"));
            InitializeComponent();
            var context = new ApiKeysViewModel();
            var task = new Task(async () => await context.Update());
            task.Start();
            DataContext = context;
        }
    }
}
