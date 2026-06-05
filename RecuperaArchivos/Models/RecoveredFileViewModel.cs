using System;
using System.ComponentModel;
using RecuperaArchivos.Core;

namespace RecuperaArchivos.Models
{
    public class RecoveredFileViewModel : INotifyPropertyChanged
    {
        private RecoveredFile _file;
        private bool _isSelected;

        public RecoveredFileViewModel(RecoveredFile file)
        {
            _file = file;
            _isSelected = false;
        }

        public string FileName => _file.FileName;
        public string FilePath => _file.FilePath;
        public long FileSize => _file.FileSize;
        public string FileType => _file.FileType;
        public DateTime CreatedDate => _file.CreatedDate;
        public DateTime ModifiedDate => _file.ModifiedDate;

        public string FileSizeFormatted
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = _file.FileSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }

        public string ModifiedDateFormatted => _file.ModifiedDate.ToString("yyyy-MM-dd HH:mm");

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
