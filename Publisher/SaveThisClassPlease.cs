using Tools;

namespace Publisher
{
    public class SaveThisClassPlease : ObservableObject
    {
        private TrulyObservableCollection<Project> _projects = new TrulyObservableCollection<Project>();

        public TrulyObservableCollection<Project> Projects
        {
            get => _projects;
            set => OnPropertyChanged(ref _projects, value);
        }

        private int _selectedProjectIndex;

        public int SelectedProjectIndex
        {
            get => _selectedProjectIndex;
            set => OnPropertyChanged(ref _selectedProjectIndex, value);
        }
    }
}