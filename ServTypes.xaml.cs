using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MySqlConnector;
using System;
using System.Linq;
using System.Windows.Media;

namespace WpfApp_Krasotka
{
    public partial class ServTypes : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";
        public ObservableCollection<ServTypesClass> servTypesCollection { get; set; }
        public ObservableCollection<ServTypesClass> allServTypesCollection { get; set; }

        private bool showInactiveServTypes = false;

        public ServTypes()
        {
            servTypesCollection = new ObservableCollection<ServTypesClass>();
            allServTypesCollection = new ObservableCollection<ServTypesClass>();
            InitializeComponent();
            servTypeCell.ItemsSource = servTypesCollection;

            // Автоматическая загрузка данных при открытии окна
            LoadServTypesData();
            UpdateStatusBar();

            // Устанавливаем начальное состояние кнопок
            b_servTypeShowActive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_servTypeShowInactive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
        }

        public class ServTypesClass
        {
            public int ServTypeCode { get; set; }
            public string ServType { get; set; }
            public string ServTypeActivity { get; set; } // Исправлено на ServTypeActivity
        }

        // Метод для загрузки данных типов услуг
        private void LoadServTypesData()
        {
            try
            {
                statusText.Text = "Загрузка данных...";

                MySqlConnection conn = new MySqlConnection(ConnectionString);
                conn.Open();

                string queryShowAll = "SELECT * FROM servTypes;";
                MySqlCommand comm = new MySqlCommand(queryShowAll, conn);
                MySqlDataReader reader = comm.ExecuteReader();

                servTypesCollection.Clear();
                allServTypesCollection.Clear();

                while (reader.Read())
                {
                    // Безопасное чтение данных с проверкой на NULL и правильными индексами
                    var servType = new ServTypesClass()
                    {
                        ServTypeCode = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        ServType = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        ServTypeActivity = reader.IsDBNull(2) ? "Да" : reader.GetString(2) // Исправлено на ServTypeActivity
                    };

                    allServTypesCollection.Add(servType);

                    // В основную коллекцию добавляем в зависимости от режима отображения
                    if (!showInactiveServTypes && servType.ServTypeActivity == "Да")
                    {
                        servTypesCollection.Add(servType);
                    }
                    else if (showInactiveServTypes && servType.ServTypeActivity == "Нет")
                    {
                        servTypesCollection.Add(servType);
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
            servTypeCountText.Text = $" Типов услуг: {servTypesCollection.Count}";
            statusText.Text = "Готово.";
        }

        // Поиск типов услуг
        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                // Если строка поиска пустая, показываем типы в зависимости от режима
                servTypesCollection.Clear();
                foreach (var servType in allServTypesCollection)
                {
                    if (!showInactiveServTypes && servType.ServTypeActivity == "Да")
                    {
                        servTypesCollection.Add(servType);
                    }
                    else if (showInactiveServTypes && servType.ServTypeActivity == "Нет")
                    {
                        servTypesCollection.Add(servType);
                    }
                }
            }
            else
            {
                // Фильтруем типы услуг по названию
                servTypesCollection.Clear();
                var filteredServTypes = allServTypesCollection.Where(s =>
                    ((!showInactiveServTypes && s.ServTypeActivity == "Да") ||
                     (showInactiveServTypes && s.ServTypeActivity == "Нет")) &&
                    (s.ServType != null && s.ServType.ToLower().Contains(searchText))
                );

                foreach (var servType in filteredServTypes)
                {
                    servTypesCollection.Add(servType);
                }
            }

            UpdateStatusBar();
        }

        private void b_servTypeShowActive_Click(object sender, RoutedEventArgs e)
        {
            showInactiveServTypes = false;
            b_servTypeShowActive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_servTypeShowInactive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
            searchTextBox.Text = "";
            LoadServTypesData();
            statusText.Text = "Отображены активные типы услуг";
        }

        private void b_servTypeShowInactive_Click(object sender, RoutedEventArgs e)
        {
            showInactiveServTypes = true;
            b_servTypeShowInactive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_servTypeShowActive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
            searchTextBox.Text = "";
            LoadServTypesData();
            statusText.Text = "Отображены неактивные типы услуг";
        }

        private void b_servTypeAdd_Click(object sender, RoutedEventArgs e)
        {
            int nextCode = GetNextServTypeCode();
            ShowServTypeCard(new ServTypesClass()
            {
                ServTypeCode = nextCode,
                ServType = "",
                ServTypeActivity = "Да" // Исправлено на ServTypeActivity
            }, true, true);
        }

        private void b_servTypeChange_Click(object sender, RoutedEventArgs e)
        {
            if (servTypeCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип услуги для изменения!");
                return;
            }

            var selectedServType = servTypeCell.SelectedItem as ServTypesClass;
            if (selectedServType != null)
            {
                ShowServTypeCard(selectedServType, true, false);
            }
        }

        private void b_servTypeDelete_Click(object sender, RoutedEventArgs e)
        {
            if (servTypeCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип услуги для удаления!");
                return;
            }

            var selectedServType = servTypeCell.SelectedItem as ServTypesClass;
            if (selectedServType == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите деактивировать тип услуги '{selectedServType.ServType}'?\nТип услуги будет скрыт из списка активных, но останется в базе данных.",
                "Подтверждение деактивации", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    statusText.Text = "Деактивация типа услуги...";

                    MySqlConnection conn = new MySqlConnection(ConnectionString);
                    conn.Open();

                    string queryUpdate = "UPDATE servTypes SET servTypeActivity = 'Нет' WHERE servTypeCode = @ServTypeCode;"; // Исправлено на servTypeActivity
                    MySqlCommand comm = new MySqlCommand(queryUpdate, conn);
                    comm.Parameters.AddWithValue("@ServTypeCode", selectedServType.ServTypeCode);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Тип услуги успешно деактивирован!");

                        // Обновляем статус в коллекциях
                        selectedServType.ServTypeActivity = "Нет"; // Исправлено на ServTypeActivity

                        // Удаляем из основной коллекции (только активные типы)
                        if (!showInactiveServTypes)
                        {
                            servTypesCollection.Remove(selectedServType);
                        }

                        // Обновляем отображение
                        servTypeCell.Items.Refresh();
                        UpdateServTypeCard(selectedServType);

                        statusText.Text = "Тип услуги деактивирован успешно.";
                        UpdateStatusBar();
                    }

                    conn.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при деактивации типа услуги: {ex.Message}");
                    statusText.Text = "Ошибка деактивации типа услуги.";
                }
            }
        }

        private void servTypeCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (servTypeCell.SelectedItem != null)
            {
                var selectedServType = servTypeCell.SelectedItem as ServTypesClass;
                if (selectedServType != null)
                {
                    UpdateServTypeCard(selectedServType);
                }
            }
            else
            {
                // Очищаем поля, если ничего не выбрано
                l_stc.Text = "";
                l_st.Text = "";
                cb_activity.SelectedIndex = 0;
            }
        }

        private void UpdateServTypeCard(ServTypesClass servType)
        {
            try
            {
                l_stc.Text = servType.ServTypeCode.ToString();
                l_st.Text = servType.ServType ?? "";

                // Безопасная установка значения активности в ComboBox
                if (servType.ServTypeActivity != null) // Исправлено на ServTypeActivity
                {
                    foreach (ComboBoxItem item in cb_activity.Items)
                    {
                        if (item.Content?.ToString() == servType.ServTypeActivity) // Исправлено на ServTypeActivity
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
                MessageBox.Show($"Ошибка при обновлении карточки типа услуги: {ex.Message}");
            }
        }

        // Метод для отображения карточки типа услуги в отдельном окне
        private void ShowServTypeCard(ServTypesClass servType, bool isEditable, bool isNewServType)
        {
            try
            {
                string windowTitle = isNewServType ? "Добавление типа услуги" : "Редактирование типа услуги";
                string headerText = isNewServType ? "Добавление типа услуги" : "Редактирование типа услуги";

                Window servTypeCardWindow = new Window()
                {
                    Title = windowTitle,
                    Width = 400,
                    Height = 400,
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

                // Поле кода типа - всегда только для чтения
                TextBox codeTextBox = new TextBox()
                {
                    Text = servType.ServTypeCode.ToString(),
                    IsReadOnly = true, // Всегда только чтение
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Код типа генерируется автоматически"
                };
                mainPanel.Children.Add(new Label() { Content = "Код типа:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(codeTextBox);

                // Поле названия типа
                TextBox nameTextBox = new TextBox()
                {
                    Text = servType.ServType ?? "",
                    IsReadOnly = !isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Название типа:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(nameTextBox);

                // Поле активности типа
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
                string currentActivity = servType.ServTypeActivity ?? "Да"; // Исправлено на ServTypeActivity
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
                            MessageBox.Show("Выберите активность типа услуги!");
                            return;
                        }
                        // Передаем isNewServType = true при добавлении, false при редактировании
                        SaveServTypeWithCustomCode(servType, nameTextBox.Text, activity, servTypeCardWindow, isNewServType);
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
                    servTypeCardWindow.Close();
                    statusText.Text = "Готово.";
                };

                buttonPanel.Children.Add(closeButton);
                mainPanel.Children.Add(buttonPanel);

                servTypeCardWindow.Content = mainPanel;

                // Устанавливаем фокус ДО показа окна
                if (isEditable && isNewServType)
                {
                    servTypeCardWindow.Loaded += (s, e) =>
                    {
                        nameTextBox.Focus();
                        nameTextBox.SelectAll();
                    };
                }

                servTypeCardWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна редактирования: {ex.Message}");
            }
        }

        // Автоматический ключ
        private int GetNextServTypeCode()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(ConnectionString);
                string query = "SELECT MAX(servTypeCode) FROM servTypes;";

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

        // Метод для сохранения типа услуги
        private void SaveServTypeWithCustomCode(ServTypesClass servType, string name, string activity, Window window, bool isNewServType = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Название типа услуги не может быть пустым!");
                return;
            }

            try
            {
                statusText.Text = "Сохранение типа услуги...";

                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    if (isNewServType) // Новый тип - используем переданный код
                    {
                        string queryInsert = @"INSERT INTO servTypes (servTypeCode, servType, servTypeActivity) 
                                      VALUES (@ServTypeCode, @ServType, @ServTypeActivity)";

                        using (MySqlCommand comm = new MySqlCommand(queryInsert, conn))
                        {
                            comm.Parameters.AddWithValue("@ServTypeCode", servType.ServTypeCode); // Используем уже сгенерированный код
                            comm.Parameters.AddWithValue("@ServType", name);
                            comm.Parameters.AddWithValue("@ServTypeActivity", activity);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show($"Новый тип услуги успешно добавлен с кодом {servType.ServTypeCode}!");
                                window.Close();
                                LoadServTypesData();
                                statusText.Text = "Готово.";
                            }
                            else
                            {
                                MessageBox.Show("Не удалось добавить тип услуги!");
                            }
                        }
                    }
                    else // Редактирование существующего типа
                    {
                        string queryUpdate = @"UPDATE servTypes SET servType = @ServType, servTypeActivity = @ServTypeActivity 
                                      WHERE servTypeCode = @ServTypeCode";

                        using (MySqlCommand comm = new MySqlCommand(queryUpdate, conn))
                        {
                            comm.Parameters.AddWithValue("@ServType", name);
                            comm.Parameters.AddWithValue("@ServTypeActivity", activity);
                            comm.Parameters.AddWithValue("@ServTypeCode", servType.ServTypeCode);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Данные типа услуги успешно обновлены!");

                                // Обновление данных в коллекциях
                                servType.ServType = name;
                                servType.ServTypeActivity = activity;

                                // Обновление отображения
                                servTypeCell.Items.Refresh();
                                UpdateServTypeCard(servType);

                                window.Close();
                                LoadServTypesData();
                                statusText.Text = "Готово.";
                            }
                            else
                            {
                                MessageBox.Show("Не удалось обновить данные типа услуги!");
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Ошибка дублирования ключа
                {
                    MessageBox.Show("Тип услуги с таким кодом уже существует!");
                    statusText.Text = "Ошибка: тип услуги с таким кодом уже существует.";
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

        private void b_servTypeReturn_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Hide();
        }
    }
}