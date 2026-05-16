using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace BlockFactory.Desktop.ViewModels.Base
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        // ─── INotifyPropertyChanged ─────────────────
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // ─── Loading State ──────────────────────────
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // ─── Error Message ──────────────────────────
        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        // ─── Success Message ────────────────────────
        private string _successMessage = string.Empty;
        public string SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        // ─── Helper Methods ─────────────────────────
        protected void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
            SuccessMessage = string.Empty;
        }

        protected void ShowSuccess(string message)
        {
            SuccessMessage = message;
            HasError = false;
            ErrorMessage = string.Empty;
        }

        protected void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            HasError = false;
        }

        protected void RunOnUI(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }
    }
}
