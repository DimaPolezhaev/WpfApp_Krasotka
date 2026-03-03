using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MySqlConnector;
using System;
using System.Linq;
using System.Windows.Media;

namespace WpfApp_Krasotka
{
    public partial class Masters : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";
        public ObservableCollection<MastersClass> mastersCollection { get; set; }
        public ObservableCollection<MastersClass> allMastersCollection { get; set; }
        public ObservableCollection<ServTypeComboBox> servTypesCollection { get; set; }

        private bool showInactiveMasters = false;

        public Masters()
        {
            mastersCollection = new ObservableCollection<MastersClass>();
            allMastersCollection = new ObservableCollection<MastersClass>();
            servTypesCollection = new ObservableCollection<ServTypeComboBox>();
            InitializeComponent();
            masterCell.ItemsSource = mastersCollection;

            // Автоматическая загрузка данных при открытии окна
            LoadServTypesData();
            LoadMastersData();
            UpdateStatusBar();

            // Устанавливаем начальное состояние кнопок
            b_masterShowActive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_masterShowInactive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
        }

        public class MastersClass
        {
            public int MasterCode { get; set; }
            public string MasterName { get; set; }
            public string MasterTel { get; set; }
            public int ServTypeCode { get; set; }
            public string ServTypeName { get; set; }
            public string MasterActivity { get; set; } // Исправлено на MasterActivity
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

                string query = "SELECT servTypeCode, servType FROM servTypes WHERE servTypeActivity = 'Да';"; // Исправлено на servTypeActivity
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

        // Метод для загрузки данных мастеров
        private void LoadMastersData()
        {
            try
            {
                statusText.Text = "Загрузка данных...";

                MySqlConnection conn = new MySqlConnection(ConnectionString);
                conn.Open();

                string queryShowAll = @"
                    SELECT m.masterCode, m.masterName, m.masterTel, m.servTypeCode, 
                           st.servType, m.masterActivity 
                    FROM masters m 
                    LEFT JOIN servTypes st ON m.servTypeCode = st.servTypeCode;"; // Исправлено на masterActivity

                MySqlCommand comm = new MySqlCommand(queryShowAll, conn);
                MySqlDataReader reader = comm.ExecuteReader();

                mastersCollection.Clear();
                allMastersCollection.Clear();

                while (reader.Read())
                {
                    // Безопасное чтение данных с проверкой на NULL и правильными индексами
                    var master = new MastersClass()
                    {
                        MasterCode = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        MasterName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        MasterTel = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        ServTypeCode = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        ServTypeName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        MasterActivity = reader.IsDBNull(5) ? "Да" : reader.GetString(5) // Исправлено на MasterActivity
                    };

                    allMastersCollection.Add(master);

                    // В основную коллекцию добавляем в зависимости от режима отображения
                    if (!showInactiveMasters && master.MasterActivity == "Да") // Исправлено на MasterActivity
                    {
                        mastersCollection.Add(master);
                    }
                    else if (showInactiveMasters && master.MasterActivity == "Нет") // Исправлено на MasterActivity
                    {
                        mastersCollection.Add(master);
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
            masterCountText.Text = $" Мастеров: {mastersCollection.Count}";
            statusText.Text = "Готово.";
        }

        // Поиск мастеров
        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                // Если строка поиска пустая, показываем мастеров в зависимости от режима
                mastersCollection.Clear();
                foreach (var master in allMastersCollection)
                {
                    if (!showInactiveMasters && master.MasterActivity == "Да") // Исправлено на MasterActivity
                    {
                        mastersCollection.Add(master);
                    }
                    else if (showInactiveMasters && master.MasterActivity == "Нет") // Исправлено на MasterActivity
                    {
                        mastersCollection.Add(master);
                    }
                }
            }
            else
            {
                // Фильтруем мастеров по имени, телефону или типу услуги
                mastersCollection.Clear();
                var filteredMasters = allMastersCollection.Where(m =>
                    ((!showInactiveMasters && m.MasterActivity == "Да") || // Исправлено на MasterActivity
                     (showInactiveMasters && m.MasterActivity == "Нет")) && // Исправлено на MasterActivity
                    ((m.MasterName != null && m.MasterName.ToLower().Contains(searchText)) ||
                     (m.MasterTel != null && m.MasterTel.ToLower().Contains(searchText)) ||
                     (m.ServTypeName != null && m.ServTypeName.ToLower().Contains(searchText)))
                );

                foreach (var master in filteredMasters)
                {
                    mastersCollection.Add(master);
                }
            }

            UpdateStatusBar();
        }

        private void b_masterShowActive_Click(object sender, RoutedEventArgs e)
        {
            showInactiveMasters = false;
            b_masterShowActive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_masterShowInactive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
            searchTextBox.Text = "";
            LoadMastersData();
            statusText.Text = "Отображены активные мастера";
        }

        private void b_masterShowInactive_Click(object sender, RoutedEventArgs e)
        {
            showInactiveMasters = true;
            b_masterShowInactive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_masterShowActive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
            searchTextBox.Text = "";
            LoadMastersData();
            statusText.Text = "Отображены неактивные мастера";
        }

        private void b_masterAdd_Click(object sender, RoutedEventArgs e)
        {
            int nextCode = GetNextMasterCode();
            ShowMasterCard(new MastersClass()
            {
                MasterCode = nextCode,
                MasterName = "",
                MasterTel = "",
                ServTypeCode = 0,
                ServTypeName = "",
                MasterActivity = "Да" // Исправлено на MasterActivity
            }, true, true);
        }

        private void b_masterChange_Click(object sender, RoutedEventArgs e)
        {
            if (masterCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите мастера для изменения!");
                return;
            }

            var selectedMaster = masterCell.SelectedItem as MastersClass;
            if (selectedMaster != null)
            {
                ShowMasterCard(selectedMaster, true, false);
            }
        }

        private void b_masterDelete_Click(object sender, RoutedEventArgs e)
        {
            if (masterCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите мастера для удаления!");
                return;
            }

            var selectedMaster = masterCell.SelectedItem as MastersClass;
            if (selectedMaster == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите деактивировать мастера {selectedMaster.MasterName}?\nМастер будет скрыт из списка активных, но останется в базе данных.",
                "Подтверждение деактивации", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    statusText.Text = "Деактивация мастера...";

                    MySqlConnection conn = new MySqlConnection(ConnectionString);
                    conn.Open();

                    string queryUpdate = "UPDATE masters SET masterActivity = 'Нет' WHERE masterCode = @MasterCode;"; // Исправлено на masterActivity
                    MySqlCommand comm = new MySqlCommand(queryUpdate, conn);
                    comm.Parameters.AddWithValue("@MasterCode", selectedMaster.MasterCode);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Мастер успешно деактивирован!");

                        // Обновляем статус в коллекциях
                        selectedMaster.MasterActivity = "Нет"; // Исправлено на MasterActivity

                        // Удаляем из основной коллекции (только активные мастера)
                        if (!showInactiveMasters)
                        {
                            mastersCollection.Remove(selectedMaster);
                        }

                        // Обновляем отображение
                        masterCell.Items.Refresh();
                        UpdateMasterCard(selectedMaster);

                        statusText.Text = "Мастер деактивирован успешно.";
                        UpdateStatusBar();
                    }

                    conn.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при деактивации мастера: {ex.Message}");
                    statusText.Text = "Ошибка деактивации мастера.";
                }
            }
        }

