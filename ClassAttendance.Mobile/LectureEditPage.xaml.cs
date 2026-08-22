using ClassAttendance.Mobile.Services;

namespace ClassAttendance.Mobile;

public partial class LectureEditPage : ContentPage
{
    private readonly ClassAttendanceApiClient _apiClient;
    private readonly ScheduleLectureResponse _lecture;
    private readonly Func<Task> _reloadSchedule;

    public LectureEditPage(
        ClassAttendanceApiClient apiClient,
        ScheduleLectureResponse lecture,
        Func<Task> reloadSchedule)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _lecture = lecture;
        _reloadSchedule = reloadSchedule;

        var startsAt = lecture.StartsAt.ToLocalTime();
        var endsAt = lecture.EndsAt.ToLocalTime();
        LectureSubjectLabel.Text = lecture.SubjectName;
        LectureDatePicker.Date = startsAt.Date;
        LectureStartTimePicker.Time = startsAt.TimeOfDay;
        LectureEndTimePicker.Time = endsAt.TimeOfDay;
        LectureRoomEntry.Text = lecture.Room;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (LectureDatePicker.Date is not DateTime date
            || LectureStartTimePicker.Time is not TimeSpan startTime
            || LectureEndTimePicker.Time is not TimeSpan endTime)
        {
            return;
        }

        var startsAt = DateTime.SpecifyKind(
            date.Date.Add(startTime),
            DateTimeKind.Local);
        var endsAt = DateTime.SpecifyKind(
            date.Date.Add(endTime),
            DateTimeKind.Local);
        if (endsAt <= startsAt)
        {
            await DisplayAlertAsync("Invalid time", "End time must be after start time.", "OK");
            return;
        }

        try
        {
            await _apiClient.UpdateLectureAsync(
                _lecture.Id,
                startsAt.ToUniversalTime(),
                endsAt.ToUniversalTime(),
                LectureRoomEntry.Text?.Trim(),
                CancellationToken.None);
            await _reloadSchedule();
            await Navigation.PopAsync();
        }
        catch (HttpRequestException exception)
        {
            await DisplayAlertAsync("Unable to save", exception.Message, "OK");
        }
    }
}
