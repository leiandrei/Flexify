using System.Globalization;

namespace GymCrudApp.Helpers;

/// <summary>View helpers for the Flexify design (badge classes, initials, money format).</summary>
public static class FlexifyUi
{
    /// <summary>Maps a status word to its badge classes. The badge must still show the word itself.</summary>
    public static string StatusBadge(string? status) => (status ?? "").Replace(" ", "").ToLowerInvariant() switch
    {
        "active" or "paid"                                  => "fx-badge fx-badge--active",
        "due" or "duesoon" or "pending" or "expiring"       => "fx-badge fx-badge--due",
        "overdue" or "expired" or "unpaid" or "voided"      => "fx-badge fx-badge--overdue",
        _                                                   => "fx-badge fx-badge--neutral"
    };

    public static string Initials(string? name) =>
        string.Concat((name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
              .Take(2).Select(p => char.ToUpperInvariant(p[0])));

    public static string Peso(decimal amount) =>
        "₱" + amount.ToString("N0", CultureInfo.InvariantCulture);
}
