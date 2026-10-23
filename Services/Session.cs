using MarketPrenga.Models;

namespace MarketPrenga.Services;

public static class Session
{
    public static Perdorues? User { get; set; }
    public static bool IsAdmin => User?.Access.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
}
