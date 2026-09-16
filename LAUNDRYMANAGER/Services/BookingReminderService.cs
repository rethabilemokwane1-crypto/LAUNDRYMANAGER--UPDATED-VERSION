using LaundryManager.Data;
using LaundryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryManager.Services
{
    public class BookingReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BookingReminderService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var push = scope.ServiceProvider.GetRequiredService<PushNotificationService>();
                    var now = DateTime.Now;

                    // Activate bookings at their start time and create the 10-minute arrival window.
                    var due = await db.Bookings
                        .Where(b => b.SlotStart <= now && b.SlotEnd > now && !b.IsNoShow)
                        .ToListAsync(stoppingToken);

                    foreach (var booking in due)
                    {
                        var machine = await db.Machines.FirstOrDefaultAsync(m => m.Id == booking.MachineId, stoppingToken);
                        if (machine == null || machine.Status != MachineStatus.Working) continue;

                        if (!machine.IsBooked)
                        {
                            machine.IsBooked = true;
                            machine.BookedBy = booking.StudentEmail;
                            machine.BookedAt = booking.SlotStart;
                            machine.IsConfirmed = booking.ConfirmedAt.HasValue;
                            machine.ConfirmationDeadline = booking.ConfirmedAt.HasValue ? null : booking.SlotStart.AddMinutes(10);
                        }

                        // Reminder when the slot begins.
                        if (booking.SlotStart <= now && booking.SlotStart > now.AddMinutes(-1) && !booking.ConfirmedAt.HasValue)
                        {
                            try
                            {
                                await push.SendNotificationAsync(
                                    booking.StudentEmail,
                                    "Your laundry slot has started",
                                    $"{machine.Name} is reserved for you. Confirm your arrival within 10 minutes.",
                                    "/Home/Index");
                            }
                            catch { }
                        }
                    }

                    // Release no-shows after 10 minutes.
                    var expiredMachines = await db.Machines
                        .Where(m => m.IsBooked && !m.IsConfirmed &&
                                    m.ConfirmationDeadline != null &&
                                    m.ConfirmationDeadline <= now)
                        .ToListAsync(stoppingToken);

                    foreach (var machine in expiredMachines)
                    {
                        var booking = await db.Bookings.FirstOrDefaultAsync(b =>
                            b.MachineId == machine.Id &&
                            b.StudentEmail == machine.BookedBy &&
                            b.SlotStart <= now && b.SlotEnd > now, stoppingToken);

                        if (booking != null)
                        {
                            booking.IsNoShow = true;
                            db.Bookings.Remove(booking);
                            try
                            {
                                await push.SendNotificationAsync(
                                    booking.StudentEmail,
                                    "Reservation released",
                                    $"{machine.Name} was released because arrival was not confirmed within 10 minutes.",
                                    "/Home/Index");
                            }
                            catch { }
                        }

                        machine.IsBooked = false;
                        machine.BookedBy = null;
                        machine.BookedAt = null;
                        machine.IsConfirmed = true;
                        machine.ConfirmationDeadline = null;
                    }

                    var finished = await db.Bookings.Where(b => b.SlotEnd <= now).ToListAsync(stoppingToken);
                    db.Bookings.RemoveRange(finished);

                    // Release completed immediate-use cycles and completed scheduled cycles.
                    var activeScheduledIds = await db.Bookings
                        .Where(b => b.SlotStart <= now && b.SlotEnd > now)
                        .Select(b => b.MachineId)
                        .ToListAsync(stoppingToken);
                    var activeSet = activeScheduledIds.ToHashSet();

                    var bookedMachines = await db.Machines
                        .Where(m => m.IsBooked && m.BookedAt != null)
                        .ToListAsync(stoppingToken);

                    var completedMachines = bookedMachines
                        .Where(m => !activeSet.Contains(m.Id) &&
                                    m.BookedAt!.Value.AddMinutes(Machine.GetAverageDurationMinutes(m.Type)) <= now)
                        .ToList();

                    foreach (var machine in completedMachines)
                    {
                        machine.IsBooked = false;
                        machine.BookedBy = null;
                        machine.BookedAt = null;
                        machine.IsConfirmed = true;
                        machine.ConfirmationDeadline = null;
                    }

                    if (due.Any() || expiredMachines.Any() || finished.Any() || completedMachines.Any())
                        await db.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                catch
                {
                    // A background notification failure must not stop the booking monitor.
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
