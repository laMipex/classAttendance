namespace ClassAttendance.Api.Contracts.Quiz;

public sealed record QuizDetailsResponse(
    int Id,
    string Title,
    IReadOnlyList<QuizQuestionResponse> Questions
);

public sealed record QuizQuestionResponse(
    int Id,
    string Text,
    string Type,
    IReadOnlyList<QuizOptionResponse> Options,
    string? AnswerText,
    int? SelectedOptionId
);

public sealed record QuizOptionResponse(int Id, string Text);

public sealed record QuizResponsesResponse(
    int Id,
    string Title,
    IReadOnlyList<QuizQuestionResponsesResponse> Questions
);

public sealed record QuizQuestionResponsesResponse(
    int Id,
    string Text,
    IReadOnlyList<QuizStudentResponse> Responses
);

public sealed record QuizStudentResponse(
    int StudentId,
    string StudentIndex,
    string? AnswerText,
    int? SelectedOptionId,
    string? SelectedOptionText,
    DateTime SubmittedAt
);
