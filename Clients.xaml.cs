using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MySqlConnector;
using System;
using System.Linq;
using System.Windows.Media;

namespace WpfApp_Krasotka
{
    public partial class Clients : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";
        public ObservableCollection<ClientsClass> clientsCollection { get; set; }
        public ObservableCollection<ClientsClass> allClientsCollection { get; set; }

        private bool showInactiveClients = false;

        public Clients()
        {
            clientsCollection = new ObservableCollection<ClientsClass>();
            allClientsCollection = new ObservableCollection<ClientsClass>();
            InitializeComponent();
            clientCell.ItemsSource = clientsCollection;

            // Автоматическая загрузка данных при открытии окна
            LoadClientsData();
            UpdateStatusBar();

            // Устанавливаем начальное состояние кнопок
            b_clientShowActive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_clientShowInactive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
        }

        public class ClientsClass
        {
            public int ClientCode { get; set; }
            public string ClientName { get; set; }
            public string ClientTel { get; set; }
            public string ClientsActivity { get; set; }
        }

        // Метод для загрузки данных клиентов
        private void LoadClientsData()
        {
            try
            {
                statusText.Text = "Загрузка данных...";

                MySqlConnection conn = new MySqlConnection(ConnectionString);
                conn.Open();

                string queryShowAll = "SELECT * FROM clients;";
                MySqlCommand comm = new MySqlCommand(queryShowAll, conn);
                MySqlDataReader reader = comm.ExecuteReader();

                clientsCollection.Clear();
                allClientsCollection.Clear();

                while (reader.Read())
                {
                    // Безопасное чтение данных с проверкой на NULL и правильными индексами
                    var client = new ClientsClass()
                    {
                        ClientCode = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        ClientName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        ClientTel = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        ClientsActivity = reader.IsDBNull(3) ? "Да" : reader.GetString(3)
                    };

                    allClientsCollection.Add(client);

                    // В основную коллекцию добавляем в зависимости от режима отображения
                    if (!showInactiveClients && client.ClientsActivity == "Да")
                    {
                        clientsCollection.Add(client);
                    }
                    else if (showInactiveClients && client.ClientsActivity == "Нет")
                    {
                        clientsCollection.Add(client);
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
            clientCountText.Text = $" Клиентов: {clientsCollection.Count}";
            statusText.Text = "Готово.";
        }

        // Поиск клиентов
        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                // Если строка поиска пустая, показываем клиентов в зависимости от режима
                clientsCollection.Clear();
                foreach (var client in allClientsCollection)
                {
                    if (!showInactiveClients && client.ClientsActivity == "Да")
                    {
                        clientsCollection.Add(client);
                    }
                    else if (showInactiveClients && client.ClientsActivity == "Нет")
                    {
                        clientsCollection.Add(client);
                    }
                }
            }
            else
            {
                // Фильтруем клиентов по имени или телефону
                clientsCollection.Clear();
                var filteredClients = allClientsCollection.Where(c =>
                    ((!showInactiveClients && c.ClientsActivity == "Да") ||
                     (showInactiveClients && c.ClientsActivity == "Нет")) &&
                    ((c.ClientName != null && c.ClientName.ToLower().Contains(searchText)) ||
                     (c.ClientTel != null && c.ClientTel.ToLower().Contains(searchText)))
                );

                foreach (var client in filteredClients)
                {
                    clientsCollection.Add(client);
                }
            }

            UpdateStatusBar();
        }

        private void b_clientShowActive_Click(object sender, RoutedEventArgs e)
        {
            showInactiveClients = false;
            b_clientShowActive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_clientShowInactive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
            searchTextBox.Text = "";
            LoadClientsData();
            statusText.Text = "Отображены активные клиенты";
        }

        private void b_clientShowInactive_Click(object sender, RoutedEventArgs e)
        {
            showInactiveClients = true;
            b_clientShowInactive.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            b_clientShowActive.Background = new SolidColorBrush(Color.FromRgb(120, 144, 156)); // Серый
            searchTextBox.Text = "";
            LoadClientsData();
            statusText.Text = "Отображены неактивные клиенты";
        }

        private void b_clientAdd_Click(object sender, RoutedEventArgs e)
        {
            int nextCode = GetNextClientCode();
            ShowClientCard(new ClientsClass()
            {
                ClientCode = nextCode,
                ClientName = "",
                ClientTel = "",
                ClientsActivity = "Да"
            }, true, true);
        }

        private void b_clientChange_Click(object sender, RoutedEventArgs e)
        {
            if (clientCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента для изменения!");
                return;
            }

            var selectedClient = clientCell.SelectedItem as ClientsClass;
            if (selectedClient != null)
            {
                ShowClientCard(selectedClient, true, false);
            }
        }

        private void b_clientDelete_Click(object sender, RoutedEventArgs e)
        {
            if (clientCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента для удаления!");
                return;
            }

            var selectedClient = clientCell.SelectedItem as ClientsClass;
            if (selectedClient == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите деактивировать клиента {selectedClient.ClientName}?\nКлиент будет скрыт из списка активных, но останется в базе данных.",
                "Подтверждение деактивации", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    statusText.Text = "Деактивация клиента...";

                    MySqlConnection conn = new MySqlConnection(ConnectionString);
                    conn.Open();

                    string queryUpdate = "UPDATE clients SET clientsActivity = 'Нет' WHERE ClientCode = @ClientCode;";
                    MySqlCommand comm = new MySqlCommand(queryUpdate, conn);
                    comm.Parameters.AddWithValue("@ClientCode", selectedClient.ClientCode);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Клиент успешно деактивирован!");

                        // Обновляем статус в коллекциях
                        selectedClient.ClientsActivity = "Нет";

                        // Удаляем из основной коллекции (только активные клиенты)
                        if (!showInactiveClients)
                        {
                            clientsCollection.Remove(selectedClient);
                        }

                        // Обновляем отображение
                        clientCell.Items.Refresh();
                        UpdateClientCard(selectedClient);

                        statusText.Text = "Клиент деактивирован успешно.";
                        UpdateStatusBar();
                    }

                    conn.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при деактивации клиента: {ex.Message}");
                    statusText.Text = "Ошибка деактивации клиента.";
                }
            }
        }

        private void clientCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (clientCell.SelectedItem != null)
            {
                var selectedClient = clientCell.SelectedItem as ClientsClass;
                if (selectedClient != null)
                {
                    UpdateClientCard(selectedClient);
                }
            }
            else
            {
                // Очищаем поля, если ничего не выбрано
                l_cc.Text = "";
                l_cn.Text = "";
                l_ct.Text = "";
                cb_activity.SelectedIndex = 0;
            }
        }

        private void UpdateClientCard(ClientsClass client)
        {
            try
            {
                l_cc.Text = client.ClientCode.ToString();
                l_cn.Text = client.ClientName ?? "";
                l_ct.Text = client.ClientTel ?? "";

                // Безопасная установка значения активности в ComboBox
                if (client.ClientsActivity != null)
                {
                    foreach (ComboBoxItem item in cb_activity.Items)
                    {
                        if (item.Content?.ToString() == client.ClientsActivity)
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
                MessageBox.Show($"Ошибка при обновлении карточки клиента: {ex.Message}");
            }
        }

        // Метод для отображения карточки клиента в отдельном окне
        private void ShowClientCard(ClientsClass client, bool isEditable, bool isNewClient)
        {
            try
            {
                string windowTitle = isNewClient ? "Добавление клиента" : "Редактирование клиента";
                string headerText = isNewClient ? "Добавление клиента" : "Редактирование клиента";

                Window clientCardWindow = new Window()
                {
                    Title = windowTitle,
                    Width = 400,
                    Height = 450,
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

                // Поле кода клиента - всегда только для чтения
                TextBox codeTextBox = new TextBox()
                {
                    Text = client.ClientCode.ToString(),
                    IsReadOnly = true, // Всегда только чтение
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    ToolTip = "Код клиента генерируется автоматически"
                };
                mainPanel.Children.Add(new Label() { Content = "Код клиента:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(codeTextBox);

                // Поле имени клиента
                TextBox nameTextBox = new TextBox()
                {
                    Text = client.ClientName ?? "",
                    IsReadOnly = !isEditable,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14,
                    Height = 30,
                    Padding = new Thickness(5),
                    Background = isEditable ? Brushes.White : Brushes.LightGray,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1)
                };
                mainPanel.Children.Add(new Label() { Content = "Имя клиента:", FontWeight = FontWeights.SemiBold });
                mainPanel.Children.Add(nameTextBox);

                // Поле телефона клиента
                TextBox telTextBox = new TextBox()
                {
                    Text = client.ClientTel ?? "",
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

                // Поле активности клиента
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
                string currentActivity = client.ClientsActivity ?? "Да";
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
                            MessageBox.Show("Выберите активность клиента!");
                            return;
                        }
                        // Передаем isNewClient = true при добавлении, false при редактировании
                        SaveClientWithCustomCode(client, nameTextBox.Text, telTextBox.Text, activity, clientCardWindow, isNewClient);
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
                    clientCardWindow.Close();
                    statusText.Text = "Готово.";
                };

                buttonPanel.Children.Add(closeButton);
                mainPanel.Children.Add(buttonPanel);

                clientCardWindow.Content = mainPanel;

                // Устанавливаем фокус ДО показа окна
                if (isEditable && isNewClient)
                {
                    clientCardWindow.Loaded += (s, e) =>
                    {
                        nameTextBox.Focus();
                        nameTextBox.SelectAll();
                    };
                }

                clientCardWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна редактирования: {ex.Message}");
            }
        }

        // Автоматический ключ
        private int GetNextClientCode()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(ConnectionString);
                string query = "SELECT MAX(ClientCode) FROM clients;";

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

        // Метод для сохранения клиента
        private void SaveClientWithCustomCode(ClientsClass client, string name, string tel, string activity, Window window, bool isNewClient = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Имя клиента не может быть пустым!");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    if (isNewClient) // Новый клиент - используем переданный код
                    {
                        string query = "INSERT INTO clients (ClientCode, ClientName, ClientTel, clientsActivity) VALUES (@code, @name, @tel, @activity)";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@code", client.ClientCode); // Используем уже сгенерированный код
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@tel", tel);
                        cmd.Parameters.AddWithValue("@activity", activity);

                        int result = cmd.ExecuteNonQuery();

                        if (result > 0)
                        {
                            MessageBox.Show($"Клиент успешно добавлен с кодом {client.ClientCode}!");
                            window.Close();
                            LoadClientsData();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось добавить клиента!");
                        }
                    }
                    else // Редактирование существующего клиента
                    {
                        string query = "UPDATE clients SET ClientName = @name, ClientTel = @tel, clientsActivity = @activity WHERE ClientCode = @code";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@tel", tel);
                        cmd.Parameters.AddWithValue("@activity", activity);
                        cmd.Parameters.AddWithValue("@code", client.ClientCode);

                        int result = cmd.ExecuteNonQuery();

                        if (result > 0)
                        {
                            MessageBox.Show("Данные клиента обновлены!");

                            // Обновляем локальные данные
                            client.ClientName = name;
                            client.ClientTel = tel;
                            client.ClientsActivity = activity;

                            clientCell.Items.Refresh();
                            UpdateClientCard(client);

                            window.Close();
                            LoadClientsData();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось обновить данные. Возможно, клиент не найден.");
                        }
                    }
                }
            }
            catch (MySqlException ex) when (ex.Number == 1062) // Дублирование ключа
            {
                MessageBox.Show($"Клиент с кодом {client.ClientCode} уже существует! Попробуйте снова.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void b_clientReturn_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Hide();
        }
    }
}