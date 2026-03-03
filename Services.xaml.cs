using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MySqlConnector;
using System;
using System.Linq;
using System.Windows.Media;

namespace WpfApp_Krasotka
{
    public partial class Services : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";
        public ObservableCollection<ServicesClass> servicesCollection { get; set; }
        public ObservableCollection<ServicesClass> allServicesCollection { get; set; }
        public ObservableCollection<ServTypeComboBox> servTypesCollection { get; set; }

        private bool showInactiveServices = false;

        public Services()
        {
            servicesCollection = new ObservableCollection<ServicesClass>();
            allServicesCollection = new ObservableCollection<ServicesClass>();
            servTypesCollection = new ObservableCollection<ServTypeComboBox>();
            InitializeComponent();
            serviceCell.ItemsSource = servicesCollection;

            // Автоматическая загрузка данных при открытии окна
            LoadServTypesData();
            LoadServicesData();
            UpdateStatusBar();

            // Устанавливаем начальное состояние кнопок
            b_serviceShowActive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_serviceShowInactive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
        }

        public class ServicesClass
        {
            public int ServCode { get; set; }
            public string ServName { get; set; }
            public int ServPrice { get; set; } // Изменено с decimal на int
            public int ServDuration { get; set; }
            public int ServTypeCode { get; set; }
            public string ServTypeName { get; set; }
            public string ServiceActivity { get; set; }
        }

        public class ServTypeComboBox
        {
            public int ServTypeCode { get; set; }
            public string ServType { get; set; }
        }

        // Метод для загрузки данных типов услуг (для ComboBox)
        private void LoadServTypesData()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(ConnectionString);
                conn.Open();

                string query = "SELECT servTypeCode, servType FROM servTypes WHERE servTypeActivity = 'Да';";
                MySqlCommand comm = new MySqlCommand(query, conn);
                MySqlDataReader reader = comm.ExecuteReader();

                servTypesCollection.Clear();

                while (reader.Read())
                {
                    var servType = new ServTypeComboBox()
                    {
                        ServTypeCode = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        ServType = reader.IsDBNull(1) ? "" : reader.GetString(1)
                    };

                    servTypesCollection.Add(servType);
                }

                reader.Close();
                conn.Close();

