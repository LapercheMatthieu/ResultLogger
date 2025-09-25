using MatthL.ResultLogger.Core.Enums;
using MatthL.ResultLogger.Core.Managers;
using MatthL.ResultLogger.Core.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace MatthL.ResultLogger.UI
{
    /// <summary>
    /// Logique d'interaction pour LogViewer.xaml
    /// </summary>
    public partial class LogViewer : UserControl
    {
        private ObservableCollection<LogEntry> _logs;
        private ICollectionView _logsView;
        private DispatcherTimer _refreshTimer = new DispatcherTimer();

        public LogViewer()
        {
            InitializeComponent();
            InitializeLogViewer();
        }

        private void InitializeLogViewer()
        {
            _logs = new ObservableCollection<LogEntry>();
            _logsView = CollectionViewSource.GetDefaultView(_logs);

            // Configure le filtrage
            _logsView.Filter = FilterLog;

            LogGrid.ItemsSource = _logsView;

            // Configure l'auto-refresh
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            if (AutoRefreshCheck.IsChecked == true)
            {
                _refreshTimer.Start();
            }

            // Charger les logs initiaux
            RefreshLogs();

            // Setup les event handlers pour le filtrage
            LevelFilter.SelectionChanged += (s, e) => _logsView.Refresh();
            SearchBox.TextChanged += (s, e) => _logsView.Refresh();

            // Afficher le chemin du fichier de log s'il existe
            UpdateLogFileStatus();
        }

        private bool FilterLog(object item)
        {
            if (item is not LogEntry log) return false;

            // Filtrage par niveau
            if (LevelFilter.SelectedIndex > 0)
            {
                var selectedLevel = (LogLevel)(LevelFilter.SelectedIndex - 1);
                if (log.Level != selectedLevel) return false;
            }

            // Filtrage par texte
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var searchText = SearchBox.Text.ToLower();
                return log.Message?.ToLower().Contains(searchText) == true ||
                       log.Error?.ToLower().Contains(searchText) == true ||
                       log.CallerMethod?.ToLower().Contains(searchText) == true;
            }

            return true;
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshLogs();
        }

        private void RefreshLogs()
        {
            var currentLogs = LogManager.GetLogs();

            // Ajouter seulement les nouveaux logs
            var existingTimestamps = new HashSet<DateTime>(_logs.Select(l => l.Timestamp));
            var newLogs = currentLogs.Where(l => !existingTimestamps.Contains(l.Timestamp)).ToList();

            foreach (var log in newLogs)
            {
                _logs.Add(log);
            }

            // Limiter le nombre de logs affichés
            while (_logs.Count > 10000)
            {
                _logs.RemoveAt(0);
            }

            // Auto-scroll
            if (AutoScrollCheck.IsChecked == true && _logs.Count > 0)
            {
                LogGrid.ScrollIntoView(_logs[_logs.Count - 1]);
            }

            // Mettre à jour le compteur
            CountText.Text = $"{_logs.Count} logs";
        }

        private void UpdateLogFileStatus()
        {
            var logPath = LogManager.GetLogFilePath();
            if (!string.IsNullOrEmpty(logPath))
            {
                LogFileText.Text = Path.GetFileName(logPath);
                LogFileText.ToolTip = logPath;
            }
            else
            {
                LogFileText.Text = "Memory";
                LogFileText.ToolTip = "Logs are stored in memory only";
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            _logsView.Refresh();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear all logs from memory?", "Confirm Clear",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _logs.Clear();
                LogManager.Clear();
                CountText.Text = "0 logs";
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".log",
                FileName = $"NexusDATA_Export_{DateTime.Now:yyyyMMdd_HHmmss}.log"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var logs = _logsView.Cast<LogEntry>().Select(l => l.ToString());
                    File.WriteAllLines(saveDialog.FileName, logs);
                    StatusText.Text = $"Exported {_logsView.Cast<LogEntry>().Count()} logs";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting logs: {ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var logPath = LogManager.GetLogFilePath();
            if (!string.IsNullOrEmpty(logPath) && File.Exists(logPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("No log file configured or file does not exist.", "No File",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (LogGrid.SelectedItems.Count > 0)
            {
                var selectedLogs = LogGrid.SelectedItems.Cast<LogEntry>();
                var text = string.Join(Environment.NewLine, selectedLogs.Select(l => l.ToString()));
                Clipboard.SetText(text);
                StatusText.Text = $"Copied {LogGrid.SelectedItems.Count} log(s)";
            }
        }

        private void CopyAll_Click(object sender, RoutedEventArgs e)
        {
            var allLogs = _logsView.Cast<LogEntry>();
            var text = string.Join(Environment.NewLine, allLogs.Select(l => l.ToString()));
            Clipboard.SetText(text);
            StatusText.Text = $"Copied all {allLogs.Count()} log(s)";
        }

        private void GoToFile_Click(object sender, RoutedEventArgs e)
        {
            if (LogGrid.SelectedItem is LogEntry log && !string.IsNullOrEmpty(log.CallerFile))
            {
                if (File.Exists(log.CallerFile))
                {
                    // Ouvrir le fichier dans l'éditeur par défaut
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = log.CallerFile,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show($"File not found: {log.CallerFile}", "File Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void AutoRefresh_Changed(object sender, RoutedEventArgs e)
        {
            if (AutoRefreshCheck.IsChecked == true)
            {
                _refreshTimer.Start();
            }
            else
            {
                _refreshTimer.Stop();
            }
        }
    }

}
