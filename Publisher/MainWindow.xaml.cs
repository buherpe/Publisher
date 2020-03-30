using System.ComponentModel;
using System.IO;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.Win32;
using Newtonsoft.Json;
using NLog;
using Tools;

namespace Publisher
{
    public partial class MainWindow
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public MainViewModel MainViewModel { get; set; } = new MainViewModel();

        public Timer SavingTimer = new Timer(5_000)
        {
            AutoReset = false,
            Enabled = false,
        };

        public MainWindow()
        {
            InitializeComponent();
            UpdateAvailableTextBlock.Visibility = Visibility.Collapsed;
            DataContext = MainViewModel;

            Helper.AppUpdated += OnAppUpdated;
            SavingTimer.Elapsed += (s, e) => { SaveSettings(); };
        }

        private void OnAppUpdated()
        {
            Dispatcher.Invoke(() => UpdateAvailableTextBlock.Visibility = Visibility.Visible);
        }

        private void UpdateAvailable_Click(object sender, RequestNavigateEventArgs e)
        {
            Helper.RestartApp();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (SavingTimer.Enabled)
            {
                SavingTimer.Enabled = false;
                SaveSettings();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        public void SaveSettings()
        {
            var settingsJson = JsonConvert.SerializeObject(MainViewModel.SaveThisClassPlease, Newtonsoft.Json.Formatting.Indented);
            //Log.Info($"settingsJson: {settingsJson}");

            Helper.CreateSettingsFolderIfNotExist();

            File.WriteAllText(Helper.SettingsPath, settingsJson, Encoding.UTF8);
        }

        public void LoadSettings()
        {
            if (File.Exists(Helper.SettingsPath))
            {
                var settingsJson = File.ReadAllText(Helper.SettingsPath);
                var settings = JsonConvert.DeserializeObject<SaveThisClassPlease>(settingsJson);
                MainViewModel.SaveThisClassPlease = settings;

                foreach (var project in MainViewModel.SaveThisClassPlease.Projects)
                {
                    project.LoadName();
                    project.LoadVersion();
                }
            }
            else
            {
                // first run

            }

            MainViewModel.SaveThisClassPlease.PropertyChanged += (sender, args) => RestartSavingTimer();
            MainViewModel.SaveThisClassPlease.Projects.CollectionChanged += (sender, args) => RestartSavingTimer();
            MainViewModel.SaveThisClassPlease.Projects.CollectionItemChanged += (sender, args) => RestartSavingTimer();
        }

        public void RestartSavingTimer()
        {
            //Log.Info("Start");
            SavingTimer.Stop();
            SavingTimer.Start();
        }

        private void AddProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Csproj (*.csproj)|*.csproj|All files (*.*)|*.*";
            
            if (!ofd.ShowDialog() ?? false) return;

            var project = new Project();
            project.CsprojPath = ofd.FileName;
            project.LoadName();
            project.LoadVersion();

            MainViewModel.SaveThisClassPlease.Projects.Add(project);
        }

        private void DeleteSelectedProjectButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MainViewModel.SaveThisClassPlease.Projects.RemoveAt(MainViewModel.SaveThisClassPlease.SelectedProjectIndex);
        }
    }
}