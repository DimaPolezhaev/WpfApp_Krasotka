using System.Collections.ObjectModel;
using System.Reflection.PortableExecutable;
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
using System.Data.SqlClient;
using MySqlConnector;
using System.Data;
using System.Net.Sockets;
using System;

namespace WpfApp_Krasotka
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

        private void b_db_Click(object sender, RoutedEventArgs e)
        {
           OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Hide();
            
        }

        private void b_work_Click(object sender, RoutedEventArgs e)
        {
           Everyday everyday = new Everyday();
            everyday.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            everyday.Show();
            this.Hide();
        }

        private void b_exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}