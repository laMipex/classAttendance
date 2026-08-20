using ClassAttendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Infrastructure.Services;

public sealed class AttendanceRemovalService(
    IServiceScopeFactory scopeFactory,
    ILogger<AttendanceRemovalService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DeleteExpiredAttendanceAsync(stoppingToken);
            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private async Task DeleteExpiredAttendanceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var now = DateTime.UtcNow;
        var expired = await dbContext.Attendances
            .Where(attendance => attendance.AttendanceSession.Lecture.EndsAt <= now)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        dbContext.Attendances.RemoveRange(expired);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Removed {Count} attendance confirmations for ended classes.", expired.Count);
    }
}

