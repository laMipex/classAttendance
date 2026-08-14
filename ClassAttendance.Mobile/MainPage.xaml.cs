using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ClassAttendance.Mobile.Services;

namespace ClassAttendance.Mobile;

public partial class MainPage : ContentPage
{
    private AppSection _activeSection = AppSection.Dashboard;
    private bool _isLoginSubmitting;
    private bool _isStudent;
    private bool _isAttendanceConfirmed;
    private readonly ClassAttendanceApiClient _apiClient = new();

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public ObservableCollection<QuizItem> StudentQuizzes { get; } = [];

    public ObservableCollection<QuizItem> ProfessorQuizzes { get; } = [];

    public ObservableCollection<LectureResponse> ProfessorLectures { get; } = [];

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
    public bool IsQuizAnswerVisible => IsAppVisible && _activeSection == AppSection.QuizAnswer;
    public bool IsQuizResponsesVisible => IsAppVisible && _activeSection == AppSection.QuizResponses;
    public bool IsTextAnswerVisible { get; private set; }
    public bool IsChoiceAnswerVisible { get; private set; }
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
    public QuizQuestionResponse? ActiveQuestion { get; private set; }
    private int _activeQuizId;
    public ObservableCollection<QuizOptionResponse> ActiveOptions { get; } = [];
    public string AnswerText { get; set; } = string.Empty;
    public QuizOptionResponse? SelectedOption { get; set; }
    public string ActiveQuizTitle { get; private set; } = string.Empty;
    public ObservableCollection<QuizQuestionResponsesResponse> ActiveQuizResponses { get; } = [];

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
            var loginResult = await _apiClient.LoginAsync(
                index.Length > 0 ? index : null,
                email.Length > 0 ? email : null,
                password,
                CancellationToken.None);

            if (loginResult is null)
            {
                ShowValidationMessage("Incorrect index/email or password.");
                return;
            }

