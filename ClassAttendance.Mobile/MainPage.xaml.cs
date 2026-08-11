using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ClassAttendance.Mobile;

public partial class MainPage : ContentPage
{
    private AppSection _activeSection = AppSection.Dashboard;
    private bool _isLoginSubmitting;
    private bool _isStudent;
    private bool _isAttendanceConfirmed;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public ObservableCollection<QuizItem> StudentQuizzes { get; } =
    [
        new("C# fundamentals", "5 questions · 8 minutes"),
        new("Database normalization", "8 questions · Due today")
    ];

    public ObservableCollection<QuizItem> ProfessorQuizzes { get; } =
    [
        new("C# fundamentals", "Published · 18 responses"),
        new("Database normalization", "Draft")
    ];

    public ObservableCollection<ScheduleItem> ScheduleItems { get; } =
    [
        new("09:00", "Mon", "Software engineering", "Room 204 · Prof. Jovanović"),
        new("11:00", "Tue", "Databases", "Room 102 · Prof. Marković"),
        new("13:00", "Wed", "Mobile development", "Lab 3 · Prof. Petrović")
    ];

    public bool IsLoginVisible => !IsAppVisible;
    public bool IsAppVisible { get; private set; }
    public bool IsStudent => _isStudent;
    public bool IsProfessor => !_isStudent;
    public bool IsDashboardVisible => IsAppVisible && _activeSection == AppSection.Dashboard;
    public bool IsScheduleVisible => IsAppVisible && _activeSection == AppSection.Schedule;
    public bool IsProfileVisible => IsAppVisible && _activeSection == AppSection.Profile;
    public bool IsQuizEditorVisible => IsAppVisible && _activeSection == AppSection.QuizEditor;
    public bool IsLoginSubmitting
    {
        get => _isLoginSubmitting;
        private set => SetProperty(ref _isLoginSubmitting, value);
    }

    public string UserName { get; private set; } = string.Empty;
    public string UserIdentifier { get; private set; } = string.Empty;
    public string UserRole => IsStudent ? "Student" : "Professor";
    public string WelcomeText => $"Welcome, {UserName}";
    public string RoleDescription => IsStudent ? "Here is your class overview for today." : "Here is your teaching overview for today.";
    public string LessonStatusTitle => IsStudent ? "Your next class starts soon" : "Your next class starts soon";
    public string LessonStatusDetail => IsStudent ? "Software engineering · Room 204" : "Software engineering · Room 204";
    public string LessonTime => "Today · 09:00 - 10:30";
    public string AttendanceButtonText => _isAttendanceConfirmed ? "Attendance confirmed" : "Confirm attendance";
    public bool IsAttendanceCheckInOpen { get; private set; }
    public string AttendanceAvailabilityMessage => _isAttendanceConfirmed
        ? "Your attendance has been recorded."
        : IsAttendanceCheckInOpen
            ? "Attendance is open for this class."
            : "Attendance can be confirmed only while the class attendance session is open.";
    public string ScheduleDescription => IsStudent ? "All classes for your programme." : "Your teaching schedule.";
    public string ProfileSummary => IsStudent ? "Student account · Class attendance enabled" : "Professor account · Quiz publishing enabled";

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

        if (index.Length > 0 && !Regex.IsMatch(index, @"^\d{8}$"))
        {
            ShowValidationMessage("Index must contain exactly eight digits.");
            return;
        }

        if (email.Length > 0 && !EmailAddressRegex().IsMatch(email))
        {
            ShowValidationMessage("Enter a valid email address.");
            return;
        }

        ValidationMessage.IsVisible = false;
        IsLoginSubmitting = true;

        try
        {
            await Task.Delay(250);
            _isStudent = index.Length > 0;
            UserIdentifier = _isStudent ? index : email;
            UserName = _isStudent ? $"Student {index}" : email.Split('@')[0];
            _activeSection = AppSection.Dashboard;
            IsAppVisible = true;
            NotifyAppStateChanged();
        }
        finally
        {
            IsLoginSubmitting = false;
        }
    }

    private void OnAttendanceClicked(object? sender, EventArgs e)
    {
        if (!IsAttendanceCheckInOpen)
        {
            return;
        }

        _isAttendanceConfirmed = true;
        OnPropertyChanged(nameof(AttendanceButtonText));
        OnPropertyChanged(nameof(AttendanceAvailabilityMessage));
    }

    private void OnDashboardClicked(object? sender, EventArgs e) => ShowSection(AppSection.Dashboard);
    private void OnScheduleClicked(object? sender, EventArgs e) => ShowSection(AppSection.Schedule);
    private void OnProfileClicked(object? sender, EventArgs e) => ShowSection(AppSection.Profile);
    private void OnCreateQuizClicked(object? sender, EventArgs e) => ShowSection(AppSection.QuizEditor);
    private void OnBackToDashboardClicked(object? sender, EventArgs e) => ShowSection(AppSection.Dashboard);

    private void OnPublishQuizClicked(object? sender, EventArgs e)
    {
        var title = QuizTitleEntry.Text?.Trim();
        var question = QuizQuestionEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(question))
        {
            return;
        }

        ProfessorQuizzes.Insert(0, new QuizItem(title, "Published · 0 responses"));
        QuizTitleEntry.Text = string.Empty;
        QuizQuestionEntry.Text = string.Empty;
        ShowSection(AppSection.Dashboard);
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        IsAppVisible = false;
        _isAttendanceConfirmed = false;
        IsAttendanceCheckInOpen = false;
        IndexEntry.Text = string.Empty;
        EmailEntry.Text = string.Empty;
        PasswordEntry.Text = string.Empty;
        ValidationMessage.IsVisible = false;
        NotifyAppStateChanged();
    }

    private void ShowSection(AppSection section)
    {
        _activeSection = section;
        NotifyAppStateChanged();
    }

    private void ShowValidationMessage(string message)
    {
        ValidationMessage.Text = message;
        ValidationMessage.IsVisible = true;
    }

    private void NotifyAppStateChanged()
    {
        OnPropertyChanged(nameof(IsAppVisible));
        OnPropertyChanged(nameof(IsLoginVisible));
        OnPropertyChanged(nameof(IsStudent));
        OnPropertyChanged(nameof(IsProfessor));
        OnPropertyChanged(nameof(IsDashboardVisible));
        OnPropertyChanged(nameof(IsScheduleVisible));
        OnPropertyChanged(nameof(IsProfileVisible));
        OnPropertyChanged(nameof(IsQuizEditorVisible));
        OnPropertyChanged(nameof(UserRole));
        OnPropertyChanged(nameof(WelcomeText));
        OnPropertyChanged(nameof(RoleDescription));
        OnPropertyChanged(nameof(ScheduleDescription));
        OnPropertyChanged(nameof(ProfileSummary));
        OnPropertyChanged(nameof(IsAttendanceCheckInOpen));
        OnPropertyChanged(nameof(AttendanceAvailabilityMessage));
    }

    private void SetProperty(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailAddressRegex();

    private enum AppSection
    {
        Dashboard,
        Schedule,
        Profile,
        QuizEditor
    }

    public sealed record QuizItem(string Title, string Detail);
    public sealed record ScheduleItem(string Time, string Day, string Subject, string Detail);
}
