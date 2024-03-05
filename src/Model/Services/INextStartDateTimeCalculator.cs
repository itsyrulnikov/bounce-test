using Model.DTOs;
using Model.Entities;

namespace Model.Services;

public interface INextStartDateTimeCalculator
{
    DateTime CalculateNextStartedAt(
        DateTime? availableAt,
        TimeSpan duration);
}
