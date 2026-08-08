using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ClassAttendance.Mobile;

public partial class MainPage : ContentPage
{
    private bool _isLoginVisible;
    private bool _isBusy;
    private bool _isStartupLoading = true;
    private bool _hasStarted;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public bool IsLoginVisible
    {
        get => _isLoginVisible;
        private set
        {
            if (_isLoginVisible == value)
            {
                return;
            }

            _isLoginVisible = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsHomeVisible));
        }
    }

    public bool IsHomeVisible => !IsLoginVisible;

    public bool IsStartupLoading
    {
        get => _isStartupLoading;
        private set
        {
            if (_isStartupLoading == value)
            {
                return;
            }

            _isStartupLoading = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoginSubmitting
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var index = IndexEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;


        if (string.IsNullOrWhiteSpace(index) == string.IsNullOrWhiteSpace(email))
        {
            ShowValidationMessage("Enter an index for a student or an email for a professor.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowValidationMessage("Password is required.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(index) && !Regex.IsMatch(index, @"^\d{8}$"))
        {
            ShowValidationMessage("Index must contain exactly eight digits.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(email) && !EmailAddressRegex().IsMatch(email))
        {
            ShowValidationMessage("Enter a valid email address.");
            return;
        }

        ValidationMessage.IsVisible = false;
        IsLoginSubmitting = true;

        try
        {
            await Task.Delay(250);
            UserIdentifierLabel.Text = index.Length > 0 ? index : email;
            IsLoginVisible = false;
        }
        finally
        {
            IsLoginSubmitting = false;
        }
    }

    private void ShowValidationMessage(string message)
    {
        ValidationMessage.Text = message;
        ValidationMessage.IsVisible = true;
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailAddressRegex();

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_hasStarted)
        {
            return;
        }

        _hasStarted = true;
        _ = CompleteStartupAsync();
    }

    private async Task CompleteStartupAsync()
    {
        await Task.Delay(500);
        IsStartupLoading = false;
        IsLoginVisible = true;
    }
}