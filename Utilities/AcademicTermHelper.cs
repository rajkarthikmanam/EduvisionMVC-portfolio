using System;

namespace EduvisionMvc.Utilities;

public static class AcademicTermHelper
{
    // Returns a human-friendly academic term like "Spring 2025", "Summer 2025", or "Fall 2025"
    public static string GetCurrentTerm(DateTime now)
    {
        var year = now.Year;
        var month = now.Month;
        if (month >= 1 && month <= 4) return $"Spring {year}";
        if (month >= 5 && month <= 8) return $"Summer {year}";
        return $"Fall {year}"; // Sep-Dec
    }
}
