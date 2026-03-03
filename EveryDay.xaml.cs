using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MySqlConnector;
using System;
using System.Linq;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp_Krasotka
{
    public partial class Everyday : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";

        public ObservableCollection<ExpandedMasterItem> Masters { get; set; }
        public ObservableCollection<EverydayServiceItem> Services { get; set; }
        public ObservableCollection<EverydayServiceComboBox> QuickServices { get; set; }
        public ObservableCollection<string> WorkHours { get; set; }

        public Everyday()
        {
            Masters = new ObservableCollection<ExpandedMasterItem>();
            Services = new ObservableCollection<EverydayServiceItem>();
            QuickServices = new ObservableCollection<EverydayServiceComboBox>();
            WorkHours = new ObservableCollection<string>();

            InitializeComponent();
            DataContext = this;

            LoadServicesData();
            LoadMastersData();
            LoadQuickServices();
            LoadWorkHours();
        }

        // Классы данных
        public class ExpandedMasterItem
        {
            public string MasterName { get; set; }
            public string ServTypeName { get; set; }
            public string MasterTel { get; set; }
            public int ClientsCount { get; set; }
            public ObservableCollection<MasterClientItem> MasterClients { get; set; }
        }

        public class MasterClientItem
        {
            public string ClientName { get; set; }
            public string ClientTel { get; set; }
            public string ServiceName { get; set; }
            public DateTime AppointmentDate { get; set; }
            public int ClientCode { get; set; }
        }

        public class EverydayServiceItem
        {
            public string ServName { get; set; }
            public int ServPrice { get; set; }
            public int ServDuration { get; set; }
            public string ServTypeName { get; set; }
        }

        public class EverydayServiceComboBox
        {
            public int ServCode { get; set; }
            public string ServName { get; set; }
            public int ServPrice { get; set; }
        }

        // ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ДЛЯ COMBOBOX
        public class MasterComboBoxItem
        {
            public int MasterCode { get; set; }
            public string MasterName { get; set; }
        }

        public class ClientComboBoxItem
        {
            public int ClientCode { get; set; }
            public string ClientName { get; set; }
        }

        public class ServTypeComboBoxItem
        {
            public int ServTypeCode { get; set; }
            public string ServType { get; set; }
        }

        public class ServiceComboBoxItem
        {
            public int ServCode { get; set; }
            public string ServName { get; set; }
            public int ServTypeCode { get; set; }
        }

        // Класс для поиска клиентов
        public class ClientSearchItem
        {
            public int ClientCode { get; set; }
            public string ClientName { get; set; }
            public string ClientTel { get; set; }
            public string MasterName { get; set; }
            public string ServiceName { get; set; }
            public string ClientActivity { get; set; }
            public int AppointmentCount { get; set; }
            public DateTime LastAppointment { get; set; }
            public string DisplayInfo => $"{ClientName} | {ClientTel} | Мастер: {MasterName ?? "Нет записей"} | Активный: {ClientActivity} | Услуг: {AppointmentCount}";
        }

        // НОВЫЙ МЕТОД: Автоматический выбор мастера по типу услуги
        private MasterComboBoxItem GetAutoSelectedMaster(int servTypeCode)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    // Ищем мастера с соответствующей специализацией
                    string query = @"
                SELECT m.masterCode, m.masterName 
                FROM masters m 
                WHERE m.servTypeCode = @ServTypeCode 
                AND m.masterActivity = 'Да'
                ORDER BY m.masterCode
                LIMIT 1";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@ServTypeCode", servTypeCode);

                    var reader = comm.ExecuteReader();

                    if (reader.Read())
                    {
                        return new MasterComboBoxItem
                        {
                            MasterCode = reader.GetInt32("masterCode"),
                            MasterName = reader.GetString("masterName")
                        };
                    }

                    // Если мастер с такой специализацией не найден, берем первого активного мастера
                    reader.Close();

                    string fallbackQuery = @"
                SELECT masterCode, masterName 
                FROM masters 
                WHERE masterActivity = 'Да'
                ORDER BY masterCode
                LIMIT 1";

                    MySqlCommand fallbackComm = new MySqlCommand(fallbackQuery, conn);
                    var fallbackReader = fallbackComm.ExecuteReader();

                    if (fallbackReader.Read())
                    {
                        return new MasterComboBoxItem
                        {
                            MasterCode = fallbackReader.GetInt32("masterCode"),
                            MasterName = fallbackReader.GetString("masterName")
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при автоматическом выборе мастера: {ex.Message}");
            }

            return null;
        }

        // Быстрая запись - открывает только окно добавления записи
        // Быстрая запись - автоматический выбор мастера
        private void b_quickBook_Click(object sender, RoutedEventArgs e)
        {
            var selectedService = cb_quickService.SelectedItem as EverydayServiceComboBox;
            if (selectedService == null)
            {
                MessageBox.Show("Выберите услугу для записи!");
                return;
            }

            // Получаем тип услуги для выбранной услуги
            int serviceTypeCode = GetServiceTypeCode(selectedService.ServCode);

            // Автоматически выбираем мастера
            var autoSelectedMaster = GetAutoSelectedMaster(serviceTypeCode);

            if (autoSelectedMaster == null)
            {
                MessageBox.Show("Не найден подходящий мастер для выбранной услуги!");
                return;
            }

            // Показываем окно с автоматически выбранным мастером
            ShowAddAppointmentWindow(selectedService, autoSelectedMaster);
        }

        // Окно добавления записи
        // Окно добавления записи с автоматическим выбором мастера
        private void ShowAddAppointmentWindow(EverydayServiceComboBox selectedService = null, MasterComboBoxItem autoSelectedMaster = null)
        {
            try
            {
                int nextCode = GetNextAppointmentCode();

                Window appointmentWindow = new Window()
                {
                    Title = "Добавление записи",
                    Width = 500,
                    Height = 850, // Увеличил высоту для нового поля
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = Brushes.White
                };

                StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

                // Заголовок
                TextBlock header = new TextBlock()
                {
                    Text = "Добавление записи на услугу",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20),
                    Foreground = Brushes.Navy
                };
                mainPanel.Children.Add(header);

                // Поле кода записи
                TextBox codeTextBox = new TextBox()
                {
                    Text = nextCode.ToString(),
                    IsReadOnly = true,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Код записи:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(codeTextBox);

                // Загружаем данные для ComboBox
                var masters = LoadMastersForComboBox();
                var servTypes = LoadServiceTypesForComboBox();
                var services = LoadServicesForComboBox();

                // Поле выбора мастера
                ComboBox masterComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "MasterName",
                    ItemsSource = masters
                };

                // АВТОМАТИЧЕСКИ ВЫБИРАЕМ МАСТЕРА
                if (autoSelectedMaster != null)
                {
                    foreach (MasterComboBoxItem master in masterComboBox.Items)
                    {
                        if (master.MasterCode == autoSelectedMaster.MasterCode)
                        {
                            masterComboBox.SelectedItem = master;
                            break;
                        }
                    }
                }

                mainPanel.Children.Add(new Label() { Content = "Мастер:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(masterComboBox);

                // Поле ввода имени клиента
                TextBox clientNameTextBox = new TextBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Введите имя клиента"
                };
                mainPanel.Children.Add(new Label() { Content = "Имя клиента:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(clientNameTextBox);

                // Поле ввода телефона клиента
                TextBox clientTelTextBox = new TextBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Введите телефон клиента"
                };
                mainPanel.Children.Add(new Label() { Content = "Телефон клиента:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(clientTelTextBox);

                // Поле выбора типа услуги
                ComboBox servTypeComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "ServType",
                    ItemsSource = servTypes
                };

                if (selectedService != null)
                {
                    var serviceTypeCode = GetServiceTypeCode(selectedService.ServCode);
                    foreach (ServTypeComboBoxItem item in servTypeComboBox.Items)
                    {
                        if (item.ServTypeCode == serviceTypeCode)
                        {
                            servTypeComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                else if (servTypes.Count > 0)
                {
                    servTypeComboBox.SelectedIndex = 0;
                }

                mainPanel.Children.Add(new Label() { Content = "Тип услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(servTypeComboBox);

                // Поле выбора услуги
                ComboBox serviceComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "ServName"
                };

                servTypeComboBox.SelectionChanged += (s, args) =>
                {
                    var selectedServType = servTypeComboBox.SelectedItem as ServTypeComboBoxItem;
                    if (selectedServType != null)
                    {
                        serviceComboBox.ItemsSource = services.Where(serv => serv.ServTypeCode == selectedServType.ServTypeCode).ToList();

                        // АВТОМАТИЧЕСКИ ВЫБИРАЕМ МАСТЕРА ПРИ ИЗМЕНЕНИИ ТИПА УСЛУГИ
                        var newAutoMaster = GetAutoSelectedMaster(selectedServType.ServTypeCode);
                        if (newAutoMaster != null)
                        {
                            foreach (MasterComboBoxItem master in masterComboBox.Items)
                            {
                                if (master.MasterCode == newAutoMaster.MasterCode)
                                {
                                    masterComboBox.SelectedItem = master;
                                    break;
                                }
                            }
                        }
                    }
                };

                var initialServType = servTypeComboBox.SelectedItem as ServTypeComboBoxItem;
                if (initialServType != null)
                {
                    serviceComboBox.ItemsSource = services.Where(serv => serv.ServTypeCode == initialServType.ServTypeCode).ToList();
                }

                if (selectedService != null)
                {
                    foreach (ServiceComboBoxItem item in serviceComboBox.Items)
                    {
                        if (item.ServCode == selectedService.ServCode)
                        {
                            serviceComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                mainPanel.Children.Add(new Label() { Content = "Услуга:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(serviceComboBox);

                // Поле даты записи
                DatePicker datePicker = new DatePicker()
                {
                    SelectedDate = DateTime.Today,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Дата записи:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(datePicker);

                // Поле времени начала
                TextBox queueFromTextBox = new TextBox()
                {
                    Text = "9",
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Время начала в формате часа (9-18)"
                };
                mainPanel.Children.Add(new Label() { Content = "Время начала (час):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(queueFromTextBox);

                // Поле времени окончания
                TextBox queueToTextBox = new TextBox()
                {
                    Text = "10",
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Время окончания в формате часа (10-19)"
                };
                mainPanel.Children.Add(new Label() { Content = "Время окончания (час):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(queueToTextBox);

                // ДОБАВЛЕНО: ПОЛЕ ВЫБОРА АКТИВНОСТИ ЗАПИСИ
                ComboBox activityComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 20),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };

                // Добавляем варианты активности
                activityComboBox.Items.Add("Да");
                activityComboBox.Items.Add("Нет");
                activityComboBox.SelectedItem = "Да"; // Значение по умолчанию

                mainPanel.Children.Add(new Label() { Content = "Активный:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(activityComboBox);

                // Кнопки
                StackPanel buttonPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                Button saveButton = new Button()
                {
                    Content = "Сохранить",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.DodgerBlue,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                saveButton.Click += (s, args) =>
                {
                    var selectedMaster = masterComboBox.SelectedItem as MasterComboBoxItem;
                    var selectedServType = servTypeComboBox.SelectedItem as ServTypeComboBoxItem;
                    var selectedServiceItem = serviceComboBox.SelectedItem as ServiceComboBoxItem;

                    if (selectedMaster == null)
                    {
                        MessageBox.Show("Выберите мастера!");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(clientNameTextBox.Text))
                    {
                        MessageBox.Show("Введите имя клиента!");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(clientTelTextBox.Text))
                    {
                        MessageBox.Show("Введите телефон клиента!");
                        return;
                    }

                    if (selectedServType == null)
                    {
                        MessageBox.Show("Выберите тип услуги!");
                        return;
                    }

                    if (selectedServiceItem == null)
                    {
                        MessageBox.Show("Выберите услугу!");
                        return;
                    }

                    if (!datePicker.SelectedDate.HasValue)
                    {
                        MessageBox.Show("Выберите дату записи!");
                        return;
                    }

                    if (!int.TryParse(queueFromTextBox.Text, out int queueFrom) || queueFrom < 9 || queueFrom > 18)
                    {
                        MessageBox.Show("Введите корректное время начала (9-18)!");
                        return;
                    }

                    if (!int.TryParse(queueToTextBox.Text, out int queueTo) || queueTo < 10 || queueTo > 19 || queueTo <= queueFrom)
                    {
                        MessageBox.Show("Введите корректное время окончания (10-19, должно быть больше времени начала)!");
                        return;
                    }

                    // ДОБАВЛЕНО: ПОЛУЧАЕМ ВЫБРАННУЮ АКТИВНОСТЬ
                    string appointmentActivity = activityComboBox.SelectedItem?.ToString() ?? "Да";

                    // Сохраняем или находим клиента
                    int clientCode = GetOrCreateClient(clientNameTextBox.Text, clientTelTextBox.Text);

                    SaveAppointment(nextCode, selectedMaster.MasterCode, clientCode,
                        selectedServType.ServTypeCode, selectedServiceItem.ServCode, queueFrom, queueTo,
                        datePicker.SelectedDate.Value, appointmentActivity, appointmentWindow);
                };

                Button closeButton = new Button()
                {
                    Content = "Закрыть",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.Gray,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                closeButton.Click += (s, args) => appointmentWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(closeButton);
                mainPanel.Children.Add(buttonPanel);

                appointmentWindow.Content = mainPanel;
                appointmentWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна: {ex.Message}");
            }
        }

        // Метод для поиска или создания клиента
        private int GetOrCreateClient(string clientName, string clientTel)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    // Сначала ищем клиента по имени и телефону
                    string findQuery = "SELECT clientCode FROM clients WHERE clientName = @ClientName AND clientTel = @ClientTel";
                    MySqlCommand findComm = new MySqlCommand(findQuery, conn);
                    findComm.Parameters.AddWithValue("@ClientName", clientName);
                    findComm.Parameters.AddWithValue("@ClientTel", clientTel);

                    var existingClient = findComm.ExecuteScalar();
                    if (existingClient != null)
                    {
                        return Convert.ToInt32(existingClient);
                    }

                    // Если клиент не найден, создаем нового
                    int nextClientCode = GetNextClientCode();

                    string insertQuery = @"INSERT INTO clients (clientCode, clientName, clientTel) 
                               VALUES (@ClientCode, @ClientName, @ClientTel)";

                    MySqlCommand insertComm = new MySqlCommand(insertQuery, conn);
                    insertComm.Parameters.AddWithValue("@ClientCode", nextClientCode);
                    insertComm.Parameters.AddWithValue("@ClientName", clientName);
                    insertComm.Parameters.AddWithValue("@ClientTel", clientTel);

                    insertComm.ExecuteNonQuery();
                    return nextClientCode;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при работе с клиентом: {ex.Message}");
                return 1; // Возвращаем код по умолчанию в случае ошибки
            }
        }

        // Метод для получения следующего кода клиента
        private int GetNextClientCode()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT MAX(clientCode) FROM clients";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var result = comm.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        return 1;
                    else
                        return Convert.ToInt32(result) + 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                return 1;
            }
        }

        // Создание записи для существующего клиента
        // Создание записи для существующего клиента - ИСПРАВЛЕННАЯ ВЕРСИЯ
        private void CreateAppointmentForClient_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is MasterClientItem client)
            {
                // Используем основное окно добавления записи, но с предзаполненными данными клиента
                ShowAddAppointmentWindowWithClientData(client.ClientName, client.ClientTel);
            }
        }

        // НОВЫЙ МЕТОД: Открывает основное окно добавления записи с предзаполненными данными клиента
        private void ShowAddAppointmentWindowWithClientData(string clientName, string clientTel)
        {
            try
            {
                int nextCode = GetNextAppointmentCode();

                Window appointmentWindow = new Window()
                {
                    Title = "Добавление записи",
                    Width = 500,
                    Height = 850,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = Brushes.White
                };

                StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

                // Заголовок
                TextBlock header = new TextBlock()
                {
                    Text = "Добавление записи на услугу",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20),
                    Foreground = Brushes.Navy
                };
                mainPanel.Children.Add(header);

                // Поле кода записи
                TextBox codeTextBox = new TextBox()
                {
                    Text = nextCode.ToString(),
                    IsReadOnly = true,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Код записи:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(codeTextBox);

                // Загружаем данные для ComboBox
                var masters = LoadMastersForComboBox();
                var servTypes = LoadServiceTypesForComboBox();
                var services = LoadServicesForComboBox();

                // Поле выбора мастера
                ComboBox masterComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "MasterName",
                    ItemsSource = masters
                };

                mainPanel.Children.Add(new Label() { Content = "Мастер:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(masterComboBox);

                // Поле ввода имени клиента - АВТОМАТИЧЕСКИ ЗАПОЛНЯЕМ
                TextBox clientNameTextBox = new TextBox()
                {
                    Text = clientName,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Имя клиента заполнено автоматически"
                };
                mainPanel.Children.Add(new Label() { Content = "Имя клиента:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(clientNameTextBox);

                // Поле ввода телефона клиента - АВТОМАТИЧЕСКИ ЗАПОЛНЯЕМ
                TextBox clientTelTextBox = new TextBox()
                {
                    Text = clientTel,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Телефон клиента заполнен автоматически"
                };
                mainPanel.Children.Add(new Label() { Content = "Телефон клиента:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(clientTelTextBox);

                // Поле выбора типа услуги
                ComboBox servTypeComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "ServType",
                    ItemsSource = servTypes
                };

                if (servTypes.Count > 0)
                {
                    servTypeComboBox.SelectedIndex = 0;
                }

                mainPanel.Children.Add(new Label() { Content = "Тип услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(servTypeComboBox);

                // Поле выбора услуги
                ComboBox serviceComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "ServName"
                };

                servTypeComboBox.SelectionChanged += (s, args) =>
                {
                    var selectedServType = servTypeComboBox.SelectedItem as ServTypeComboBoxItem;
                    if (selectedServType != null)
                    {
                        serviceComboBox.ItemsSource = services.Where(serv => serv.ServTypeCode == selectedServType.ServTypeCode).ToList();

                        // АВТОМАТИЧЕСКИ ВЫБИРАЕМ МАСТЕРА ПРИ ИЗМЕНЕНИИ ТИПА УСЛУГИ
                        var newAutoMaster = GetAutoSelectedMaster(selectedServType.ServTypeCode);
                        if (newAutoMaster != null)
                        {
                            foreach (MasterComboBoxItem master in masterComboBox.Items)
                            {
                                if (master.MasterCode == newAutoMaster.MasterCode)
                                {
                                    masterComboBox.SelectedItem = master;
                                    break;
                                }
                            }
                        }
                    }
                };

                var initialServType = servTypeComboBox.SelectedItem as ServTypeComboBoxItem;
                if (initialServType != null)
                {
                    serviceComboBox.ItemsSource = services.Where(serv => serv.ServTypeCode == initialServType.ServTypeCode).ToList();

                    // АВТОМАТИЧЕСКИ ВЫБИРАЕМ МАСТЕРА ПРИ ПЕРВОНАЧАЛЬНОЙ ЗАГРУЗКЕ
                    var initialAutoMaster = GetAutoSelectedMaster(initialServType.ServTypeCode);
                    if (initialAutoMaster != null)
                    {
                        foreach (MasterComboBoxItem master in masterComboBox.Items)
                        {
                            if (master.MasterCode == initialAutoMaster.MasterCode)
                            {
                                masterComboBox.SelectedItem = master;
                                break;
                            }
                        }
                    }
                }

                mainPanel.Children.Add(new Label() { Content = "Услуга:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(serviceComboBox);

                // Поле даты записи
                DatePicker datePicker = new DatePicker()
                {
                    SelectedDate = DateTime.Today,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Дата записи:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(datePicker);

                // Поле времени начала
                TextBox queueFromTextBox = new TextBox()
                {
                    Text = "9",
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Время начала в формате часа (9-18)"
                };
                mainPanel.Children.Add(new Label() { Content = "Время начала (час):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(queueFromTextBox);

                // Поле времени окончания
                TextBox queueToTextBox = new TextBox()
                {
                    Text = "10",
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Время окончания в формате часа (10-19)"
                };
                mainPanel.Children.Add(new Label() { Content = "Время окончания (час):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(queueToTextBox);

                // ПОЛЕ ВЫБОРА АКТИВНОСТИ ЗАПИСИ
                ComboBox activityComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 20),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };

                // Добавляем варианты активности
                activityComboBox.Items.Add("Да");
                activityComboBox.Items.Add("Нет");
                activityComboBox.SelectedItem = "Да"; // Значение по умолчанию

                mainPanel.Children.Add(new Label() { Content = "Активный:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(activityComboBox);

                // Кнопки
                StackPanel buttonPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                Button saveButton = new Button()
                {
                    Content = "Сохранить",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.DodgerBlue,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                saveButton.Click += (s, args) =>
                {
                    var selectedMaster = masterComboBox.SelectedItem as MasterComboBoxItem;
                    var selectedServType = servTypeComboBox.SelectedItem as ServTypeComboBoxItem;
                    var selectedServiceItem = serviceComboBox.SelectedItem as ServiceComboBoxItem;

                    if (selectedMaster == null)
                    {
                        MessageBox.Show("Выберите мастера!");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(clientNameTextBox.Text))
                    {
                        MessageBox.Show("Введите имя клиента!");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(clientTelTextBox.Text))
                    {
                        MessageBox.Show("Введите телефон клиента!");
                        return;
                    }

                    if (selectedServType == null)
                    {
                        MessageBox.Show("Выберите тип услуги!");
                        return;
                    }

                    if (selectedServiceItem == null)
                    {
                        MessageBox.Show("Выберите услугу!");
                        return;
                    }

                    if (!datePicker.SelectedDate.HasValue)
                    {
                        MessageBox.Show("Выберите дату записи!");
                        return;
                    }

                    if (!int.TryParse(queueFromTextBox.Text, out int queueFrom) || queueFrom < 9 || queueFrom > 18)
                    {
                        MessageBox.Show("Введите корректное время начала (9-18)!");
                        return;
                    }

                    if (!int.TryParse(queueToTextBox.Text, out int queueTo) || queueTo < 10 || queueTo > 19 || queueTo <= queueFrom)
                    {
                        MessageBox.Show("Введите корректное время окончания (10-19, должно быть больше времени начала)!");
                        return;
                    }

                    // ПОЛУЧАЕМ ВЫБРАННУЮ АКТИВНОСТЬ ИЗ COMBOBOX
                    string appointmentActivity = activityComboBox.SelectedItem?.ToString() ?? "Да";

                    // Сохраняем или находим клиента
                    int clientCode = GetOrCreateClient(clientNameTextBox.Text, clientTelTextBox.Text);

                    // ОБНОВЛЕННЫЙ ВЫЗОВ С ДОБАВЛЕНИЕМ АКТИВНОСТИ
                    SaveAppointment(nextCode, selectedMaster.MasterCode, clientCode,
                        selectedServType.ServTypeCode, selectedServiceItem.ServCode, queueFrom, queueTo,
                        datePicker.SelectedDate.Value, appointmentActivity, appointmentWindow);
                };

                Button closeButton = new Button()
                {
                    Content = "Закрыть",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.Gray,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                closeButton.Click += (s, args) => appointmentWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(closeButton);
                mainPanel.Children.Add(buttonPanel);

                appointmentWindow.Content = mainPanel;
                appointmentWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна: {ex.Message}");
            }
        }

        private void ShowAddAppointmentWindowForClient(MasterClientItem client)
        {
            try
            {
                int nextCode = GetNextAppointmentCode();

                Window appointmentWindow = new Window()
                {
                    Title = "Создание записи для клиента",
                    Width = 450,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = Brushes.White
                };

                StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

                // Заголовок
                TextBlock header = new TextBlock()
                {
                    Text = "➕ Создание записи для клиента",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20),
                    Foreground = Brushes.Navy
                };
                mainPanel.Children.Add(header);

                // Информация о клиенте
                Border clientInfo = new Border()
                {
                    Background = Brushes.LightBlue,
                    BorderBrush = Brushes.Blue,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 15)
                };

                StackPanel clientPanel = new StackPanel();
                clientPanel.Children.Add(new TextBlock()
                {
                    Text = "📋 Информация о клиенте",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 5)
                });

                // Поле имени клиента
                TextBox clientNameTextBox = new TextBox()
                {
                    Text = client.ClientName,
                    IsReadOnly = true,
                    Margin = new Thickness(0, 0, 0, 5),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue
                };
                clientPanel.Children.Add(new Label() { Content = "Имя клиента:", FontWeight = FontWeights.SemiBold });
                clientPanel.Children.Add(clientNameTextBox);

                // Поле телефона клиента
                TextBox clientTelTextBox = new TextBox()
                {
                    Text = client.ClientTel,
                    IsReadOnly = true,
                    Margin = new Thickness(0, 0, 0, 0),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue
                };
                clientPanel.Children.Add(new Label() { Content = "Телефон:", FontWeight = FontWeights.SemiBold });
                clientPanel.Children.Add(clientTelTextBox);

                clientInfo.Child = clientPanel;
                mainPanel.Children.Add(clientInfo);

                // Загружаем данные для ComboBox
                var masters = LoadMastersForComboBox();
                var servTypes = LoadServiceTypesForComboBox();
                var services = LoadServicesForComboBox();

                // Поле выбора мастера
                ComboBox masterComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "MasterName",
                    ItemsSource = masters
                };

                // ДОБАВЛЯЕМ ИНФОРМАЦИЮ ОБ АВТОМАТИЧЕСКОМ ВЫБОРЕ
                TextBlock autoMasterInfo = new TextBlock()
                {
                    Text = "Мастер будет автоматически выбран после выбора типа услуги",
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Foreground = Brushes.Green,
                    Margin = new Thickness(0, -5, 0, 5)
                };
                mainPanel.Children.Add(autoMasterInfo);

                mainPanel.Children.Add(new Label() { Content = "Мастер:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(masterComboBox);

                // Поле выбора типа услуги
                ComboBox servTypeComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "ServType",
                    ItemsSource = servTypes
                };

                if (servTypes.Count > 0)
                {
                    servTypeComboBox.SelectedIndex = 0;
                }

                mainPanel.Children.Add(new Label() { Content = "Тип услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(servTypeComboBox);

                // Поле выбора услуги
                ComboBox serviceComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "ServName"
                };

                servTypeComboBox.SelectionChanged += (s, args) =>
                {
                    var selectedServType = servTypeComboBox.SelectedItem as ServTypeComboBoxItem;
                    if (selectedServType != null)
                    {
                        serviceComboBox.ItemsSource = services.Where(serv => serv.ServTypeCode == selectedServType.ServTypeCode).ToList();

                        // АВТОМАТИЧЕСКИ ВЫБИРАЕМ МАСТЕРА ПРИ ИЗМЕНЕНИИ ТИПА УСЛУГИ
                        var newAutoMaster = GetAutoSelectedMaster(selectedServType.ServTypeCode);
                        if (newAutoMaster != null)
                        {
                            foreach (MasterComboBoxItem master in masterComboBox.Items)
                            {
                                if (master.MasterCode == newAutoMaster.MasterCode)
                                {
                                    masterComboBox.SelectedItem = master;
                                    break;
                                }
                            }
                        }
                    }
                };

                var initialServType = servTypeComboBox.SelectedItem as ServTypeComboBoxItem;
                if (initialServType != null)
                {
                    serviceComboBox.ItemsSource = services.Where(serv => serv.ServTypeCode == initialServType.ServTypeCode).ToList();

                    // АВТОМАТИЧЕСКИ ВЫБИРАЕМ МАСТЕРА ПРИ ПЕРВОНАЧАЛЬНОЙ ЗАГРУЗКЕ
                    var initialAutoMaster = GetAutoSelectedMaster(initialServType.ServTypeCode);
                    if (initialAutoMaster != null)
                    {
                        foreach (MasterComboBoxItem master in masterComboBox.Items)
                        {
                            if (master.MasterCode == initialAutoMaster.MasterCode)
                            {
                                masterComboBox.SelectedItem = master;
                                break;
                            }
                        }
                    }
                }

                mainPanel.Children.Add(new Label() { Content = "Услуга:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(serviceComboBox);

                // Поле даты записи
                DatePicker datePicker = new DatePicker()
                {
                    SelectedDate = DateTime.Today,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Дата записи:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(datePicker);

                // Поле времени начала
                TextBox queueFromTextBox = new TextBox()
                {
                    Text = "9",
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Время начала в формате часа (9-18)"
                };
                mainPanel.Children.Add(new Label() { Content = "Время начала (час):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(queueFromTextBox);

                // Поле времени окончания
                TextBox queueToTextBox = new TextBox()
                {
                    Text = "10",
                    Margin = new Thickness(0, 0, 0, 20),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Время окончания в формате часа (10-19)"
                };
                mainPanel.Children.Add(new Label() { Content = "Время окончания (час):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(queueToTextBox);

                // Кнопки
                StackPanel buttonPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                Button saveButton = new Button()
                {
                    Content = "💾 Записать",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.DodgerBlue,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                saveButton.Click += (s, args) =>
                {
                    var selectedMaster = masterComboBox.SelectedItem as MasterComboBoxItem;
                    var selectedServType = servTypeComboBox.SelectedItem as ServTypeComboBoxItem;
                    var selectedServiceItem = serviceComboBox.SelectedItem as ServiceComboBoxItem;

                    if (selectedMaster == null)
                    {
                        MessageBox.Show("Выберите мастера!");
                        return;
                    }

                    if (selectedServType == null)
                    {
                        MessageBox.Show("Выберите тип услуги!");
                        return;
                    }

                    if (selectedServiceItem == null)
                    {
                        MessageBox.Show("Выберите услугу!");
                        return;
                    }

                    if (!datePicker.SelectedDate.HasValue)
                    {
                        MessageBox.Show("Выберите дату записи!");
                        return;
                    }

                    if (!int.TryParse(queueFromTextBox.Text, out int queueFrom) || queueFrom < 9 || queueFrom > 18)
                    {
                        MessageBox.Show("Введите корректное время начала (9-18)!");
                        return;
                    }

                    if (!int.TryParse(queueToTextBox.Text, out int queueTo) || queueTo < 10 || queueTo > 19 || queueTo <= queueFrom)
                    {
                        MessageBox.Show("Введите корректное время окончания (10-19, должно быть больше времени начала)!");
                        return;
                    }

                    SaveAppointment(nextCode, selectedMaster.MasterCode, client.ClientCode,
    selectedServType.ServTypeCode, selectedServiceItem.ServCode, queueFrom, queueTo,
    datePicker.SelectedDate.Value, "Да", appointmentWindow);
                };

                Button closeButton = new Button()
                {
                    Content = "❌ Закрыть",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.Gray,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                closeButton.Click += (s, args) => appointmentWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(closeButton);
                mainPanel.Children.Add(buttonPanel);

                appointmentWindow.Content = mainPanel;
                appointmentWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна: {ex.Message}");
            }
        }

        // ПОИСК КЛИЕНТА
        private void b_searchClient_Click(object sender, RoutedEventArgs e)
        {
            ShowSearchClientWindow();
        }

        private void ShowSearchClientWindow()
        {
            try
            {
                Window searchWindow = new Window()
                {
                    Title = "Поиск клиента",
                    Width = 600,
                    Height = 560,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = Brushes.White
                };

                StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

                // Заголовок
                TextBlock header = new TextBlock()
                {
                    Text = "🔍 Поиск клиента",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20),
                    Foreground = Brushes.Navy
                };
                mainPanel.Children.Add(header);

                // Поле поиска
                StackPanel searchPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
                TextBox searchTextBox = new TextBox()
                {
                    Width = 452,
                    Height = 40,
                    FontSize = 14,
                    Padding = new Thickness(10),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Введите имя, телефон или комментарий клиента"
                };

                Button searchButton = new Button()
                {
                    Content = "Найти",
                    Width = 80,
                    Height = 35,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.DodgerBlue,
                    Foreground = Brushes.White,
                    Margin = new Thickness(10, 0, 0, 0)
                };

                searchPanel.Children.Add(searchTextBox);
                searchPanel.Children.Add(searchButton);
                mainPanel.Children.Add(searchPanel);

                // Список результатов
                ListBox resultsListBox = new ListBox()
                {
                    Height = 320,
                    Margin = new Thickness(0, 0, 0, 15),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    FontSize = 12
                };

                var allClients = LoadAllClientsWithDetails();
                resultsListBox.ItemsSource = allClients;
                resultsListBox.DisplayMemberPath = "DisplayInfo";
                resultsListBox.ItemContainerStyle = new Style(typeof(ListBoxItem));
                resultsListBox.ItemContainerStyle.Setters.Add(new Setter(ListBoxItem.PaddingProperty, new Thickness(5)));
                resultsListBox.ItemContainerStyle.Setters.Add(new Setter(ListBoxItem.MarginProperty, new Thickness(2)));

                mainPanel.Children.Add(resultsListBox);

                // Кнопки действий
                StackPanel actionPanel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

                Button viewDetailsButton = new Button()
                {
                    Content = "👁️ Просмотреть",
                    Width = 140,
                    Height = 35,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.Orange,
                    Foreground = Brushes.White,
                    Margin = new Thickness(10)
                };

                Button closeButton = new Button()
                {
                    Content = "Закрыть",
                    Width = 100,
                    Height = 35,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.Gray,
                    Foreground = Brushes.White,
                    Margin = new Thickness(10)
                };

                // Обработчики кнопок
                searchButton.Click += (s, args) =>
                {
                    var searchText = searchTextBox.Text.ToLower();
                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        resultsListBox.ItemsSource = allClients;
                    }
                    else
                    {
                        var filteredClients = allClients.Where(c =>
                            c.ClientName.ToLower().Contains(searchText) ||
                            c.ClientTel.Contains(searchText) ||
                            (c.MasterName != null && c.MasterName.ToLower().Contains(searchText)) ||
                            c.ClientActivity.ToLower().Contains(searchText)
                        ).ToList();
                        resultsListBox.ItemsSource = filteredClients;
                    }
                };

                viewDetailsButton.Click += (s, args) =>
                {
                    if (resultsListBox.SelectedItem is ClientSearchItem selectedClient)
                    {
                        ShowClientDetails(selectedClient);
                    }
                    else
                    {
                        MessageBox.Show("Выберите клиента для просмотра!");
                    }
                };

                closeButton.Click += (s, args) => searchWindow.Close();

                actionPanel.Children.Add(viewDetailsButton);
                actionPanel.Children.Add(closeButton);
                mainPanel.Children.Add(actionPanel);

                searchWindow.Content = mainPanel;
                searchWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске клиента: {ex.Message}");
            }
        }

        private List<ClientSearchItem> LoadAllClientsWithDetails()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT 
                    c.clientCode,
                    c.clientName,
                    c.clientTel,
                    c.clientsActivity,
                    COUNT(a.appCode) as AppointmentCount,
                    MAX(a.appDate) as LastAppointment,
                    GROUP_CONCAT(DISTINCT m.masterName) as MasterNames,
                    GROUP_CONCAT(DISTINCT s.servName) as ServiceNames
                FROM clients c
                LEFT JOIN appointments a ON c.clientCode = a.clientCode AND a.AppointmentActivity = 'Да'  -- ДОБАВЛЕНА ПРОВЕРКА
                LEFT JOIN masters m ON a.masterCode = m.masterCode
                LEFT JOIN services s ON a.servCode = s.servCode
                GROUP BY c.clientCode, c.clientName, c.clientTel, c.clientsActivity
                ORDER BY c.clientName";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var reader = comm.ExecuteReader();

                    var clients = new List<ClientSearchItem>();
                    while (reader.Read())
                    {
                        string masterNames = reader.IsDBNull(reader.GetOrdinal("MasterNames")) ? "" : reader.GetString("MasterNames");
                        string serviceNames = reader.IsDBNull(reader.GetOrdinal("ServiceNames")) ? "" : reader.GetString("ServiceNames");

                        string primaryMaster = masterNames.Split(',').FirstOrDefault() ?? "";

                        clients.Add(new ClientSearchItem
                        {
                            ClientCode = reader.GetInt32("clientCode"),
                            ClientName = reader.GetString("clientName"),
                            ClientTel = reader.GetString("clientTel"),
                            ClientActivity = reader.GetString("clientsActivity"),
                            MasterName = primaryMaster,
                            ServiceName = serviceNames,
                            AppointmentCount = reader.GetInt32("AppointmentCount"),
                            LastAppointment = reader.IsDBNull(reader.GetOrdinal("LastAppointment")) ? DateTime.MinValue : reader.GetDateTime("LastAppointment")
                        });
                    }
                    return clients;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}");
                return new List<ClientSearchItem>();
            }
        }

        private void ShowClientDetails(ClientSearchItem client)
        {
            Window detailsWindow = new Window()
            {
                Title = $"Информация о клиенте: {client.ClientName}",
                Width = 500,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = Brushes.White
            };

            StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

            // Заголовок
            TextBlock header = new TextBlock()
            {
                Text = $"👤 {client.ClientName}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = Brushes.Navy
            };
            mainPanel.Children.Add(header);

            // Информация о клиенте
            StackPanel infoPanel = new StackPanel() { Margin = new Thickness(0, 0, 0, 15) };

            infoPanel.Children.Add(CreateInfoRow("📞 Телефон:", client.ClientTel));
            infoPanel.Children.Add(CreateInfoRow("📊 Всего записей:", client.AppointmentCount.ToString()));
            infoPanel.Children.Add(CreateInfoRow("✅ Активный:", client.ClientActivity));

            if (client.LastAppointment != DateTime.MinValue)
                infoPanel.Children.Add(CreateInfoRow("📅 Последняя запись:", client.LastAppointment.ToString("dd.MM.yyyy")));

            if (!string.IsNullOrEmpty(client.MasterName))
                infoPanel.Children.Add(CreateInfoRow("👨‍💼 Основной мастер:", client.MasterName));

            if (!string.IsNullOrEmpty(client.ServiceName))
                infoPanel.Children.Add(CreateInfoRow("💅 Услуги:", client.ServiceName));

            mainPanel.Children.Add(infoPanel);

            // Кнопки действий
            StackPanel buttonPanel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            Button createAppointmentButton = new Button()
            {
                Content = "➕ Создать запись",
                Width = 150,
                Height = 35,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Background = Brushes.DodgerBlue,
                Foreground = Brushes.White,
                Margin = new Thickness(10)
            };

            Button closeButton = new Button()
            {
                Content = "Закрыть",
                Width = 100,
                Height = 35,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Background = Brushes.Gray,
                Foreground = Brushes.White,
                Margin = new Thickness(10)
            };

            createAppointmentButton.Click += (s, args) =>
            {
                ShowAddAppointmentWindowForSearchClient(client);
                detailsWindow.Close();
            };

            closeButton.Click += (s, args) => detailsWindow.Close();

            buttonPanel.Children.Add(createAppointmentButton);
            buttonPanel.Children.Add(closeButton);
            mainPanel.Children.Add(buttonPanel);

            detailsWindow.Content = mainPanel;
            detailsWindow.ShowDialog();
        }

        private StackPanel CreateInfoRow(string label, string value)
        {
            var panel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            panel.Children.Add(new TextBlock() { Text = label, FontWeight = FontWeights.Bold, Width = 150 });
            panel.Children.Add(new TextBlock() { Text = value });
            return panel;
        }

        private void ShowAddAppointmentWindowForSearchClient(ClientSearchItem client)
        {
            // Используем обновленный метод с предзаполненными данными и автоматическим выбором мастера
            ShowAddAppointmentWindowWithClientData(client.ClientName, client.ClientTel);
        }

        // ОБНОВЛЕННЫЙ КАТАЛОГ УСЛУГ (уже и только кнопка Закрыть)
        private void b_fullCatalog_Click(object sender, RoutedEventArgs e)
        {
            ShowFullCatalogWindow();
        }

        private void ShowFullCatalogWindow()
        {
            try
            {
                Window catalogWindow = new Window()
                {
                    Title = "Полный каталог услуг",
                    Width = 400,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = Brushes.White,
                    ResizeMode = ResizeMode.NoResize
                };

                ScrollViewer scrollViewer = new ScrollViewer()
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };

                StackPanel mainPanel = new StackPanel() { Margin = new Thickness(15) };

                TextBlock header = new TextBlock()
                {
                    Text = "📋 Полный каталог услуг",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 15),
                    Foreground = Brushes.Navy
                };
                mainPanel.Children.Add(header);

                LoadServicesByType(mainPanel);

                // Только кнопка Закрыть
                StackPanel buttonPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                };

                Button closeButton = new Button()
                {
                    Content = "❌ Закрыть",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.Gray,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                closeButton.Click += (s, args) => catalogWindow.Close();

                buttonPanel.Children.Add(closeButton);
                mainPanel.Children.Add(buttonPanel);

                scrollViewer.Content = mainPanel;
                catalogWindow.Content = scrollViewer;
                catalogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // Остальные методы остаются без изменений
        private void LoadServicesByType(StackPanel parentPanel)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string queryTypes = "SELECT servTypeCode, servType FROM servTypes WHERE servTypeActivity = 'Да' ORDER BY servType";
                    MySqlCommand commTypes = new MySqlCommand(queryTypes, conn);
                    var readerTypes = commTypes.ExecuteReader();

                    while (readerTypes.Read())
                    {
                        int typeCode = readerTypes.GetInt32("servTypeCode");
                        string typeName = readerTypes.GetString("servType");
                        CreateServiceTypeSection(parentPanel, typeCode, typeName);
                    }
                    readerTypes.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки каталога: {ex.Message}");
            }
        }

        private void CreateServiceTypeSection(StackPanel parent, int typeCode, string typeName)
        {
            Button toggleButton = new Button()
            {
                Content = $"▶ {typeName}",
                Background = Brushes.LightBlue,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 5, 0, 0),
                FontWeight = FontWeights.SemiBold
            };

            StackPanel servicesPanel = new StackPanel()
            {
                Margin = new Thickness(10, 0, 0, 10),
                Visibility = Visibility.Collapsed
            };

            LoadServicesForType(typeCode, servicesPanel);

            toggleButton.Click += (s, e) =>
            {
                if (servicesPanel.Visibility == Visibility.Visible)
                {
                    servicesPanel.Visibility = Visibility.Collapsed;
                    toggleButton.Content = $"▶ {typeName}";
                }
                else
                {
                    servicesPanel.Visibility = Visibility.Visible;
                    toggleButton.Content = $"▼ {typeName}";
                }
            };

            parent.Children.Add(toggleButton);
            parent.Children.Add(servicesPanel);
        }

        private void LoadServicesForType(int typeCode, StackPanel panel)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT servName, servPrice, servDuration 
                FROM services 
                WHERE servTypeCode = @TypeCode AND serviceActivity = 'Да' 
                ORDER BY servName";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@TypeCode", typeCode);
                    var reader = comm.ExecuteReader();

                    while (reader.Read())
                    {
                        string name = reader.GetString("servName");
                        int price = reader.GetInt32("servPrice");
                        int duration = reader.GetInt32("servDuration");

                        Border serviceBorder = new Border()
                        {
                            Background = Brushes.White,
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(3),
                            Margin = new Thickness(0, 2, 0, 2),
                            Padding = new Thickness(8)
                        };

                        StackPanel servicePanel = new StackPanel() { Orientation = Orientation.Horizontal };

                        servicePanel.Children.Add(new TextBlock()
                        {
                            Text = name,
                            FontWeight = FontWeights.SemiBold,
                            VerticalAlignment = VerticalAlignment.Center,
                            Width = 200
                        });

                        servicePanel.Children.Add(new TextBlock()
                        {
                            Text = $"{price}₽",
                            Foreground = Brushes.Blue,
                            Margin = new Thickness(10, 0, 10, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            FontWeight = FontWeights.Medium
                        });

                        servicePanel.Children.Add(new TextBlock()
                        {
                            Text = $"{duration} ч",
                            Foreground = Brushes.Gray,
                            VerticalAlignment = VerticalAlignment.Center
                        });

                        serviceBorder.Child = servicePanel;
                        panel.Children.Add(serviceBorder);
                    }
                }
            }
            catch (Exception ex)
            {
                panel.Children.Add(new TextBlock()
                {
                    Text = $"Ошибка загрузки: {ex.Message}",
                    Foreground = Brushes.Red
                });
            }
        }

        // Остальные методы без изменений
        private List<MasterComboBoxItem> LoadMastersForComboBox()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT masterCode, masterName FROM masters WHERE masterActivity = 'Да' ORDER BY masterName";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var reader = comm.ExecuteReader();

                    var masters = new List<MasterComboBoxItem>();
                    while (reader.Read())
                    {
                        masters.Add(new MasterComboBoxItem
                        {
                            MasterCode = reader.GetInt32("masterCode"),
                            MasterName = reader.GetString("masterName")
                        });
                    }
                    return masters;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}");
                return new List<MasterComboBoxItem>();
            }
        }

        private List<ClientComboBoxItem> LoadClientsForComboBox()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT clientCode, clientName FROM clients ORDER BY clientName";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var reader = comm.ExecuteReader();

                    var clients = new List<ClientComboBoxItem>();
                    while (reader.Read())
                    {
                        clients.Add(new ClientComboBoxItem
                        {
                            ClientCode = reader.GetInt32("clientCode"),
                            ClientName = reader.GetString("clientName")
                        });
                    }
                    return clients;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}");
                return new List<ClientComboBoxItem>();
            }
        }

        private List<ServTypeComboBoxItem> LoadServiceTypesForComboBox()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT servTypeCode, servType FROM servTypes WHERE servTypeActivity = 'Да' ORDER BY servType";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var reader = comm.ExecuteReader();

                    var servTypes = new List<ServTypeComboBoxItem>();
                    while (reader.Read())
                    {
                        servTypes.Add(new ServTypeComboBoxItem
                        {
                            ServTypeCode = reader.GetInt32("servTypeCode"),
                            ServType = reader.GetString("servType")
                        });
                    }
                    return servTypes;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов услуг: {ex.Message}");
                return new List<ServTypeComboBoxItem>();
            }
        }

        private List<ServiceComboBoxItem> LoadServicesForComboBox()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT servCode, servName, servTypeCode FROM services WHERE serviceActivity = 'Да' ORDER BY servName";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var reader = comm.ExecuteReader();

                    var services = new List<ServiceComboBoxItem>();
                    while (reader.Read())
                    {
                        services.Add(new ServiceComboBoxItem
                        {
                            ServCode = reader.GetInt32("servCode"),
                            ServName = reader.GetString("servName"),
                            ServTypeCode = reader.GetInt32("servTypeCode")
                        });
                    }
                    return services;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}");
                return new List<ServiceComboBoxItem>();
            }
        }

        private int GetServiceTypeCode(int serviceCode)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT servTypeCode FROM services WHERE servCode = @ServCode";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@ServCode", serviceCode);
                    var result = comm.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении типа услуги: {ex.Message}");
                return 1;
            }
        }

        private int GetNextAppointmentCode()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT MAX(appCode) FROM appointments";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var result = comm.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        return 1;
                    else
                        return Convert.ToInt32(result) + 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                return 1;
            }
        }

        private void SaveAppointment(int appCode, int masterCode, int clientCode, int servTypeCode, int servCode, int queueFrom, int queueTo, DateTime appDate, string appointmentActivity, Window window)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string queryInsert = @"INSERT INTO appointments (appCode, masterCode, clientCode, servTypeCode, servCode, queueFrom, queueTo, appDate, AppointmentActivity) 
                                  VALUES (@AppCode, @MasterCode, @ClientCode, @ServTypeCode, @ServCode, @QueueFrom, @QueueTo, @AppDate, @AppointmentActivity)";

                    MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                    comm.Parameters.AddWithValue("@AppCode", appCode);
                    comm.Parameters.AddWithValue("@MasterCode", masterCode);
                    comm.Parameters.AddWithValue("@ClientCode", clientCode);
                    comm.Parameters.AddWithValue("@ServTypeCode", servTypeCode);
                    comm.Parameters.AddWithValue("@ServCode", servCode);
                    comm.Parameters.AddWithValue("@QueueFrom", queueFrom);
                    comm.Parameters.AddWithValue("@QueueTo", queueTo);
                    comm.Parameters.AddWithValue("@AppDate", appDate);
                    comm.Parameters.AddWithValue("@AppointmentActivity", appointmentActivity);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Запись успешно добавлена с кодом {appCode}!");
                        window.Close();
                        LoadMastersData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        // Добавление новой услуги
        private void b_addService_Click(object sender, RoutedEventArgs e)
        {
            ShowAddServiceWindow();
        }

        private void ShowAddServiceWindow()
        {
            try
            {
                int nextCode = GetNextServiceCode();

                Window serviceWindow = new Window()
                {
                    Title = "Добавление услуги",
                    Width = 450,
                    Height = 520,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = Brushes.White
                };

                StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

                TextBlock header = new TextBlock()
                {
                    Text = "Добавление новой услуги",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20),
                    Foreground = Brushes.DarkBlue
                };
                mainPanel.Children.Add(header);

                TextBox codeTextBox = new TextBox()
                {
                    Text = nextCode.ToString(),
                    IsReadOnly = true,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue
                };
                mainPanel.Children.Add(new Label() { Content = "Код услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(codeTextBox);

                TextBox nameTextBox = new TextBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue
                };
                mainPanel.Children.Add(new Label() { Content = "Название услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(nameTextBox);

                TextBox priceTextBox = new TextBox()
                {
                    Text = "0",
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue
                };
                mainPanel.Children.Add(new Label() { Content = "Цена (руб.):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(priceTextBox);

                TextBox durationTextBox = new TextBox()
                {
                    Text = "1",
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue
                };
                mainPanel.Children.Add(new Label() { Content = "Длительность (часы):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(durationTextBox);

                ComboBox typeComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 20),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    DisplayMemberPath = "ServType"
                };
                var servTypes = LoadServiceTypesForComboBox();
                typeComboBox.ItemsSource = servTypes;
                if (servTypes.Count > 0)
                    typeComboBox.SelectedIndex = 0;

                mainPanel.Children.Add(new Label() { Content = "Тип услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(typeComboBox);

                StackPanel buttonPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                Button saveButton = new Button()
                {
                    Content = "💾 Сохранить",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.DodgerBlue,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                saveButton.Click += (s, args) =>
                {
                    if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                    {
                        MessageBox.Show("Введите название услуги!");
                        return;
                    }

                    if (!int.TryParse(priceTextBox.Text, out int price) || price < 0)
                    {
                        MessageBox.Show("Введите корректную цену!");
                        return;
                    }

                    if (!int.TryParse(durationTextBox.Text, out int duration) || duration <= 0)
                    {
                        MessageBox.Show("Введите корректную длительность!");
                        return;
                    }

                    var selectedType = typeComboBox.SelectedItem as ServTypeComboBoxItem;
                    if (selectedType == null)
                    {
                        MessageBox.Show("Выберите тип услуги!");
                        return;
                    }

                    SaveNewService(nextCode, nameTextBox.Text, price, duration, selectedType.ServTypeCode, serviceWindow);
                };

                Button cancelButton = new Button()
                {
                    Content = "❌ Отмена",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.Gray,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                cancelButton.Click += (s, args) => serviceWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);
                mainPanel.Children.Add(buttonPanel);

                serviceWindow.Content = mainPanel;
                serviceWindow.Loaded += (s, e) => nameTextBox.Focus();
                serviceWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна: {ex.Message}");
            }
        }

        private int GetNextServiceCode()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT MAX(servCode) FROM services";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var result = comm.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        return 1;
                    else
                        return Convert.ToInt32(result) + 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                return 1;
            }
        }

        private void SaveNewService(int servCode, string name, int price, int duration, int servTypeCode, Window window)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string query = @"INSERT INTO services (servCode, servName, servPrice, servDuration, servTypeCode, serviceActivity) 
                                  VALUES (@ServCode, @ServName, @ServPrice, @ServDuration, @ServTypeCode, 'Да')";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@ServCode", servCode);
                    comm.Parameters.AddWithValue("@ServName", name);
                    comm.Parameters.AddWithValue("@ServPrice", price);
                    comm.Parameters.AddWithValue("@ServDuration", duration);
                    comm.Parameters.AddWithValue("@ServTypeCode", servTypeCode);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Услуга '{name}' успешно добавлена с кодом {servCode}!");
                        window.Close();
                        LoadServicesData();
                        LoadQuickServices();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        // Добавление нового мастера
        private void b_addMaster_Click(object sender, RoutedEventArgs e)
        {
            ShowAddMasterWindow();
        }

        private void ShowAddMasterWindow()
        {
            try
            {
                int nextCode = GetNextMasterCode();

                Window masterWindow = new Window()
                {
                    Title = "Добавление мастера",
                    Width = 450,
                    Height = 450,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = Brushes.White
                };

                StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

                TextBlock header = new TextBlock()
                {
                    Text = "Добавление нового мастера",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20),
                    Foreground = Brushes.DarkBlue
                };
                mainPanel.Children.Add(header);

                TextBox codeTextBox = new TextBox()
                {
                    Text = nextCode.ToString(),
                    IsReadOnly = true,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue
                };
                mainPanel.Children.Add(new Label() { Content = "Код мастера:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(codeTextBox);

                TextBox nameTextBox = new TextBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue
                };
                mainPanel.Children.Add(new Label() { Content = "Имя мастера:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(nameTextBox);

                TextBox telTextBox = new TextBox()
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue
                };
                mainPanel.Children.Add(new Label() { Content = "Телефон:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(telTextBox);

                ComboBox specializationComboBox = new ComboBox()
                {
                    Margin = new Thickness(0, 0, 0, 20),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    DisplayMemberPath = "ServType"
                };
                var servTypes = LoadServiceTypesForComboBox();
                specializationComboBox.ItemsSource = servTypes;
                if (servTypes.Count > 0)
                    specializationComboBox.SelectedIndex = 0;

                mainPanel.Children.Add(new Label() { Content = "Специализация:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(specializationComboBox);

                StackPanel buttonPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                Button saveButton = new Button()
                {
                    Content = "💾 Сохранить",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.DodgerBlue,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                saveButton.Click += (s, args) =>
                {
                    if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                    {
                        MessageBox.Show("Введите имя мастера!");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(telTextBox.Text))
                    {
                        MessageBox.Show("Введите телефон мастера!");
                        return;
                    }

                    var selectedSpecialization = specializationComboBox.SelectedItem as ServTypeComboBoxItem;
                    if (selectedSpecialization == null)
                    {
                        MessageBox.Show("Выберите специализацию!");
                        return;
                    }

                    SaveNewMaster(nextCode, nameTextBox.Text, telTextBox.Text, selectedSpecialization.ServTypeCode, masterWindow);
                };

                Button cancelButton = new Button()
                {
                    Content = "❌ Отмена",
                    Margin = new Thickness(10),
                    Padding = new Thickness(15, 8, 15, 8),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.Gray,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                cancelButton.Click += (s, args) => masterWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);
                mainPanel.Children.Add(buttonPanel);

                masterWindow.Content = mainPanel;
                masterWindow.Loaded += (s, e) => nameTextBox.Focus();
                masterWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private int GetNextMasterCode()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT MAX(masterCode) FROM masters";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var result = comm.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        return 1;
                    else
                        return Convert.ToInt32(result) + 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                return 1;
            }
        }

        private void SaveNewMaster(int masterCode, string name, string tel, int servTypeCode, Window window)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string query = @"INSERT INTO masters (masterCode, masterName, masterTel, servTypeCode, masterActivity) 
                                  VALUES (@MasterCode, @MasterName, @MasterTel, @ServTypeCode, 'Да')";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@MasterCode", masterCode);
                    comm.Parameters.AddWithValue("@MasterName", name);
                    comm.Parameters.AddWithValue("@MasterTel", tel);
                    comm.Parameters.AddWithValue("@ServTypeCode", servTypeCode);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Мастер '{name}' успешно добавлен с кодом {masterCode}!");
                        window.Close();
                        LoadMastersData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void CallClient_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is string phoneNumber)
            {
                MessageBox.Show($"Имитация звонка клиенту на номер: {phoneNumber}", "Звонок клиенту");
            }
        }

        private void LoadQuickServices()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT servCode, servName, servPrice
                        FROM services 
                        WHERE serviceActivity = 'Да'
                        ORDER BY servName";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var reader = comm.ExecuteReader();

                    QuickServices.Clear();
                    while (reader.Read())
                    {
                        QuickServices.Add(new EverydayServiceComboBox()
                        {
                            ServCode = reader.GetInt32("servCode"),
                            ServName = reader.GetString("servName"),
                            ServPrice = reader.GetInt32("servPrice")
                        });
                    }

                    cb_quickService.ItemsSource = QuickServices;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке услуг: {ex.Message}");
            }
        }

        private void LoadWorkHours()
        {
            try
            {
                WorkHours.Clear();
                WorkHours.Add("Понедельник: 9:00 - 20:00");
                WorkHours.Add("Вторник: 9:00 - 20:00");
                WorkHours.Add("Среда: 9:00 - 20:00");
                WorkHours.Add("Четверг: 9:00 - 20:00");
                WorkHours.Add("Пятница: 9:00 - 20:00");
                WorkHours.Add("Суббота: 10:00 - 18:00");
                WorkHours.Add("Воскресенье: 10:00 - 18:00");

                lb_workHours.ItemsSource = WorkHours;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке графика работы: {ex.Message}");
            }
        }

        private void LoadServicesData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT s.servName, s.servPrice, s.servDuration, st.servType
                        FROM services s
                        LEFT JOIN servTypes st ON s.servTypeCode = st.servTypeCode
                        WHERE s.serviceActivity = 'Да'
                        ORDER BY st.servType, s.servPrice
                        LIMIT 10;";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var reader = comm.ExecuteReader();

                    Services.Clear();
                    while (reader.Read())
                    {
                        Services.Add(new EverydayServiceItem()
                        {
                            ServName = reader.GetString("servName"),
                            ServPrice = reader.GetInt32("servPrice"),
                            ServDuration = reader.GetInt32("servDuration"),
                            ServTypeName = reader.GetString("servType")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке услуг: {ex.Message}");
            }
        }

        private void LoadMastersData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT DISTINCT m.masterName, st.servType, m.masterTel
                        FROM masters m
                        LEFT JOIN servTypes st ON m.servTypeCode = st.servTypeCode
                        WHERE m.masterActivity = 'Да'
                        ORDER BY st.servType, m.masterName;";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    var reader = comm.ExecuteReader();

                    Masters.Clear();
                    while (reader.Read())
                    {
                        var masterName = reader.GetString("masterName");
                        var masterItem = new ExpandedMasterItem()
                        {
                            MasterName = masterName,
                            ServTypeName = reader.GetString("servType"),
                            MasterTel = reader.GetString("masterTel"),
                            MasterClients = new ObservableCollection<MasterClientItem>()
                        };

                        LoadMasterClients(masterName, masterItem.MasterClients);
                        masterItem.ClientsCount = masterItem.MasterClients.Count;

                        Masters.Add(masterItem);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке мастеров: {ex.Message}");
            }
        }

        private void LoadMasterClients(string masterName, ObservableCollection<MasterClientItem> clientsCollection)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    // ОБНОВЛЕННЫЙ ЗАПРОС С УЧЕТОМ AppointmentActivity
                    string query = @"
                SELECT 
                    c.clientName,
                    c.clientTel,
                    s.servName,
                    a.appDate,
                    c.clientCode
                FROM appointments a
                INNER JOIN masters m ON a.masterCode = m.masterCode
                INNER JOIN clients c ON a.clientCode = c.clientCode
                INNER JOIN services s ON a.servCode = s.servCode
                WHERE m.masterName = @masterName 
                AND a.AppointmentActivity = 'Да'  -- ДОБАВЛЕНА ПРОВЕРКА АКТИВНОСТИ
                ORDER BY a.appDate DESC";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@masterName", masterName);

                    var reader = comm.ExecuteReader();

                    while (reader.Read())
                    {
                        clientsCollection.Add(new MasterClientItem()
                        {
                            ClientName = reader.GetString("clientName"),
                            ClientTel = reader.GetString("clientTel"),
                            ServiceName = reader.GetString("servName"),
                            AppointmentDate = reader.GetDateTime("appDate"),
                            ClientCode = reader.GetInt32("clientCode")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки клиентов для мастера {masterName}: {ex.Message}");
            }
        }

        // Раскрытие/скрытие списка клиентов
        private void ToggleMasterClients(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var stackPanel = FindVisualParent<StackPanel>(button);
            if (stackPanel == null) return;

            var clientsPanel = FindVisualChild<StackPanel>(stackPanel, "pnlClients");
            var arrowText = FindVisualChild<TextBlock>(stackPanel, "txtArrow");

            if (clientsPanel != null && arrowText != null)
            {
                if (clientsPanel.Visibility == Visibility.Collapsed)
                {
                    clientsPanel.Visibility = Visibility.Visible;
                    arrowText.Text = "▲";
                    button.Background = new SolidColorBrush(Color.FromArgb(30, 25, 118, 210));
                }
                else
                {
                    clientsPanel.Visibility = Visibility.Collapsed;
                    arrowText.Text = "▼";
                    button.Background = Brushes.Transparent;
                }
            }
        }

        // Вспомогательные методы для поиска элементов
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null && !(child is T))
                child = VisualTreeHelper.GetParent(child);
            return child as T;
        }

        private T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T && (childName == null || ((FrameworkElement)child).Name == childName))
                    return (T)child;

                var result = FindVisualChild<T>(child, childName);
                if (result != null)
                    return result;
            }
            return null;
        }

        // Остальные методы навигации
        private void b_viewAllServices_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Hide();
        }

        private void b_viewAllMasters_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Hide();
        }

        private void b_call_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Имитация звонка на номер: +7 (999) 123-45-67", "Звонок");
        }

        private void b_admin_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Hide();
        }

        private void b_exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}