            _isStudent = loginResult.Role == "Student";
            _apiClient.SetAuthorization(loginResult.Token);
            UserIdentifier = _isStudent ? index : email;
            UserName = loginResult.FirstName;
            _activeSection = AppSection.Dashboard;
            IsAppVisible = true;
            await LoadQuizzesAsync();
            NotifyAppStateChanged();
        }
        catch (HttpRequestException)
        {
            ShowValidationMessage("Unable to reach the server. Start the API and check the configured address.");
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
    private async void OnCreateQuizClicked(object? sender, EventArgs e)
    {
        try
        {
            ProfessorLectures.Clear();
            foreach (var lecture in await _apiClient.GetProfessorLecturesAsync(CancellationToken.None))
            {
                ProfessorLectures.Add(lecture);
            }

            ShowSection(AppSection.QuizEditor);
        }
        catch (HttpRequestException)
        {
            await DisplayAlertAsync("Unable to load classes", "The server could not be reached.", "OK");
        }
    }
    private void OnBackToDashboardClicked(object? sender, EventArgs e) => ShowSection(AppSection.Dashboard);

    private async void OnStudentQuizTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject { BindingContext: QuizItem quiz })
        {
            return;
        }

        var details = await _apiClient.GetQuizDetailsAsync(quiz.Id, CancellationToken.None);
        _activeQuizId = quiz.Id;
        ActiveQuestion = details.Questions.SingleOrDefault();
        if (ActiveQuestion is null)
        {
            return;
        }

        AnswerText = ActiveQuestion.AnswerText ?? string.Empty;
        SelectedOption = ActiveQuestion.Options.FirstOrDefault(option => option.Id == ActiveQuestion.SelectedOptionId);
        ActiveOptions.Clear();
        foreach (var option in ActiveQuestion.Options)
        {
            ActiveOptions.Add(option);
        }
        IsTextAnswerVisible = ActiveQuestion.Type == "Text";
        IsChoiceAnswerVisible = ActiveQuestion.Type == "Choice";
        ShowSection(AppSection.QuizAnswer);
        OnPropertyChanged(nameof(AnswerText));
        OnPropertyChanged(nameof(SelectedOption));
        OnPropertyChanged(nameof(IsTextAnswerVisible));
        OnPropertyChanged(nameof(IsChoiceAnswerVisible));
    }

    private async void OnProfessorQuizTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject { BindingContext: QuizItem quiz })
        {
            return;
        }

        var details = await _apiClient.GetQuizResponsesAsync(quiz.Id, CancellationToken.None);
        ActiveQuizTitle = details.Title;
        ActiveQuizResponses.Clear();
        foreach (var question in details.Questions)
        {
            ActiveQuizResponses.Add(question);
        }

        ShowSection(AppSection.QuizResponses);
        OnPropertyChanged(nameof(ActiveQuizTitle));
    }

    private async void OnPublishQuizClicked(object? sender, EventArgs e)
    {
        var title = QuizTitleEntry.Text?.Trim();
        var question = QuizQuestionEntry.Text?.Trim();
        var lecture = QuizLecturePicker.SelectedItem as LectureResponse;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(question) || lecture is null)
        {
            await DisplayAlertAsync("Missing information", "Choose a class and enter a quiz title and question.", "OK");
            return;
        }

        try
        {
            var questionType = QuizTypePicker.SelectedItem as string ?? "Text";
            var options = (QuizOptionsEntry.Text ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(option => new CreateQuizOptionRequest(option, false))
                .ToArray();
            if (questionType == "Choice" && options.Length < 2)
            {
                await DisplayAlertAsync("Missing options", "Enter at least two comma-separated options.", "OK");
                return;
            }

            await _apiClient.CreateQuizAsync(new CreateQuizRequest(lecture.Id, title, question, questionType, options), CancellationToken.None);
            QuizTitleEntry.Text = string.Empty;
            QuizQuestionEntry.Text = string.Empty;
            QuizLecturePicker.SelectedItem = null;
            await LoadQuizzesAsync();
            ShowSection(AppSection.Dashboard);
        }
        catch (HttpRequestException)
        {
            await DisplayAlertAsync("Unable to publish quiz", "The server could not be reached.", "OK");
        }
    }

    private async void OnSubmitQuizClicked(object? sender, EventArgs e)
    {
        if (ActiveQuestion is null)
        {
            return;
        }

        await _apiClient.SubmitQuizAsync(
            _activeQuizId,
            new SubmitQuizRequest(
            [
                new QuizAnswerRequest(
                    ActiveQuestion.Id,
                    IsTextAnswerVisible ? AnswerText : null,
                    IsChoiceAnswerVisible ? SelectedOption?.Id : null)
            ]),
            CancellationToken.None);
        await DisplayAlertAsync("Quiz submitted", "Your answer was sent.", "OK");
        ShowSection(AppSection.Dashboard);
    }

    private async void OnQuizVisibilityClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: QuizItem quiz })
        {
            await _apiClient.SetQuizVisibilityAsync(quiz.Id, !quiz.IsVisible, CancellationToken.None);
            await LoadQuizzesAsync();
        }
    }

    private async void OnDeleteQuizClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: QuizItem quiz }
            && await DisplayAlertAsync("Delete quiz", "Delete this quiz permanently?", "Delete", "Cancel"))
        {
            await _apiClient.DeleteQuizAsync(quiz.Id, CancellationToken.None);
            await LoadQuizzesAsync();
        }
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

    private async Task LoadQuizzesAsync()
    {
        var target = IsStudent ? StudentQuizzes : ProfessorQuizzes;
        target.Clear();

        var quizzes = IsStudent
            ? await _apiClient.GetStudentQuizzesAsync(CancellationToken.None)
            : await _apiClient.GetProfessorQuizzesAsync(CancellationToken.None);

        foreach (var quiz in quizzes)
        {
            var detail = $"{quiz.SubjectName} · {quiz.QuestionCount} question(s)";
            target.Add(new QuizItem(quiz.Id, quiz.Title, detail, quiz.IsVisible));
        }
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
        OnPropertyChanged(nameof(IsQuizAnswerVisible));
        OnPropertyChanged(nameof(IsQuizResponsesVisible));
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
        QuizEditor,
        QuizAnswer,
        QuizResponses
    }

    public sealed record QuizItem(int Id, string Title, string Detail, bool IsVisible)
    {
        public string VisibilityText => IsVisible ? "Visible to students" : "Hidden from students";
        public string VisibilityColor => IsVisible ? "#16803C" : "#B54708";
    }
    public sealed record ScheduleItem(string Time, string Day, string Subject, string Detail);
}
