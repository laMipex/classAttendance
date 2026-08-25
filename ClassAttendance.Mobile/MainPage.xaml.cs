using ClassAttendance.Mobile.Services;
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
    private int? _activeAttendanceSessionId;
    private int? _editingLectureId;
    private ScheduleLectureResponse? _nextLecture;
    private readonly ClassAttendanceApiClient _apiClient = new();

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public ObservableCollection<QuizItem> StudentQuizzes { get; } = [];

    public ObservableCollection<QuizItem> ProfessorQuizzes { get; } = [];

    public ObservableCollection<LectureResponse> ProfessorLectures { get; } = [];

    public ObservableCollection<ScheduleItem> ScheduleItems { get; } = [];
    public ObservableCollection<AttendanceItem> AttendanceItems { get; } = [];
    public ObservableCollection<ScheduleLectureResponse> EditableLectures { get; } = [];
    public ObservableCollection<SubjectAttendanceResponse> ProfessorAttendances { get; } = [];

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
    public bool IsLectureEditorVisible => IsProfessor && _editingLectureId is not null;
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
    public string RoleDescription => UserRole;
    public string WelcomeText => $"Welcome, {UserName}";
    public string LessonStatusTitle => _nextLecture is null ? "No upcoming classes" : "Your next class starts soon";
    public string LessonStatusDetail => _nextLecture is null
        ? "Your schedule is currently empty."
        : $"{_nextLecture.SubjectName} · {_nextLecture.Room ?? "No room"}";
    public string LessonTime => _nextLecture is null
        ? string.Empty
        : $"{_nextLecture.StartsAt.ToLocalTime():dd-MM-yyyy HH:mm} - {_nextLecture.EndsAt.ToLocalTime():HH:mm}";
    public string AttendanceButtonText => _isAttendanceConfirmed ? "Attendance confirmed" : "Confirm attendance";
    public bool IsAttendanceCheckInOpen { get; private set; }
    public string AttendanceAvailabilityMessage => _isAttendanceConfirmed
        ? "Your attendance has been recorded."
        : IsAttendanceCheckInOpen
            ? "Attendance is open for this class."
            : "Attendance can be confirmed only while the class attendance session is open.";
    public string ScheduleDescription => IsStudent ? "All classes for your programme." : "Your teaching schedule.";
    public string ProfileSummary => IsStudent ? "Student account · Class attendance enabled" : "Professor account · Attendance and quiz management enabled";
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
            await LoadScheduleAsync();
            if (_isStudent)
            {
                await LoadAttendanceSessionAsync();
            }
            else
            {
                await LoadProfessorAttendancesAsync();
            }
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

    private async void OnAttendanceClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: AttendanceItem item }
            || !item.IsOpen)
        {
            return;
        }

        try
        {
            await _apiClient.CheckInAsync(item.Id, CancellationToken.None);
            await LoadAttendanceSessionAsync();
        }
        catch (AttendanceAlreadyConfirmedException)
        {
            await LoadAttendanceSessionAsync();
        }
        catch (HttpRequestException exception)
        {
            await DisplayAlertAsync(
                "Attendance could not be confirmed",
                exception.Message,
                "OK");
        }
    }

    private async void OnDashboardClicked(object? sender, EventArgs e)
    {
        ShowSection(AppSection.Dashboard);
        if (IsProfessor)
        {
            await LoadProfessorAttendancesAsync();
        }
    }
    private void OnScheduleClicked(object? sender, EventArgs e) => ShowSection(AppSection.Schedule);
    private async void OnEditLectureClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button
            || button.CommandParameter is not ScheduleLectureResponse lecture)
        {
            return;
        }

        await Navigation.PushAsync(new LectureEditPage(
            _apiClient,
            lecture,
            LoadScheduleAsync));
    }

    private async void OnSaveLectureClicked(object? sender, EventArgs e)
    {
        if (_editingLectureId is not int lectureId
            || LectureDatePicker.Date is not DateTime date
            || LectureStartTimePicker.Time is not TimeSpan startTime
            || LectureEndTimePicker.Time is not TimeSpan endTime)
        {
            return;
        }

        var startsAt = DateTime.SpecifyKind(date.Date.Add(startTime), DateTimeKind.Local);
        var endsAt = DateTime.SpecifyKind(date.Date.Add(endTime), DateTimeKind.Local);
        if (endsAt <= startsAt)
        {
            await DisplayAlertAsync("Invalid time", "End time must be after start time.", "OK");
            return;
        }

        try
        {
            await _apiClient.UpdateLectureAsync(
                lectureId,
                startsAt.ToUniversalTime(),
                endsAt.ToUniversalTime(),
                LectureRoomEntry.Text?.Trim(),
                CancellationToken.None);
            _editingLectureId = null;
            OnPropertyChanged(nameof(IsLectureEditorVisible));
            await LoadScheduleAsync();
            await DisplayAlertAsync("Saved", "Lecture schedule was updated.", "OK");
        }
        catch (HttpRequestException exception)
        {
            await DisplayAlertAsync("Unable to save", exception.Message, "OK");
        }
    }

    private void OnCancelLectureClicked(object? sender, EventArgs e)
    {
        _editingLectureId = null;
        OnPropertyChanged(nameof(IsLectureEditorVisible));
    }
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

        if (quiz.IsSubmitted)
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

        try
        {
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
        }
        catch (HttpRequestException exception)
        {
            await DisplayAlertAsync("Quiz could not be submitted", exception.Message, "OK");
            return;
        }

        await DisplayAlertAsync("Quiz submitted", "Your answer was sent.", "OK");
        await LoadQuizzesAsync();
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
        _apiClient.ClearAuthorization();
        _isStudent = false;
        _isAttendanceConfirmed = false;
        _activeAttendanceSessionId = null;
        _editingLectureId = null;
        IsAttendanceCheckInOpen = false;
        ProfessorAttendances.Clear();
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
            target.Add(new QuizItem(quiz.Id, quiz.Title, detail, quiz.IsVisible, quiz.IsSubmitted));
        }
    }

    private async Task LoadAttendanceSessionAsync()
    {
        AttendanceItems.Clear();
        _isAttendanceConfirmed = false;
        _activeAttendanceSessionId = null;
        var now = DateTime.UtcNow;
        var lectures = await _apiClient.GetStudentScheduleAsync(
            now.Date,
            now.Date.AddDays(1),
            CancellationToken.None);
        var sessions = lectures
            .OrderBy(lecture => lecture.StartsAt)
            .SelectMany(lecture => lecture.AttendanceSessions
                .OrderBy(session => session.OpenFrom)
                .Select(session => (Lecture: lecture, Session: session)))
            .ToList();
        var selectedSession = sessions.FirstOrDefault(item =>
            now >= AsUtc(item.Session.OpenFrom)
            && now <= AsUtc(item.Session.OpenUntil));
        if (selectedSession == default)
        {
            selectedSession = sessions.FirstOrDefault(item => AsUtc(item.Session.OpenFrom) > now);
        }

        if (selectedSession != default)
        {
            var confirmed = await _apiClient.GetMyCheckIn(
                selectedSession.Session.Id, CancellationToken.None) is not null;
            AttendanceItems.Add(new AttendanceItem(
                selectedSession.Session.Id,
                selectedSession.Lecture.SubjectName,
                selectedSession.Lecture.Room,
                selectedSession.Lecture.StartsAt,
                selectedSession.Lecture.EndsAt,
                confirmed,
                !confirmed
                && now >= AsUtc(selectedSession.Session.OpenFrom)
                && now <= AsUtc(selectedSession.Session.OpenUntil)));
        }

        var activeSession = AttendanceItems.FirstOrDefault(item => item.IsOpen);
        _activeAttendanceSessionId = activeSession?.Id;
        _isAttendanceConfirmed = activeSession?.IsConfirmed == true;
        IsAttendanceCheckInOpen = activeSession is not null;
        OnPropertyChanged(nameof(AttendanceButtonText));
        OnPropertyChanged(nameof(AttendanceAvailabilityMessage));
        OnPropertyChanged(nameof(IsAttendanceCheckInOpen));
    }

    private async Task LoadProfessorAttendancesAsync()
    {
        ProfessorAttendances.Clear();
        var now = DateTime.UtcNow;
        var activeLectureSession = EditableLectures
            .OrderBy(item => item.StartsAt)
            .SelectMany(lecture => lecture.AttendanceSessions
                .Select(session => (Lecture: lecture, Session: session)))
            .FirstOrDefault(item =>
                now >= AsUtc(item.Session.OpenFrom)
                && now <= AsUtc(item.Session.OpenUntil));
        if (activeLectureSession == default)
        {
            return;
        }

        var attendances = await _apiClient.GetProfessorSubjectAttendancesAsync(
            activeLectureSession.Lecture.SubjectId, CancellationToken.None);
        foreach (var attendance in attendances
            .Where(attendance => attendance.AttendanceSessionId == activeLectureSession.Session.Id)
            .OrderBy(attendance => attendance.StudentName))
        {
            ProfessorAttendances.Add(attendance);
        }
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private async Task LoadScheduleAsync()
    {
        var now = DateTime.UtcNow;
        var weekStart = StartOfScheduleWeek(now);
        var weekEnd = weekStart.AddDays(7);
        var lectures = IsStudent
            ? await _apiClient.GetStudentScheduleAsync(weekStart, weekEnd, CancellationToken.None)
            : await _apiClient.GetProfessorScheduleAsync(weekStart, weekEnd, CancellationToken.None);

        ScheduleItems.Clear();
        EditableLectures.Clear();
        _nextLecture = lectures
            .Where(lecture => AsUtc(lecture.StartsAt) > now)
            .OrderBy(lecture => lecture.StartsAt)
            .FirstOrDefault();
        foreach (var lecture in lectures)
        {
            ScheduleItems.Add(new ScheduleItem(
                lecture.StartsAt.ToLocalTime().ToString("HH:mm"),
                lecture.StartsAt.ToLocalTime().ToString("ddd"),
                lecture.SubjectName,
                $"{lecture.Room ?? "No room"} · {lecture.ProfessorName}"));
            if (IsProfessor)
            {
                EditableLectures.Add(lecture);
            }
        }

        OnPropertyChanged(nameof(LessonStatusTitle));
        OnPropertyChanged(nameof(LessonStatusDetail));
        OnPropertyChanged(nameof(LessonTime));
    }

    private static DateTime StartOfScheduleWeek(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        var start = date.Date.AddDays(-daysSinceMonday);
        return start;
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
        OnPropertyChanged(nameof(IsLectureEditorVisible));
        OnPropertyChanged(nameof(UserRole));
        OnPropertyChanged(nameof(WelcomeText));
        OnPropertyChanged(nameof(RoleDescription));
        OnPropertyChanged(nameof(ScheduleDescription));
        OnPropertyChanged(nameof(ProfileSummary));
        OnPropertyChanged(nameof(AttendanceButtonText));
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

    public sealed record QuizItem(int Id, string Title, string Detail, bool IsVisible, bool IsSubmitted)
    {
        public bool CanOpen => !IsSubmitted;
        public string VisibilityText => IsVisible ? "Visible to students" : "Hidden from students";
        public string VisibilityColor => IsVisible ? "#16803C" : "#B54708";
    }
    public sealed record AttendanceItem(
        int Id,
        string SubjectName,
        string? Room,
        DateTime StartsAt,
        DateTime EndsAt,
        bool IsConfirmed,
        bool IsOpen)
    {
        public string DisplayTime =>
            $"{StartsAt.ToLocalTime():dd-MM-yyyy HH:mm} - {EndsAt.ToLocalTime():HH:mm}";
        public string ButtonText => IsConfirmed ? "Attendance confirmed" : "Confirm attendance";
        public string StatusText => IsConfirmed
            ? "Your attendance has been recorded."
            : IsOpen
                ? "Attendance is open for this class."
                : "Attendance is not currently open.";
    }
    public sealed record ScheduleItem(string Time, string Day, string Subject, string Detail);
}
