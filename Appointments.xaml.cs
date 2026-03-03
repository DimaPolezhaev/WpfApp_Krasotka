using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MySqlConnector;
using System;
using System.Linq;
using System.Windows.Media;
using System.Globalization;
using System.IO;
using System.Text;

namespace WpfApp_Krasotka
{
    public partial class Appointments : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";
        public ObservableCollection<AppointmentsClass> appointmentsCollection { get; set; }
        public ObservableCollection<AppointmentsClass> allAppointmentsCollection { get; set; }
        public ObservableCollection<MasterComboBox> mastersCollection { get; set; }
        public ObservableCollection<ClientComboBox> clientsCollection { get; set; }
        public ObservableCollection<ServTypeComboBox> servTypesCollection { get; set; }
        public ObservableCollection<ServiceComboBox> servicesCollection { get; set; }

        private bool showInactiveAppointments = false;

        public Appointments()
        {
            appointmentsCollection = new ObservableCollection<AppointmentsClass>();
            allAppointmentsCollection = new ObservableCollection<AppointmentsClass>();
            mastersCollection = new ObservableCollection<MasterComboBox>();
            clientsCollection = new ObservableCollection<ClientComboBox>();
            servTypesCollection = new ObservableCollection<ServTypeComboBox>();
            servicesCollection = new ObservableCollection<ServiceComboBox>();

            InitializeComponent();
            appointmentCell.ItemsSource = appointmentsCollection;

            // Автоматическая загрузка данных при открытии окна
            LoadComboBoxData();
            LoadAppointmentsData();
            UpdateStatusBar();
        }

        public class AppointmentsClass
        {
            public int AppCode { get; set; }
            public int MasterCode { get; set; }
            public string MasterName { get; set; }
            public int ClientCode { get; set; }
            public string ClientName { get; set; }
            public int ServTypeCode { get; set; }
            public string ServTypeName { get; set; }
            public int ServCode { get; set; }
            public string ServName { get; set; }
            public int QueueFrom { get; set; }
            public int QueueTo { get; set; }
            public DateTime AppDate { get; set; }
            public int Duration { get; set; }
            public string AppointmentActivity { get; set; }
        }

        public class MasterComboBox
        {
            public int MasterCode { get; set; }
            public string MasterName { get; set; }
        }

        public class ClientComboBox
        {
            public int ClientCode { get; set; }
            public string ClientName { get; set; }
        }

        public class ServTypeComboBox
        {
            public int ServTypeCode { get; set; }
            public string ServType { get; set; }
        }

        public class ServiceComboBox
        {
            public int ServCode { get; set; }
            public string ServName { get; set; }
            public int ServTypeCode { get; set; }
        }

        // Метод для загрузки данных для ComboBox
        private void LoadComboBoxData()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(ConnectionString);
                conn.Open();

                // Загрузка мастеров
                string queryMasters = "SELECT masterCode, masterName FROM masters WHERE masterActivity = 'Да';";
                MySqlCommand commMasters = new MySqlCommand(queryMasters, conn);
                MySqlDataReader readerMasters = commMasters.ExecuteReader();

                mastersCollection.Clear();
                while (readerMasters.Read())
                {
                    mastersCollection.Add(new MasterComboBox()
                    {
                        MasterCode = readerMasters.GetInt32(0),
                        MasterName = readerMasters.GetString(1)
                    });
                }
                readerMasters.Close();

                // Загрузка клиентов
                string queryClients = "SELECT clientCode, clientName FROM clients WHERE clientsActivity = 'Да';";
                MySqlCommand commClients = new MySqlCommand(queryClients, conn);
                MySqlDataReader readerClients = commClients.ExecuteReader();

                clientsCollection.Clear();
                while (readerClients.Read())
                {
                    clientsCollection.Add(new ClientComboBox()
                    {
                        ClientCode = readerClients.GetInt32(0),
                        ClientName = readerClients.GetString(1)
                    });
                }
                readerClients.Close();

