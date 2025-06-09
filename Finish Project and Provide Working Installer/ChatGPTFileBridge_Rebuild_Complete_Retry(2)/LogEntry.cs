// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32; // For SaveFileDialog

namespace WinBridgeAgentControlPanel.Models
{
    // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
    public class LogEntry : INotifyPropertyChanged
    {
        private DateTime _timestamp;
        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(nameof(Timestamp)); }
        }

        private string? _level;
        public string? Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(nameof(Level)); }
        }

        private string? _message;
        public string? Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(nameof(Message)); }
        }

        // Add other properties from your JSON log structure if needed
        // For example, if your logs have an "Exception" field or "SourceContext"
        public string? RenderedMessage { get; set; } // Often logs have a pre-rendered message
        public string? Exception { get; set; } // For exception details
        public JsonElement Properties { get; set; } // For any other structured properties

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

