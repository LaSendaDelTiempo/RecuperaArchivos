using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RecuperaArchivos.Core
{
    /// <summary>
    /// Clase para acceder a información de bajo nivel del disco
    /// </summary>
    public class DiskAccessor
    {
        // P/Invoke para acceso a disco
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileA(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint GENERIC_READ = 0x80000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        /// <summary>
        /// Obtiene una lista de unidades disponibles
        /// </summary>
        public static List<DriveInfo> GetAvailableDrives()
        {
            var drives = new List<DriveInfo>();
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        drives.Add(drive);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener unidades: {ex.Message}");
            }
            return drives;
        }

        /// <summary>
        /// Lee un sector específico del disco
        /// </summary>
        public static byte[] ReadSector(string drivePath, long sectorNumber, int sectorSize = 512)
        {
            try
            {
                IntPtr driveHandle = CreateFileA(
                    drivePath,
                    GENERIC_READ,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    0,
                    IntPtr.Zero);

                if (driveHandle == new IntPtr(-1))
                {
                    throw new Exception($"No se puede abrir la unidad: {Marshal.GetLastWin32Error()}");
                }

                byte[] buffer = new byte[sectorSize];
                uint bytesRead;

                // Posicionarse en el sector
                long offset = sectorNumber * sectorSize;
                
                ReadFile(driveHandle, buffer, (uint)sectorSize, out bytesRead, IntPtr.Zero);
                CloseHandle(driveHandle);

                return buffer;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al leer sector: {ex.Message}");
                return new byte[0];
            }
        }

        /// <summary>
        /// Obtiene información sobre una unidad
        /// </summary>
        public static string GetDriveInfo(string drivePath)
        {
            try
            {
                var driveInfo = new DriveInfo(drivePath);
                return $"Unidad: {driveInfo.Name}\n" +
                       $"Sistema de archivos: {driveInfo.DriveFormat}\n" +
                       $"Espacio total: {FormatBytes(driveInfo.TotalSize)}\n" +
                       $"Espacio disponible: {FormatBytes(driveInfo.AvailableFreeSpace)}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
