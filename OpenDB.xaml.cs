using System;
using System.Collections.ObjectModel;
// using MySqlConnector;
using System.Data;
using System.Data.SqlClient;
using System.Net.Sockets;
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

namespace WpfApp_Krasotka
{
    /// <summary>
    /// Логика взаимодействия для OpenDB.xaml
    /// </summary>
    public partial class OpenDB : Window
    {
        public OpenDB()
        {

            InitializeComponent();

        }

        private void b_client_Click(object sender, RoutedEventArgs e)
        {
            Clients clients = new Clients();
            clients.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            clients.Show();
            this.Hide();
        }

        private void b_typeService_Click(object sender, RoutedEventArgs e)
        {
            ServTypes servTypes = new ServTypes();
            servTypes.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            servTypes.Show();
            this.Hide();
        }

        private void b_master_Click(object sender, RoutedEventArgs e)
        {
            Masters masters = new Masters();
            masters.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            masters.Show();
            this.Hide();
        }

        private void b_service_Click(object sender, RoutedEventArgs e)
        {
            Services services = new Services();
            services.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            services.Show();
            this.Hide();
        }

        private void b_appoint_Click(object sender, RoutedEventArgs e)
        {
            Appointments appointments = new Appointments();
            appointments.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            appointments.Show();
            this.Hide();
        }

        private void b_returnO_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            mainWindow.Show();
            this.Hide();
        }
    }
}
