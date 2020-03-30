using Tools;

namespace Publisher
{
    public class MainViewModel : ObservableObject
    {
        public string AppNameWithVersion => Helper.AppNameWithVersion;

        private SaveThisClassPlease _saveThisClassPlease = new SaveThisClassPlease();

        public SaveThisClassPlease SaveThisClassPlease
        {
            get => _saveThisClassPlease;
            set => OnPropertyChanged(ref _saveThisClassPlease, value);
        }
    }
}