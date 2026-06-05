using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecuperaArchivos.Core
{
    /// <summary>
    /// Firma de archivo para identificar tipos de archivo
    /// </summary>
    public class FileSignature
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public byte[] Signature { get; set; }

        public FileSignature(string name, string extension, byte[] signature)
        {
            Name = name;
            Extension = extension;
            Signature = signature;
        }
    }

    /// <summary>
    /// Motor de búsqueda de archivos eliminados
    /// </summary>
    public class FileRecoveryEngine
    {
        private static readonly List<FileSignature> FileSignatures = new()
        {
            // Imágenes
            new FileSignature("JPEG", ".jpg", new byte[] { 0xFF, 0xD8, 0xFF }),
            new FileSignature("PNG", ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
            new FileSignature("BMP", ".bmp", new byte[] { 0x42, 0x4D }),
            new FileSignature("GIF", ".gif", new byte[] { 0x47, 0x49, 0x46 }),
            
            // Documentos
            new FileSignature("PDF", ".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }),
            new FileSignature("DOC/DOCX", ".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
            new FileSignature("XLS/XLSX", ".xlsx", new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
            
            // Audio/Video
            new FileSignature("MP3", ".mp3", new byte[] { 0xFF, 0xFB }),
            new FileSignature("MP4", ".mp4", new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }),
            new FileSignature("AVI", ".avi", new byte[] { 0x52, 0x49, 0x46, 0x46 }),
            
            // Archivos comprimidos
            new FileSignature("ZIP", ".zip", new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
            new FileSignature("RAR", ".rar", new byte[] { 0x52, 0x61, 0x72, 0x21 }),
            
            // Ejecutables
            new FileSignature("EXE", ".exe", new byte[] { 0x4D, 0x5A }),
        };

        public delegate void ProgressChangedEventHandler(int percentage);
        public event ProgressChangedEventHandler ProgressChanged;

        /// <summary>
        /// Escanea una unidad en busca de archivos eliminados
        /// </summary>
        public List<RecoveredFile> ScanDrive(string drivePath, IProgress<ScanProgress> progress = null)
        {
            var recoveredFiles = new List<RecoveredFile>();

            try
            {
                var driveInfo = new DriveInfo(drivePath);
                long totalBytes = driveInfo.TotalSize;
                long scannedBytes = 0;

                // Escanear los archivos existentes en el sistema de archivos
                ScanDirectory(drivePath, recoveredFiles, ref scannedBytes, totalBytes, progress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al escanear: {ex.Message}");
            }

            return recoveredFiles;
        }

        private void ScanDirectory(string path, List<RecoveredFile> files, ref long scannedBytes, long totalBytes, IProgress<ScanProgress> progress)
        {
            try
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    var fileInfo = new FileInfo(file);
                    scannedBytes += fileInfo.Length;

                    var recoveredFile = new RecoveredFile
                    {
                        FileName = fileInfo.Name,
                        FilePath = fileInfo.FullName,
                        FileSize = fileInfo.Length,
                        FileType = Path.GetExtension(fileInfo.Name),
                        CreatedDate = fileInfo.CreationTime,
                        ModifiedDate = fileInfo.LastWriteTime
                    };

                    files.Add(recoveredFile);

                    // Reportar progreso
                    if (progress != null)
                    {
                        int percentage = (int)((scannedBytes * 100) / totalBytes);
                        progress.Report(new ScanProgress { Percentage = percentage, ScannedFiles = files.Count });
                    }
                }

                foreach (var directory in Directory.GetDirectories(path))
                {
                    try
                    {
                        ScanDirectory(directory, files, ref scannedBytes, totalBytes, progress);
                    }
                    catch
                    {
                        // Ignorar directorios inaccesibles
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignorar directorios protegidos
            }
        }

        /// <summary>
        /// Identifica el tipo de archivo basado en su firma
        /// </summary>
        public FileSignature IdentifyFileSignature(byte[] data)
        {
            return FileSignatures.FirstOrDefault(sig => 
                data.Length >= sig.Signature.Length && 
                data.Take(sig.Signature.Length).SequenceEqual(sig.Signature));
        }

        /// <summary>
        /// Filtra archivos por tipo
        /// </summary>
        public List<RecoveredFile> FilterByFileType(List<RecoveredFile> files, string fileType)
        {
            if (string.IsNullOrEmpty(fileType))
                return files;

            return files.Where(f => f.FileType.Equals(fileType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Filtra archivos por rango de tamaño
        /// </summary>
        public List<RecoveredFile> FilterBySize(List<RecoveredFile> files, long minSize, long maxSize)
        {
            return files.Where(f => f.FileSize >= minSize && f.FileSize <= maxSize).ToList();
        }
    }

    public class RecoveredFile
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ScanProgress
    {
        public int Percentage { get; set; }
        public int ScannedFiles { get; set; }
    }
}
