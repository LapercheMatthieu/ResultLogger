using MatthL.ResultLogger.Core.Models;
using MatthL.ResultLogger.UI;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MatthL.ResultLogger.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Content = new LogViewer();

            test(true);
            test(false);
        }

        public Result test(bool value)
        {
            if (value)
            {
                return Result.Success();
            }
            else
            {
                return Result.Failure("Le test montre faux");
            }
        }
    }
}