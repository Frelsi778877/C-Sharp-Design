using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SudokuUnlimited
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Exit_Click(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Show_GameWindow(object sender, MouseButtonEventArgs e)
        {
            LoadGame loadWindow = new LoadGame();
            loadWindow.Show();
            //Game gameWindow = new Game();
            //gameWindow.Show();

            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, args) => this.Hide();
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}