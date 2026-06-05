using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using RecuperaArchivos.Core;
using RecuperaArchivos.Models;

namespace RecuperaArchivos
{
    public partial class MainWindow : Window
    {
        private FileRecoveryEngine _recoveryEngine;
        private ObservableCollection<RecoveredFileViewModel> _filesCollection;
        private bool _isScanning = false;

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            _recoveryEngine = new FileRecoveryEngine();
            _filesCollection = new ObservableCollection<RecoveredFileViewModel>();
            FilesDataGrid.ItemsSource = _filesCollection;

            LoadDrives();
            FileTypeComboBox.SelectedIndex = 0;
            if (DriveComboBox.Items.Count > 0)
                DriveComboBox.SelectedIndex = 0;
        }

        private void LoadDrives()
        {
            var drives = DiskAccessor.GetAvailableDrives();
            foreach (var drive in drives)
            {
                DriveComboBox.Items.Add(drive.Name);
            }

            if (DriveComboBox.Items.Count > 0)
            {
                DriveComboBox.SelectedIndex = 0;
                UpdateDriveInfo();
            }
        }

        private void DriveComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateDriveInfo();
        }

        private void UpdateDriveInfo()
        {
            if (DriveComboBox.SelectedItem != null)
            {
                string drivePath = DriveComboBox.SelectedItem.ToString();
                DriveInfoText.Text = DiskAccessor.GetDriveInfo(drivePath);
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning)
                return;

            _isScanning = true;
            _filesCollection.Clear();
            ScanButton.IsEnabled = false;
            RecoverButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            ScanProgressBar.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;
            ScanProgressBar.Maximum = 100;
            ScanProgressBar.Value = 0;

            try
            {
                string selectedDrive = DriveComboBox.SelectedItem.ToString();
                
                var progress = new Progress<ScanProgress>(p =>
                {
                    ScanProgressBar.Value = p.Percentage;
                    ProgressText.Text = $"Escaneo en progreso... {p.Percentage}% - {p.ScannedFiles} archivos encontrados";
                    FilesFoundText.Text = p.ScannedFiles.ToString();
                });

                var recoveredFiles = await Task.Run(() => _recoveryEngine.ScanDrive(selectedDrive, progress));

                // Aplicar filtros
                var filtered = ApplyFilters(recoveredFiles);

                foreach (var file in filtered)
                {
                    _filesCollection.Add(new RecoveredFileViewModel(file));
                }

                StatusText.Text = $"Escaneo completado. {_filesCollection.Count} archivos encontrados.";
                RecoverButton.IsEnabled = _filesCollection.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante el escaneo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error durante el escaneo";
            }
            finally
            {
                _isScanning = false;
                ScanButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
                ScanProgressBar.Visibility = Visibility.Collapsed;
                ProgressText.Visibility = Visibility.Collapsed;
            }
        }

        private System.Collections.Generic.List<RecoveredFile> ApplyFilters(System.Collections.Generic.List<RecoveredFile> files)
        {
            var filtered = files;

            // Filtrar por tipo
            if (FileTypeComboBox.SelectedItem != null)
            {
                string selectedType = FileTypeComboBox.SelectedItem.ToString();
                if (selectedType != "Todos")
                {
                    filtered = _recoveryEngine.FilterByFileType(filtered, selectedType);
                }
            }

            // Filtrar por tamaño
            if (long.TryParse(MinSizeTextBox.Text, out long minSize) &&
                long.TryParse(MaxSizeTextBox.Text, out long maxSize))
            {
                minSize *= 1024 * 1024; // Convertir MB a bytes
                maxSize *= 1024 * 1024;
                filtered = _recoveryEngine.FilterBySize(filtered, minSize, maxSize);
            }

            return filtered;
        }

        private void FilesDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            var selected = _filesCollection.Where(f => f.IsSelected).ToList();
            FilesSelectedText.Text = selected.Count.ToString();

            long totalSize = selected.Sum(f => f.FileSize);
            long totalSizeMB = totalSize / (1024 * 1024);
            TotalSizeText.Text = $"{totalSizeMB} MB";
        }

        private async void RecoverButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = _filesCollection.Where(f => f.IsSelected).ToList();

            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("Por favor, selecciona archivos para recuperar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Diálogo para seleccionar carpeta de destino
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Selecciona la carpeta donde guardar los archivos recuperados"
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string destinationPath = dialog.SelectedPath;

            RecoverButton.IsEnabled = false;
            ScanProgressBar.Visibility = Visibility.Visible;
            ScanProgressBar.Maximum = selectedFiles.Count;
            ScanProgressBar.Value = 0;

            try
            {
                int recovered = 0;
                foreach (var file in selectedFiles)
                {
                    try
                    {
                        string destFile = Path.Combine(destinationPath, file.FileName);
                        
                        // Evitar sobrescribir archivos
                        if (File.Exists(destFile))
                        {
                            destFile = Path.Combine(destinationPath, 
                                Path.GetFileNameWithoutExtension(file.FileName) + "_" + Guid.NewGuid().ToString().Substring(0, 8) + 
                                Path.GetExtension(file.FileName));
                        }

                        File.Copy(file.FilePath, destFile);
                        recovered++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al recuperar {file.FileName}: {ex.Message}");
                    }

                    ScanProgressBar.Value++;
                }

                MessageBox.Show($"Se recuperaron {recovered} archivos exitosamente en:\n{destinationPath}", 
                    "Recuperación completada", MessageBoxButton.OK, MessageBoxImage.Information);
                
                StatusText.Text = $"Recuperación completada. {recovered} archivos guardados.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la recuperación: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RecoverButton.IsEnabled = true;
                ScanProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isScanning = false;
            ScanButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
            ScanProgressBar.Visibility = Visibility.Collapsed;
            ProgressText.Visibility = Visibility.Collapsed;
            StatusText.Text = "Escaneo cancelado";
        }
    }
}
