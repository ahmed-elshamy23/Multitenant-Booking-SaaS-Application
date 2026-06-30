using System.Text.Json.Serialization;
using Booking_SaaS.Domain.Enums;

namespace Booking_SaaS.Domain.Results;

public class Error
{
    private Error(string code, string description, ErrorType errorType)
    {
        Code = code;
        Description = description;
        ErrorType = errorType;
    }

    public string Code { get; }
    public string Description { get; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ErrorType ErrorType { get; }

    public static class Auth
    {
        public static readonly Error InvalidCredentials =
            new("Auth.InvalidCredentials", "Invalid email or password", ErrorType.Validation);

        public static readonly Error EmailNotConfirmed =
            new("Auth.EmailNotConfirmed", "Email not confirmed", ErrorType.Validation);

        public static readonly Error InvalidToken =
            new("Auth.InvalidToken", "Invalid token", ErrorType.Validation);

        public static readonly Error InvalidUser =
            new("Auth.InvalidUser", "Invalid user", ErrorType.Validation);
    }

    public static class Validation
    {
        public static Error InvalidParameters(IEnumerable<string> errors)
        {
            return new Error("Validation.InvalidParameters", string.Join(", ", errors), ErrorType.Validation);
        }

        public static Error MismatchedIds(string name)
        {
            return new Error("Validation.MismatchedIds", $"{name} IDs must match", ErrorType.Validation);
        }

        public static Error NotFound(string name)
        {
            return new Error("Validation.NotFound", $"{name} not found", ErrorType.NotFound);
        }
    }

    public static class Tenant
    {
        public static readonly Error Inactive =
            new("Tenant.Inactive", "Tenant inactive", ErrorType.Validation);

        public static readonly Error DuplicateName =
            new("Tenant.DuplicateName", "Duplicate tenant name", ErrorType.Validation);
    }

    public static class Resource
    {
        public static readonly Error Duplicate =
            new("Resource.Duplicate", "Duplicate resource", ErrorType.Validation);

        public static readonly Error ExistingSchedules =
            new("Resource.ExistingSchedules", "Can't delete resource with existing schedules", ErrorType.Validation);
    }

    public static class Schedule
    {
        public static readonly Error Duplicate =
            new("Schedule.Duplicate", "Duplicate schedule", ErrorType.Validation);

        public static readonly Error PendingBookings =
            new("Schedule.PendingBookings", "Can't delete schedule with pending booking", ErrorType.Validation);

        public static readonly Error UpdateConflict =
            new("Schedule.UpdateConflict", "Can't update date or time for a schedule with pending booking",
                ErrorType.Validation);

        public static readonly Error OldSchedule =
            new("Schedule.OldSchedule", "Can't update or delete an old schedule", ErrorType.Validation);

        public static readonly Error UnmatchedCapacity =
            new("Schedule.UnmatchedCapacity", "Capacity can't be less than currently pending bookings",
                ErrorType.Validation);

        public static readonly Error ScheduleConversion =
            new("Schedule.ScheduleConversion",
                "Can't convert a recurrent schedule into non-recurrent or vice-versa",
                ErrorType.Validation);

        public static readonly Error MultipleSupportChange =
            new("Schedule.MultipleSupportChange",
                "Can't change the multiple support status of a schedule after creating it",
                ErrorType.Validation);

        public static readonly Error CantRegisterJobs =
            new("Schedule.CantRegisterJobs",
                "Can't register state management jobs",
                ErrorType.Validation);
    }

    public static class Booking
    {
        public static readonly Error PastDateNotAllowed =
            new("Booking.PastDateNotAllowed", "Can't book on a past date", ErrorType.Validation);

        public static readonly Error MissingDate =
            new("Booking.MissingDate", "Date must be specified for a recurrent schedule", ErrorType.Validation);

        public static readonly Error DayOfWeekMismatch =
            new("Booking.DayOfWeekMismatch", "Day of the week must match the recurrent schedule day",
                ErrorType.Validation);

        public static readonly Error FullyBooked =
            new("Booking.FullyBooked", "The schedule is fully booked", ErrorType.Validation);

        public static readonly Error DuplicateUserBooking =
            new("Booking.DuplicateUserBooking", "User already booked", ErrorType.Validation);

        public static readonly Error NonPending =
            new("Booking.NonPending", "Can't cancel a non-pending booking", ErrorType.Validation);

        public static readonly Error SameDayCancellation =
            new("Booking.SameDayCancellation", "Can't cancel a booking on its day", ErrorType.Validation);

        public static Error ExceededMaximum(int number)
        {
            return new Error("Booking.ExceededMaximum", $"Can't book more than {number} instance(s)",
                ErrorType.Validation);
        }
    }

    public static class Idempotency
    {
        public static readonly Error MissingHeader =
            new("Idempotency.MissingHeader", "Missing idempotency header", ErrorType.Validation);
    }
}