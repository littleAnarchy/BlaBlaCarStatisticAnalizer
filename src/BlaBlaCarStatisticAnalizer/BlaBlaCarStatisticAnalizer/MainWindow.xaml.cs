using System.IO;
using System.Threading.Tasks;
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace BlaBlaCarStatisticAnalizer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            DataContext = new AnalizerViewModel(this);
            ThemeManager.ChangeAppStyle(this,
                ThemeManager.GetAccent("Emerald"),
                ThemeManager.GetAppTheme("BaseDark"));
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/" + "Trips");
            InitializeComponent();
            new Task(ApiKeyController.LoadKeys).Start();
        }
    }
}