        private void masterCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (masterCell.SelectedItem != null)
            {
                var selectedMaster = masterCell.SelectedItem as MastersClass;
                if (selectedMaster != null)
                {
                    UpdateMasterCard(selectedMaster);
                }
            }
            else
            {
                // Очищаем поля, если ничего не выбрано
                l_mc.Text = "";
                l_mn.Text = "";
                l_mt.Text = "";
                cb_servType.SelectedIndex = -1;
                cb_activity.SelectedIndex = 0;
            }
        }

        private void UpdateMasterCard(MastersClass master)
        {
            try
            {
                l_mc.Text = master.MasterCode.ToString();
                l_mn.Text = master.MasterName ?? "";
                l_mt.Text = master.MasterTel ?? "";

                // Установка выбранного типа услуги в ComboBox
                foreach (ServTypeComboBox item in cb_servType.Items)
                {
                    if (item.ServTypeCode == master.ServTypeCode)
                    {
                        cb_servType.SelectedItem = item;
                        break;
                    }
                }

                // Безопасная установка значения активности в ComboBox
                if (master.MasterActivity != null) // Исправлено на MasterActivity
                {
                    foreach (ComboBoxItem item in cb_activity.Items)
                    {
                        if (item.Content?.ToString() == master.MasterActivity) // Исправлено на MasterActivity
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
                MessageBox.Show($"Ошибка при обновлении карточки мастера: {ex.Message}");
            }
        }

        // Метод для отображения карточки мастера в отдельном окне
        private void ShowMasterCard(MastersClass master, bool isEditable, bool isNewMaster)
        {
            try
            {
                string windowTitle = isNewMaster ? "Добавление мастера" : "Редактирование мастера";
                string headerText = isNewMaster ? "Добавление мастера" : "Редактирование мастера";

                Window masterCardWindow = new Window()
                {
                    Title = windowTitle,
                    Width = 450,
                    Height = 520,
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

                // Поле кода мастера - всегда только для чтения
                TextBox codeTextBox = new TextBox()
                {
                    Text = master.MasterCode.ToString(),
                    IsReadOnly = true, // Всегда только чтение
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Код мастера генерируется автоматически"
                };
                mainPanel.Children.Add(new Label() { Content = "Код мастера:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(codeTextBox);

                // Поле имени мастера
                TextBox nameTextBox = new TextBox()
                {
                    Text = master.MasterName ?? "",
                    IsReadOnly = !isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Имя мастера:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(nameTextBox);

                // Поле телефона мастера
                TextBox telTextBox = new TextBox()
                {
                    Text = master.MasterTel ?? "",
                    IsReadOnly = !isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Телефон:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(telTextBox);

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
                    if (item.ServTypeCode == master.ServTypeCode)
                    {
                        servTypeComboBox.SelectedItem = item;
                        break;
                    }
                }

                mainPanel.Children.Add(new Label() { Content = "Тип услуги:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(servTypeComboBox);

                // Поле активности мастера
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
                string currentActivity = master.MasterActivity ?? "Да"; // Исправлено на MasterActivity
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
                            MessageBox.Show("Выберите активность мастера!");
                            return;
                        }

                        var selectedServType = servTypeComboBox.SelectedItem as ServTypeComboBox;
                        if (selectedServType == null)
                        {
                            MessageBox.Show("Выберите тип услуги!");
                            return;
                        }

                        // Передаем isNewMaster = true при добавлении, false при редактировании
                        SaveMasterWithCustomCode(master, nameTextBox.Text, telTextBox.Text, selectedServType.ServTypeCode, activity, masterCardWindow, isNewMaster);
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
                    masterCardWindow.Close();
                    statusText.Text = "Готово.";
                };

                buttonPanel.Children.Add(closeButton);
                mainPanel.Children.Add(buttonPanel);

                masterCardWindow.Content = mainPanel;

                // Устанавливаем фокус ДО показа окна
                if (isEditable && isNewMaster)
                {
                    masterCardWindow.Loaded += (s, e) =>
                    {
                        nameTextBox.Focus();
                        nameTextBox.SelectAll();
                    };
                }

                masterCardWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна редактирования: {ex.Message}");
            }
        }

        // Автоматический ключ
        private int GetNextMasterCode()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(ConnectionString);
                string query = "SELECT MAX(masterCode) FROM masters;";

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

        // Метод для сохранения мастера
        private void SaveMasterWithCustomCode(MastersClass master, string name, string tel, int servTypeCode, string activity, Window window, bool isNewMaster = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Имя мастера не может быть пустым!");
                return;
            }

            if (string.IsNullOrWhiteSpace(tel))
            {
                MessageBox.Show("Телефон мастера не может быть пустым!");
                return;
            }

            try
            {
                statusText.Text = "Сохранение мастера...";

                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    if (isNewMaster) // Новый мастер - используем переданный код
                    {
                        string queryInsert = @"INSERT INTO masters (masterCode, masterName, masterTel, servTypeCode, masterActivity) 
                                      VALUES (@MasterCode, @MasterName, @MasterTel, @ServTypeCode, @MasterActivity)";

                        using (MySqlCommand comm = new MySqlCommand(queryInsert, conn))
                        {
                            comm.Parameters.AddWithValue("@MasterCode", master.MasterCode); // Используем уже сгенерированный код
                            comm.Parameters.AddWithValue("@MasterName", name);
                            comm.Parameters.AddWithValue("@MasterTel", tel);
                            comm.Parameters.AddWithValue("@ServTypeCode", servTypeCode);
                            comm.Parameters.AddWithValue("@MasterActivity", activity);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show($"Новый мастер успешно добавлен с кодом {master.MasterCode}!");
                                window.Close();
                                LoadMastersData();
                                statusText.Text = "Готово.";
                            }
                            else
                            {
                                MessageBox.Show("Не удалось добавить мастера!");
                            }
                        }
                    }
                    else // Редактирование существующего мастера
                    {
                        string queryUpdate = @"UPDATE masters SET masterName = @MasterName, masterTel = @MasterTel, 
                                      servTypeCode = @ServTypeCode, masterActivity = @MasterActivity 
                                      WHERE masterCode = @MasterCode";

                        using (MySqlCommand comm = new MySqlCommand(queryUpdate, conn))
                        {
                            comm.Parameters.AddWithValue("@MasterName", name);
                            comm.Parameters.AddWithValue("@MasterTel", tel);
                            comm.Parameters.AddWithValue("@ServTypeCode", servTypeCode);
                            comm.Parameters.AddWithValue("@MasterActivity", activity);
                            comm.Parameters.AddWithValue("@MasterCode", master.MasterCode);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Данные мастера успешно обновлены!");

                                // Обновление данных в коллекциях
                                master.MasterName = name;
                                master.MasterTel = tel;
                                master.ServTypeCode = servTypeCode;
                                master.MasterActivity = activity;

                                // Обновляем название типа услуги
                                var servType = servTypesCollection.FirstOrDefault(st => st.ServTypeCode == servTypeCode);
                                master.ServTypeName = servType?.ServType ?? "";

                                // Обновление отображения
                                masterCell.Items.Refresh();
                                UpdateMasterCard(master);

                                window.Close();
                                LoadMastersData();
                                statusText.Text = "Готово.";
                            }
                            else
                            {
                                MessageBox.Show("Не удалось обновить данные мастера!");
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Ошибка дублирования ключа
                {
                    MessageBox.Show("Мастер с таким кодом уже существует!");
                    statusText.Text = "Ошибка: мастер с таким кодом уже существует.";
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

        private void b_masterReturn_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Hide();
        }
    }
}