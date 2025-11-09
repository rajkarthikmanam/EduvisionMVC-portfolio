using EduvisionMvc.Models;

namespace EduvisionMvc.ViewModels;

/// <summary>
/// Represents a course the student can enroll in for the current term along with capacity info.
/// </summary>
public class CourseEnrollOption
{
    public Course Course { get; set; } = new();
    public int CurrentEnrollments { get; set; }
    public int RemainingSeats => Course.Capacity - CurrentEnrollments;
    public bool IsFull => RemainingSeats <= 0;
}