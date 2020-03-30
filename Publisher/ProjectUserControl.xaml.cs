using System.ComponentModel;
using System.Windows;

namespace Publisher
{
    public partial class ProjectUserControl
    {
        public Project Project { get; set; }

        public ProjectUserControl()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //avoid a "object reference not set to an instance of an object@ exception in XAML code while design time
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            Project = (Project) DataContext;
        }

        private void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            Project.LoadName();
            Project.LoadVersion();

            if (MessageBox.Show($"Опубликовать {Project.Name} v{Project.Version}?", "Публикация", MessageBoxButton.YesNo) == MessageBoxResult.No) return;

            Project.Publish();
        }

        private void LoadNameButton_Click(object sender, RoutedEventArgs e)
        {
            Project.LoadName();
        }

        private void LoadVersionButton_Click(object sender, RoutedEventArgs e)
        {
            Project.LoadVersion();
        }

        private void NugetPackButton_Click(object sender, RoutedEventArgs e)
        {
            Project.NugetPack();
        }
    }
}