                // Загрузка типов услуг
                string queryServTypes = "SELECT servTypeCode, servType FROM servTypes WHERE servTypeActivity = 'Да';";
                MySqlCommand commServTypes = new MySqlCommand(queryServTypes, conn);
                MySqlDataReader readerServTypes = commServTypes.ExecuteReader();

                servTypesCollection.Clear();
                while (readerServTypes.Read())
                {
                    servTypesCollection.Add(new ServTypeComboBox()
                    {
                        ServTypeCode = readerServTypes.GetInt32(0),
                        ServType = readerServTypes.GetString(1)
                    });
                }
                readerServTypes.Close();

                // Загрузка услуг
                string queryServices = "SELECT servCode, servName, servTypeCode FROM services WHERE serviceActivity = 'Да';";
                MySqlCommand commServices = new MySqlCommand(queryServices, conn);
                MySqlDataReader readerServices = commServices.ExecuteReader();

                servicesCollection.Clear();
                while (readerServices.Read())
                {
                    servicesCollection.Add(new ServiceComboBox()
                    {
                        ServCode = readerServices.GetInt32(0),
                        ServName = readerServices.GetString(1),
                        ServTypeCode = readerServices.GetInt32(2)
                    });
                }
                readerServices.Close();

                conn.Close();

                // Устанавливаем источники данных для ComboBox
                cb_master.ItemsSource = mastersCollection;
                cb_client.ItemsSource = clientsCollection;
                cb_servType.ItemsSource = servTypesCollection;
                cb_service.ItemsSource = servicesCollection;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных для выпадающих списков: {ex.Message}");
            }
        }

        // Метод для загрузки данных записей
        private void LoadAppointmentsData()
        {
            try
            {
                statusText.Text = "Загрузка данных...";

                MySqlConnection conn = new MySqlConnection(ConnectionString);
                conn.Open();

                // ИЗМЕНЕННЫЙ ЗАПРОС - добавил appointmentActivity
                string queryShowAll = @"
            SELECT a.appCode, a.masterCode, m.masterName, 
                   a.clientCode, c.clientName, 
                   a.servTypeCode, st.servType, 
                   a.servCode, s.servName, s.servDuration,
                   a.queueFrom, a.queueTo, a.appDate, a.appointmentActivity
            FROM appointments a 
            LEFT JOIN masters m ON a.masterCode = m.masterCode
            LEFT JOIN clients c ON a.clientCode = c.clientCode
            LEFT JOIN servTypes st ON a.servTypeCode = st.servTypeCode
            LEFT JOIN services s ON a.servCode = s.servCode
            ORDER BY a.appCode ASC, a.appDate ASC, a.queueFrom ASC;";

                MySqlCommand comm = new MySqlCommand(queryShowAll, conn);
                MySqlDataReader reader = comm.ExecuteReader();

                appointmentsCollection.Clear();
                allAppointmentsCollection.Clear();

                while (reader.Read())
                {
                    var appointment = new AppointmentsClass()
                    {
                        AppCode = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        MasterCode = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        MasterName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        ClientCode = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        ClientName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        ServTypeCode = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                        ServTypeName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        ServCode = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                        ServName = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        QueueFrom = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                        QueueTo = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                        AppDate = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12),
                        Duration = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                        AppointmentActivity = reader.IsDBNull(13) ? "Да" : reader.GetString(13)
                    };

                    allAppointmentsCollection.Add(appointment);

                    // В ОСНОВНУЮ КОЛЛЕКЦИЮ ДОБАВЛЯЕМ В ЗАВИСИМОСТИ ОТ РЕЖИМА ОТОБРАЖЕНИЯ
                    if (!showInactiveAppointments && appointment.AppointmentActivity == "Да")
                    {
                        appointmentsCollection.Add(appointment);
                    }
                    else if (showInactiveAppointments && appointment.AppointmentActivity == "Нет")
                    {
                        appointmentsCollection.Add(appointment);
                    }
                }

                reader.Close();
                conn.Close();

                statusText.Text = "Данные загружены успешно.";
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
                statusText.Text = "Ошибка загрузки данных.";
            }
        }

        private void UpdateStatusBar()
        {
            appointmentCountText.Text = $" Записей: {appointmentsCollection.Count}";
            statusText.Text = "Готово.";
        }

        // Поиск записей
        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                appointmentsCollection.Clear();
                foreach (var appointment in allAppointmentsCollection)
                {
                    appointmentsCollection.Add(appointment);
                }
            }
            else
            {
                appointmentsCollection.Clear();
                var filteredAppointments = allAppointmentsCollection.Where(a =>
                    (a.MasterName != null && a.MasterName.ToLower().Contains(searchText)) ||
                    (a.ClientName != null && a.ClientName.ToLower().Contains(searchText)) ||
                    (a.ServTypeName != null && a.ServTypeName.ToLower().Contains(searchText)) ||
                    (a.ServName != null && a.ServName.ToLower().Contains(searchText)) ||
                    (a.AppDate.ToString("dd.MM.yyyy").Contains(searchText))
                );

                foreach (var appointment in filteredAppointments)
                {
                    appointmentsCollection.Add(appointment);
                }
            }

            UpdateStatusBar();
        }

        private void b_appointmentShowAll_Click(object sender, RoutedEventArgs e)
        {
            searchTextBox.Text = "";
            LoadAppointmentsData();
            statusText.Text = "Отображены все записи";
        }

        private void b_appointmentAdd_Click(object sender, RoutedEventArgs e)
        {
            int nextCode = GetNextAppointmentCode();
            ShowAppointmentCard(new AppointmentsClass()
            {
                AppCode = nextCode,
                MasterCode = 0,
                ClientCode = 0,
                ServTypeCode = 0,
                ServCode = 0,
                QueueFrom = 9,
                QueueTo = 10,
                AppDate = DateTime.Today,
                Duration = 1
            }, true, true);
        }

        private void b_appointmentChange_Click(object sender, RoutedEventArgs e)
        {
            if (appointmentCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для изменения!");
                return;
            }

            var selectedAppointment = appointmentCell.SelectedItem as AppointmentsClass;
            if (selectedAppointment != null)
            {
                ShowAppointmentCard(selectedAppointment, true, false);
            }
        }

        private void b_appointmentDelete_Click(object sender, RoutedEventArgs e)
        {
            if (appointmentCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для деактивации!");
                return;
            }

            var selectedAppointment = appointmentCell.SelectedItem as AppointmentsClass;
            if (selectedAppointment == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите деактивировать запись от {selectedAppointment.AppDate:dd.MM.yyyy}?\nКлиент: {selectedAppointment.ClientName}, Услуга: {selectedAppointment.ServName}",
                "Подтверждение деактивации", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    statusText.Text = "Деактивация записи...";

                    using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                    {
                        conn.Open();

                        string queryUpdate = "UPDATE appointments SET appointmentActivity = 'Нет' WHERE appCode = @AppCode;";
                        using (MySqlCommand comm = new MySqlCommand(queryUpdate, conn))
                        {
                            comm.Parameters.AddWithValue("@AppCode", selectedAppointment.AppCode);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Запись успешно деактивирована!");

                                // Обновляем статус в коллекциях
                                selectedAppointment.AppointmentActivity = "Нет";

                                // Удаляем из основной коллекции (только активные записи)
                                appointmentsCollection.Remove(selectedAppointment);

                                // Обновляем отображение
                                appointmentCell.Items.Refresh();

                                statusText.Text = "Запись деактивирована успешно.";
                                UpdateStatusBar();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при деактивации записи: {ex.Message}");
                    statusText.Text = "Ошибка деактивации записи.";
                }
            }
        }

        private void b_appointmentFilter_Click(object sender, RoutedEventArgs e)
        {
            // Окно фильтрации по дате
            Window filterWindow = new Window()
            {
                Title = "Фильтр по дате",
                Width = 350,
                Height = 210,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            StackPanel panel = new StackPanel() { Margin = new Thickness(20) };

            TextBlock header = new TextBlock()
            {
                Text = "Выберите дату для фильтрации",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(header);

            DatePicker datePicker = new DatePicker()
            {
                SelectedDate = DateTime.Today,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(datePicker);

            Button applyButton = new Button()
            {
                Content = "Применить фильтр",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Background = Brushes.DodgerBlue,
                Foreground = Brushes.White,
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };

            applyButton.Click += (s, args) =>
            {
                if (datePicker.SelectedDate.HasValue)
                {
                    FilterByDate(datePicker.SelectedDate.Value);
                    filterWindow.Close();
                }
            };

            panel.Children.Add(applyButton);

            Button showAllButton = new Button()
            {
                Content = "Показать все",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Background = Brushes.Gray,
                Foreground = Brushes.White,
                Padding = new Thickness(15, 8, 15, 8)
            };

            showAllButton.Click += (s, args) =>
            {
                LoadAppointmentsData();
                filterWindow.Close();
            };

            panel.Children.Add(showAllButton);

            filterWindow.Content = panel;
            filterWindow.ShowDialog();
        }

        private void FilterByDate(DateTime date)
        {
            appointmentsCollection.Clear();
            var filteredAppointments = allAppointmentsCollection.Where(a => a.AppDate.Date == date.Date);

            foreach (var appointment in filteredAppointments)
            {
                appointmentsCollection.Add(appointment);
            }

            statusText.Text = $"Отображены записи на {date:dd.MM.yyyy}";
            UpdateStatusBar();
        }

        private void appointmentCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (appointmentCell.SelectedItem != null)
            {
                var selectedAppointment = appointmentCell.SelectedItem as AppointmentsClass;
                if (selectedAppointment != null)
                {
                    UpdateAppointmentCard(selectedAppointment);
                }
            }
            else
            {
                // Очищаем поля, если ничего не выбрано
                l_ac.Text = "";
                cb_master.SelectedIndex = -1;
                cb_client.SelectedIndex = -1;
                cb_servType.SelectedIndex = -1;
                cb_service.SelectedIndex = -1;
                dp_appDate.SelectedDate = null;
                l_qf.Text = "";
                l_qt.Text = "";
            }
        }

        private void UpdateAppointmentCard(AppointmentsClass appointment)
        {
            try
            {
                l_ac.Text = appointment.AppCode.ToString();

                // Установка выбранного мастера
                foreach (MasterComboBox item in cb_master.Items)
                {
                    if (item.MasterCode == appointment.MasterCode)
                    {
                        cb_master.SelectedItem = item;
                        break;
                    }
                }

                // Установка выбранного клиента
                foreach (ClientComboBox item in cb_client.Items)
                {
                    if (item.ClientCode == appointment.ClientCode)
                    {
                        cb_client.SelectedItem = item;
                        break;
                    }
                }

                // Установка выбранного типа услуги
                foreach (ServTypeComboBox item in cb_servType.Items)
                {
                    if (item.ServTypeCode == appointment.ServTypeCode)
                    {
                        cb_servType.SelectedItem = item;
                        break;
                    }
                }

                // Установка выбранной услуги
                foreach (ServiceComboBox item in cb_service.Items)
                {
                    if (item.ServCode == appointment.ServCode)
                    {
                        cb_service.SelectedItem = item;
                        break;
                    }
                }

                dp_appDate.SelectedDate = appointment.AppDate;
                l_qf.Text = appointment.QueueFrom.ToString();
                l_qt.Text = appointment.QueueTo.ToString();

                // Установка активности
                if (appointment.AppointmentActivity != null)
                {
                    foreach (ComboBoxItem item in cb_activity.Items)
                    {
                        if (item.Content?.ToString() == appointment.AppointmentActivity)
                        {
                            cb_activity.SelectedItem = item;
                            break;
                        }
                    }
                }
                else
                {
                    cb_activity.SelectedIndex = 0; // Значение по умолчанию
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении карточки записи: {ex.Message}");
            }
        }

        // НОВЫЙ МЕТОД: Автоматический выбор мастера по типу услуги
        private MasterComboBox GetAutoSelectedMaster(int servTypeCode)
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
                        return new MasterComboBox
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
                        return new MasterComboBox
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

        // НОВЫЙ МЕТОД: Получение кода типа услуги по коду услуги
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

        // Метод для отображения карточки записи в отдельном окне
        private void ShowAppointmentCard(AppointmentsClass appointment, bool isEditable, bool isNewAppointment)
        {
            try
            {
                string windowTitle = isNewAppointment ? "Добавление записи" : "Редактирование записи";
                string headerText = isNewAppointment ? "Добавление записи" : "Редактирование записи";

                Window appointmentCardWindow = new Window()
                {
                    Title = windowTitle,
                    Width = 500,
                    Height = 790, // Увеличил высоту для нового поля
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = Brushes.White
                };

                StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

                // Заголовок
                TextBlock header = new TextBlock()
                {
                    Text = headerText,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20),
                    Foreground = Brushes.Navy
                };
                mainPanel.Children.Add(header);

                // Поле кода записи - всегда только для чтения
                TextBox codeTextBox = new TextBox()
                {
                    Text = appointment.AppCode.ToString(),
                    IsReadOnly = true,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Код записи генерируется автоматически"
                };
                mainPanel.Children.Add(new Label() { Content = "Код записи:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(codeTextBox);

                // Поле выбора мастера
                ComboBox masterComboBox = new ComboBox()
                {
                    IsEnabled = isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "MasterName"
                };
                masterComboBox.ItemsSource = mastersCollection;

                foreach (MasterComboBox item in masterComboBox.Items)
                {
                    if (item.MasterCode == appointment.MasterCode)
                    {
                        masterComboBox.SelectedItem = item;
                        break;
                    }
                }

                mainPanel.Children.Add(new Label() { Content = "Мастер:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(masterComboBox);

                // Поле выбора клиента
                ComboBox clientComboBox = new ComboBox()
                {
                    IsEnabled = isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "ClientName"
                };
                clientComboBox.ItemsSource = clientsCollection;

                foreach (ClientComboBox item in clientComboBox.Items)
                {
                    if (item.ClientCode == appointment.ClientCode)
                    {
                        clientComboBox.SelectedItem = item;
                        break;
                    }
                }

                mainPanel.Children.Add(new Label() { Content = "Клиент:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(clientComboBox);

                // Поле выбора типа услуги
                ComboBox servTypeComboBox = new ComboBox()
                {
                    IsEnabled = isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "ServType"
                };
                servTypeComboBox.ItemsSource = servTypesCollection;

                // УСТАНОВКА ВЫБРАННОГО ТИПА УСЛУГИ - ИСПРАВЛЕННАЯ ВЕРСИЯ
                bool servTypeSet = false;
                foreach (ServTypeComboBox item in servTypeComboBox.Items)
                {
                    if (item.ServTypeCode == appointment.ServTypeCode)
                    {
                        servTypeComboBox.SelectedItem = item;
                        servTypeSet = true;
                        break;
                    }
                }

                // Если не удалось установить по коду, выбираем первый элемент
                if (!servTypeSet && servTypesCollection.Count > 0)
                {
                    servTypeComboBox.SelectedIndex = 0;
                }

                mainPanel.Children.Add(new Label() { Content = "Тип услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(servTypeComboBox);

                // Поле выбора услуги
                ComboBox serviceComboBox = new ComboBox()
                {
                    IsEnabled = isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    DisplayMemberPath = "ServName"
                };

                // ПЕРВОНАЧАЛЬНАЯ ЗАГРУЗКА УСЛУГ И АВТОМАТИЧЕСКИЙ ВЫБОР МАСТЕРА
                var currentServType = servTypeComboBox.SelectedItem as ServTypeComboBox;
                if (currentServType != null)
                {
                    // Загрузка услуг для выбранного типа
                    serviceComboBox.ItemsSource = servicesCollection.Where(serv => serv.ServTypeCode == currentServType.ServTypeCode).ToList();

                    // Установка выбранной услуги
                    foreach (ServiceComboBox item in serviceComboBox.Items)
                    {
                        if (item.ServCode == appointment.ServCode)
                        {
                            serviceComboBox.SelectedItem = item;
                            break;
                        }
                    }

                    // Автоматический выбор мастера для новых записей
                    if (isNewAppointment && isEditable)
                    {
                        var autoSelectedMaster = GetAutoSelectedMaster(currentServType.ServTypeCode);
                        if (autoSelectedMaster != null)
                        {
                            foreach (MasterComboBox master in masterComboBox.Items)
                            {
                                if (master.MasterCode == autoSelectedMaster.MasterCode)
                                {
                                    masterComboBox.SelectedItem = master;
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (servTypesCollection.Count > 0)
                {
                    // Если тип услуги не выбран, выбираем первый доступный
                    servTypeComboBox.SelectedIndex = 0;
                    var firstServType = servTypeComboBox.SelectedItem as ServTypeComboBox;
                    serviceComboBox.ItemsSource = servicesCollection.Where(serv => serv.ServTypeCode == firstServType.ServTypeCode).ToList();

                    // Автоматический выбор мастера для первого типа
                    if (isNewAppointment && isEditable)
                    {
                        var autoSelectedMaster = GetAutoSelectedMaster(firstServType.ServTypeCode);
                        if (autoSelectedMaster != null)
                        {
                            foreach (MasterComboBox master in masterComboBox.Items)
                            {
                                if (master.MasterCode == autoSelectedMaster.MasterCode)
                                {
                                    masterComboBox.SelectedItem = master;
                                    break;
                                }
                            }
                        }
                    }
                }

                // ОБРАБОТЧИК ИЗМЕНЕНИЯ ТИПА УСЛУГИ
                servTypeComboBox.SelectionChanged += (s, args) =>
                {
                    var selectedServType = servTypeComboBox.SelectedItem as ServTypeComboBox;
                    if (selectedServType != null)
                    {
                        // Обновление списка услуг
                        serviceComboBox.ItemsSource = servicesCollection.Where(serv => serv.ServTypeCode == selectedServType.ServTypeCode).ToList();

                        // Автоматический выбор мастера при изменении типа услуги (только для новых записей)
                        if (isNewAppointment && isEditable)
                        {
                            var newAutoMaster = GetAutoSelectedMaster(selectedServType.ServTypeCode);
                            if (newAutoMaster != null)
                            {
                                foreach (MasterComboBox master in masterComboBox.Items)
                                {
                                    if (master.MasterCode == newAutoMaster.MasterCode)
                                    {
                                        masterComboBox.SelectedItem = master;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Если тип услуги не выбран, очищаем список услуг
                        serviceComboBox.ItemsSource = null;
                    }
                };

                foreach (ServiceComboBox item in serviceComboBox.Items)
                {
                    if (item.ServCode == appointment.ServCode)
                    {
                        serviceComboBox.SelectedItem = item;
                        break;
                    }
                }

                mainPanel.Children.Add(new Label() { Content = "Услуга:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(serviceComboBox);

                // Поле даты записи
                DatePicker datePicker = new DatePicker()
                {
                    SelectedDate = appointment.AppDate,
                    IsEnabled = isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Дата записи:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(datePicker);

                // Поле времени начала
                TextBox queueFromTextBox = new TextBox()
                {
                    Text = appointment.QueueFrom.ToString(),
                    IsReadOnly = !isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Время начала в формате часа (9-18)"
                };
                mainPanel.Children.Add(new Label() { Content = "Время начала (час):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(queueFromTextBox);

                // Поле времени окончания
                TextBox queueToTextBox = new TextBox()
                {
                    Text = appointment.QueueTo.ToString(),
                    IsReadOnly = !isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Время окончания в формате часа (10-19)"
                };
                mainPanel.Children.Add(new Label() { Content = "Время окончания (час):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(queueToTextBox);

                // ПОЛЕ ВЫБОРА АКТИВНОСТИ ЗАПИСИ - ДОБАВЛЕНО ДЛЯ ВСЕХ СЛУЧАЕВ
                ComboBox activityComboBox = new ComboBox()
                {
                    IsEnabled = isEditable,
                    Margin = new Thickness(0, 0, 0, 20),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };

                // Добавляем варианты активности
                activityComboBox.Items.Add("Да");
                activityComboBox.Items.Add("Нет");

                // Установка текущего значения активности
                string currentActivity = appointment.AppointmentActivity ?? "Да";
                activityComboBox.SelectedItem = currentActivity;

                mainPanel.Children.Add(new Label() { Content = "Активный:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(activityComboBox);

                // Кнопки
                StackPanel buttonPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                if (isEditable)
                {
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
                        var selectedMaster = masterComboBox.SelectedItem as MasterComboBox;
                        var selectedClient = clientComboBox.SelectedItem as ClientComboBox;
                        var selectedServType = servTypeComboBox.SelectedItem as ServTypeComboBox;
                        var selectedService = serviceComboBox.SelectedItem as ServiceComboBox;

                        // Получаем выбранную активность
                        string activity = activityComboBox.SelectedItem?.ToString() ?? "Да";

                        if (selectedMaster == null)
                        {
                            MessageBox.Show("Выберите мастера!");
                            return;
                        }

                        if (selectedClient == null)
                        {
                            MessageBox.Show("Выберите клиента!");
                            return;
                        }

                        if (selectedServType == null)
                        {
                            MessageBox.Show("Выберите тип услуги!");
                            return;
                        }

                        if (selectedService == null)
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

                        SaveAppointmentWithCustomCode(appointment, selectedMaster.MasterCode, selectedClient.ClientCode,
                            selectedServType.ServTypeCode, selectedService.ServCode, queueFrom, queueTo,
                            datePicker.SelectedDate.Value, activity, appointmentCardWindow, isNewAppointment);
                    };

                    buttonPanel.Children.Add(saveButton);
                }

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

                closeButton.Click += (s, args) =>
                {
                    appointmentCardWindow.Close();
                    statusText.Text = "Готово.";
                };

                buttonPanel.Children.Add(closeButton);
                mainPanel.Children.Add(buttonPanel);

                appointmentCardWindow.Content = mainPanel;

                // Устанавливаем фокус ДО показа окна
                if (isEditable && isNewAppointment)
                {
                    appointmentCardWindow.Loaded += (s, e) =>
                    {
                        masterComboBox.Focus();
                    };
                }

                appointmentCardWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна редактирования: {ex.Message}");
            }
        }

        // Автоматический ключ
        private int GetNextAppointmentCode()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(ConnectionString);
                string query = "SELECT MAX(appCode) FROM appointments;";

                conn.Open();
                MySqlCommand comm = new MySqlCommand(query, conn);
                var result = comm.ExecuteScalar();

                conn.Close();

                if (result == null || result == DBNull.Value)
                    return 1;
                else
                    return Convert.ToInt32(result) + 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении следующего кода: {ex.Message}");
                return 1;
            }
        }

        // Метод для сохранения записи
        private void SaveAppointmentWithCustomCode(AppointmentsClass appointment, int masterCode, int clientCode, int servTypeCode, int servCode, int queueFrom, int queueTo, DateTime appDate, string appointmentActivity, Window window, bool isNewAppointment)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string query;
                    if (isNewAppointment)
                    {
                        // ВСТАВКА НОВОЙ ЗАПИСИ С AppointmentActivity
                        query = @"INSERT INTO appointments (appCode, masterCode, clientCode, servTypeCode, servCode, queueFrom, queueTo, appDate, AppointmentActivity) 
                          VALUES (@AppCode, @MasterCode, @ClientCode, @ServTypeCode, @ServCode, @QueueFrom, @QueueTo, @AppDate, @AppointmentActivity)";
                    }
                    else
                    {
                        // ОБНОВЛЕНИЕ СУЩЕСТВУЮЩЕЙ ЗАПИСИ С AppointmentActivity
                        query = @"UPDATE appointments 
                          SET masterCode = @MasterCode, clientCode = @ClientCode, servTypeCode = @ServTypeCode, 
                              servCode = @ServCode, queueFrom = @QueueFrom, queueTo = @QueueTo, 
                              appDate = @AppDate, AppointmentActivity = @AppointmentActivity
                          WHERE appCode = @AppCode";
                    }

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@AppCode", appointment.AppCode);
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
                        string action = isNewAppointment ? "добавлена" : "обновлена";
                        MessageBox.Show($"Запись успешно {action} с кодом {appointment.AppCode}!");
                        window.Close();

                        // Обновляем данные
                        if (isNewAppointment)
                        {
                            LoadAppointmentsData();
                        }
                        else
                        {
                            // Для обновления существующей записи
                            LoadAppointmentsData(); // или другой метод обновления данных
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void b_appointmentExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.Filter = "Excel файлы (*.xlsx)|*.xlsx|CSV файлы (*.csv)|*.csv";
                saveFileDialog.FileName = $"Записи_салона_{DateTime.Now:ddMMyyyy}";

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    if (Path.GetExtension(filePath).ToLower() == ".csv")
                    {
                        ExportToCsv(filePath);
                    }
                    else
                    {
                        ExportToExcel(filePath);
                    }

                    MessageBox.Show($"Данные успешно экспортированы в файл: {filePath}", "Экспорт завершен");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка");
            }
        }

        private void ExportToCsv(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Заголовки
                writer.WriteLine("Код записи;Мастер;Клиент;Тип услуги;Услуга;Дата;Время начала;Время окончания;Длительность");

                // Данные
                foreach (var appointment in allAppointmentsCollection)
                {
                    writer.WriteLine($"{appointment.AppCode};{appointment.MasterName};{appointment.ClientName};" +
                                   $"{appointment.ServTypeName};{appointment.ServName};{appointment.AppDate:dd.MM.yyyy};" +
                                   $"{appointment.QueueFrom}:00;{appointment.QueueTo}:00;{appointment.Duration} ч");
                }
            }
        }

        private void ExportToExcel(string filePath)
        {
            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Записи");

                // Заголовки
                worksheet.Cells[1, 1].Value = "Код записи";
                worksheet.Cells[1, 2].Value = "Мастер";
                worksheet.Cells[1, 3].Value = "Клиент";
                worksheet.Cells[1, 4].Value = "Тип услуги";
                worksheet.Cells[1, 5].Value = "Услуга";
                worksheet.Cells[1, 6].Value = "Дата";
                worksheet.Cells[1, 7].Value = "Время начала";
                worksheet.Cells[1, 8].Value = "Время окончания";
                worksheet.Cells[1, 9].Value = "Длительность";

                // Данные
                int row = 2;
                foreach (var appointment in allAppointmentsCollection)
                {
                    worksheet.Cells[row, 1].Value = appointment.AppCode;
                    worksheet.Cells[row, 2].Value = appointment.MasterName;
                    worksheet.Cells[row, 3].Value = appointment.ClientName;
                    worksheet.Cells[row, 4].Value = appointment.ServTypeName;
                    worksheet.Cells[row, 5].Value = appointment.ServName;
                    worksheet.Cells[row, 6].Value = appointment.AppDate.ToString("dd.MM.yyyy");
                    worksheet.Cells[row, 7].Value = $"{appointment.QueueFrom}:00";
                    worksheet.Cells[row, 8].Value = $"{appointment.QueueTo}:00";
                    worksheet.Cells[row, 9].Value = $"{appointment.Duration} ч";
                    row++;
                }

                // Авто-ширина колонок
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void b_appointmentReturn_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Hide();
        }

        private void b_appointmentShowActive_Click(object sender, RoutedEventArgs e)
        {
            showInactiveAppointments = false;
            b_appointmentShowActive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_appointmentShowInactive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
            searchTextBox.Text = "";
            LoadAppointmentsData();
            statusText.Text = "Отображены активные записи";
        }

        private void b_appointmentShowInactive_Click(object sender, RoutedEventArgs e)
        {
            showInactiveAppointments = true;
            b_appointmentShowInactive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_appointmentShowActive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
            searchTextBox.Text = "";
            LoadAppointmentsData();
            statusText.Text = "Отображены неактивные записи";
        }
    }
}