                // Устанавливаем источник данных для ComboBox
                cb_servType.ItemsSource = servTypesCollection;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке типов услуг: {ex.Message}");
            }
        }

        // Метод для загрузки данных услуг
        private void LoadServicesData()
        {
            try
            {
                statusText.Text = "Загрузка данных...";

                MySqlConnection conn = new MySqlConnection(ConnectionString);
                conn.Open();

                string queryShowAll = @"
                    SELECT s.servCode, s.servName, s.servPrice, s.servDuration, 
                           s.servTypeCode, st.servType, s.serviceActivity 
                    FROM services s 
                    LEFT JOIN servTypes st ON s.servTypeCode = st.servTypeCode;";

                MySqlCommand comm = new MySqlCommand(queryShowAll, conn);
                MySqlDataReader reader = comm.ExecuteReader();

                servicesCollection.Clear();
                allServicesCollection.Clear();

                while (reader.Read())
                {
                    // Безопасное чтение данных с проверкой на NULL и правильными индексами
                    var service = new ServicesClass()
                    {
                        ServCode = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        ServName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        ServPrice = reader.IsDBNull(2) ? 0 : reader.GetInt32(2), // Изменено на GetInt32
                        ServDuration = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        ServTypeCode = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        ServTypeName = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        ServiceActivity = reader.IsDBNull(6) ? "Да" : reader.GetString(6)
                    };

                    allServicesCollection.Add(service);

                    // В основную коллекцию добавляем в зависимости от режима отображения
                    if (!showInactiveServices && service.ServiceActivity == "Да")
                    {
                        servicesCollection.Add(service);
                    }
                    else if (showInactiveServices && service.ServiceActivity == "Нет")
                    {
                        servicesCollection.Add(service);
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
            serviceCountText.Text = $" Услуг: {servicesCollection.Count}";
            statusText.Text = "Готово.";
        }

        // Поиск услуг
        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                // Если строка поиска пустая, показываем услуги в зависимости от режима
                servicesCollection.Clear();
                foreach (var service in allServicesCollection)
                {
                    if (!showInactiveServices && service.ServiceActivity == "Да")
                    {
                        servicesCollection.Add(service);
                    }
                    else if (showInactiveServices && service.ServiceActivity == "Нет")
                    {
                        servicesCollection.Add(service);
                    }
                }
            }
            else
            {
                // Фильтруем услуги по названию или типу услуги
                servicesCollection.Clear();
                var filteredServices = allServicesCollection.Where(s =>
                    ((!showInactiveServices && s.ServiceActivity == "Да") ||
                     (showInactiveServices && s.ServiceActivity == "Нет")) &&
                    ((s.ServName != null && s.ServName.ToLower().Contains(searchText)) ||
                     (s.ServTypeName != null && s.ServTypeName.ToLower().Contains(searchText)))
                );

                foreach (var service in filteredServices)
                {
                    servicesCollection.Add(service);
                }
            }

            UpdateStatusBar();
        }

        private void b_serviceShowActive_Click(object sender, RoutedEventArgs e)
        {
            showInactiveServices = false;
            b_serviceShowActive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_serviceShowInactive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
            searchTextBox.Text = "";
            LoadServicesData();
            statusText.Text = "Отображены активные услуги";
        }

        private void b_serviceShowInactive_Click(object sender, RoutedEventArgs e)
        {
            showInactiveServices = true;
            b_serviceShowInactive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_serviceShowActive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
            searchTextBox.Text = "";
            LoadServicesData();
            statusText.Text = "Отображены неактивные услуги";
        }

        private void b_serviceAdd_Click(object sender, RoutedEventArgs e)
        {
            int nextCode = GetNextServiceCode();
            ShowServiceCard(new ServicesClass()
            {
                ServCode = nextCode,
                ServName = "",
                ServPrice = 0,
                ServDuration = 1,
                ServTypeCode = 0,
                ServTypeName = "",
                ServiceActivity = "Да"
            }, true, true);
        }

        private void b_serviceChange_Click(object sender, RoutedEventArgs e)
        {
            if (serviceCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите услугу для изменения!");
                return;
            }

            var selectedService = serviceCell.SelectedItem as ServicesClass;
            if (selectedService != null)
            {
                ShowServiceCard(selectedService, true, false);
            }
        }

        private void b_serviceDelete_Click(object sender, RoutedEventArgs e)
        {
            if (serviceCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите услугу для удаления!");
                return;
            }

            var selectedService = serviceCell.SelectedItem as ServicesClass;
            if (selectedService == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите деактивировать услугу '{selectedService.ServName}'?\nУслуга будет скрыта из списка активных, но останется в базе данных.",
                "Подтверждение деактивации", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    statusText.Text = "Деактивация услуги...";

                    MySqlConnection conn = new MySqlConnection(ConnectionString);
                    conn.Open();

                    string queryUpdate = "UPDATE services SET serviceActivity = 'Нет' WHERE servCode = @ServCode;";
                    MySqlCommand comm = new MySqlCommand(queryUpdate, conn);
                    comm.Parameters.AddWithValue("@ServCode", selectedService.ServCode);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Услуга успешно деактивирована!");

                        // Обновляем статус в коллекциях
                        selectedService.ServiceActivity = "Нет";

                        // Удаляем из основной коллекции (только активные услуги)
                        if (!showInactiveServices)
                        {
                            servicesCollection.Remove(selectedService);
                        }

                        // Обновляем отображение
                        serviceCell.Items.Refresh();
                        UpdateServiceCard(selectedService);

                        statusText.Text = "Услуга деактивирована успешно.";
                        UpdateStatusBar();
                    }

                    conn.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при деактивации услуги: {ex.Message}");
                    statusText.Text = "Ошибка деактивации услуги.";
                }
            }
        }

        private void serviceCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serviceCell.SelectedItem != null)
            {
                var selectedService = serviceCell.SelectedItem as ServicesClass;
                if (selectedService != null)
                {
                    UpdateServiceCard(selectedService);
                }
            }
            else
            {
                // Очищаем поля, если ничего не выбрано
                l_sc.Text = "";
                l_sn.Text = "";
                l_sp.Text = "";
                l_sd.Text = "";
                cb_servType.SelectedIndex = -1;
                cb_activity.SelectedIndex = 0;
            }
        }

        private void UpdateServiceCard(ServicesClass service)
        {
            try
            {
                l_sc.Text = service.ServCode.ToString();
                l_sn.Text = service.ServName ?? "";
                l_sp.Text = service.ServPrice.ToString("N0"); // Изменено на N0 для целых чисел
                l_sd.Text = service.ServDuration.ToString();

                // Установка выбранного типа услуги в ComboBox
                foreach (ServTypeComboBox item in cb_servType.Items)
                {
                    if (item.ServTypeCode == service.ServTypeCode)
                    {
                        cb_servType.SelectedItem = item;
                        break;
                    }
                }

                // Безопасная установка значения активности в ComboBox
                if (service.ServiceActivity != null)
                {
                    foreach (ComboBoxItem item in cb_activity.Items)
                    {
                        if (item.Content?.ToString() == service.ServiceActivity)
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
                MessageBox.Show($"Ошибка при обновлении карточки услуги: {ex.Message}");
            }
        }

        // Метод для отображения карточки услуги в отдельном окне
        private void ShowServiceCard(ServicesClass service, bool isEditable, bool isNewService)
        {
            try
            {
                string windowTitle = isNewService ? "Добавление услуги" : "Редактирование услуги";
                string headerText = isNewService ? "Добавление услуги" : "Редактирование услуги";

                Window serviceCardWindow = new Window()
                {
                    Title = windowTitle,
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
                    Text = headerText,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20),
                    Foreground = Brushes.Navy
                };
                mainPanel.Children.Add(header);

                // Поле кода услуги - всегда только для чтения
                TextBox codeTextBox = new TextBox()
                {
                    Text = service.ServCode.ToString(),
                    IsReadOnly = true,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Код услуги генерируется автоматически"
                };
                mainPanel.Children.Add(new Label() { Content = "Код услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(codeTextBox);

                // Поле названия услуги
                TextBox nameTextBox = new TextBox()
                {
                    Text = service.ServName ?? "",
                    IsReadOnly = !isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Название услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(nameTextBox);

                // Поле цены
                TextBox priceTextBox = new TextBox()
                {
                    Text = service.ServPrice.ToString(),
                    IsReadOnly = !isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Цена (руб.):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(priceTextBox);

                // Поле длительности
                TextBox durationTextBox = new TextBox()
                {
                    Text = service.ServDuration.ToString(),
                    IsReadOnly = !isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Длительность (часы):", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(durationTextBox);

                // Поле типа услуги
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

                // Установка выбранного типа услуги
                foreach (ServTypeComboBox item in servTypeComboBox.Items)
                {
                    if (item.ServTypeCode == service.ServTypeCode)
                    {
                        servTypeComboBox.SelectedItem = item;
                        break;
                    }
                }

                mainPanel.Children.Add(new Label() { Content = "Тип услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(servTypeComboBox);

                // Поле активности услуги
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
                activityComboBox.Items.Add(new ComboBoxItem() { Content = "Да" });
                activityComboBox.Items.Add(new ComboBoxItem() { Content = "Нет" });

                // Безопасная установка текущего значения
                string currentActivity = service.ServiceActivity ?? "Да";
                foreach (ComboBoxItem item in activityComboBox.Items)
                {
                    if (item.Content?.ToString() == currentActivity)
                    {
                        activityComboBox.SelectedItem = item;
                        break;
                    }
                }

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
                        string activity = ((ComboBoxItem)activityComboBox.SelectedItem)?.Content?.ToString();
                        if (string.IsNullOrEmpty(activity))
                        {
                            MessageBox.Show("Выберите активность услуги!");
                            return;
                        }

                        var selectedServType = servTypeComboBox.SelectedItem as ServTypeComboBox;
                        if (selectedServType == null)
                        {
                            MessageBox.Show("Выберите тип услуги!");
                            return;
                        }

                        if (!int.TryParse(priceTextBox.Text, out int price) || price < 0)
                        {
                            MessageBox.Show("Введите корректную цену (целое число не меньше 0)!");
                            return;
                        }

                        if (!int.TryParse(durationTextBox.Text, out int duration) || duration <= 0)
                        {
                            MessageBox.Show("Введите корректную длительность (целое число больше 0)!");
                            return;
                        }

                        // Передаем isNewService = true при добавлении, false при редактировании
                        SaveServiceWithCustomCode(service, nameTextBox.Text, price, duration, selectedServType.ServTypeCode, activity, serviceCardWindow, isNewService);
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
                    serviceCardWindow.Close();
                    statusText.Text = "Готово.";
                };

                buttonPanel.Children.Add(closeButton);
                mainPanel.Children.Add(buttonPanel);

                serviceCardWindow.Content = mainPanel;

                // Устанавливаем фокус ДО показа окна
                if (isEditable && isNewService)
                {
                    serviceCardWindow.Loaded += (s, e) =>
                    {
                        nameTextBox.Focus();
                        nameTextBox.SelectAll();
                    };
                }

                serviceCardWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна редактирования: {ex.Message}");
            }
        }

        // Автоматический ключ
        private int GetNextServiceCode()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(ConnectionString);
                string query = "SELECT MAX(servCode) FROM services;";

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

        // Метод для сохранения услуги
        private void SaveServiceWithCustomCode(ServicesClass service, string name, int price, int duration, int servTypeCode, string activity, Window window, bool isNewService = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Название услуги не может быть пустым!");
                return;
            }

            try
            {
                statusText.Text = "Сохранение услуги...";

                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    if (isNewService) // Новая услуга - используем переданный код
                    {
                        string queryInsert = @"INSERT INTO services (servCode, servName, servPrice, servDuration, servTypeCode, serviceActivity) 
                                      VALUES (@ServCode, @ServName, @ServPrice, @ServDuration, @ServTypeCode, @ServiceActivity)";

                        using (MySqlCommand comm = new MySqlCommand(queryInsert, conn))
                        {
                            comm.Parameters.AddWithValue("@ServCode", service.ServCode); // Используем уже сгенерированный код
                            comm.Parameters.AddWithValue("@ServName", name);
                            comm.Parameters.AddWithValue("@ServPrice", price);
                            comm.Parameters.AddWithValue("@ServDuration", duration);
                            comm.Parameters.AddWithValue("@ServTypeCode", servTypeCode);
                            comm.Parameters.AddWithValue("@ServiceActivity", activity);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show($"Новая услуга успешно добавлена с кодом {service.ServCode}!");
                                window.Close();
                                LoadServicesData();
                                statusText.Text = "Готово.";
                            }
                            else
                            {
                                MessageBox.Show("Не удалось добавить услугу!");
                            }
                        }
                    }
                    else // Редактирование существующей услуги
                    {
                        string queryUpdate = @"UPDATE services SET servName = @ServName, servPrice = @ServPrice, 
                                      servDuration = @ServDuration, servTypeCode = @ServTypeCode, serviceActivity = @ServiceActivity 
                                      WHERE servCode = @ServCode";

                        using (MySqlCommand comm = new MySqlCommand(queryUpdate, conn))
                        {
                            comm.Parameters.AddWithValue("@ServName", name);
                            comm.Parameters.AddWithValue("@ServPrice", price);
                            comm.Parameters.AddWithValue("@ServDuration", duration);
                            comm.Parameters.AddWithValue("@ServTypeCode", servTypeCode);
                            comm.Parameters.AddWithValue("@ServiceActivity", activity);
                            comm.Parameters.AddWithValue("@ServCode", service.ServCode);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Данные услуги успешно обновлены!");

                                // Обновление данных в коллекциях
                                service.ServName = name;
                                service.ServPrice = price;
                                service.ServDuration = duration;
                                service.ServTypeCode = servTypeCode;
                                service.ServiceActivity = activity;

                                // Обновляем название типа услуги
                                var servType = servTypesCollection.FirstOrDefault(st => st.ServTypeCode == servTypeCode);
                                service.ServTypeName = servType?.ServType ?? "";

                                // Обновление отображения
                                serviceCell.Items.Refresh();
                                UpdateServiceCard(service);

                                window.Close();
                                LoadServicesData();
                                statusText.Text = "Готово.";
                            }
                            else
                            {
                                MessageBox.Show("Не удалось обновить данные услуги!");
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Ошибка дублирования ключа
                {
                    MessageBox.Show("Услуга с таким кодом уже существует!");
                    statusText.Text = "Ошибка: услуга с таким кодом уже существует.";
                }
                else
                {
                    MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}");
                    statusText.Text = "Ошибка сохранения данных.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}");
                statusText.Text = "Ошибка сохранения данных.";
            }
        }

        private void b_serviceReturn_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Hide();
        }
    }
}