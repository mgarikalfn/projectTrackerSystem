// projectTracker.Infrastructure/Risk/IRiskCalculatorService.cs

using projectTracker.Application.Dto;
using projectTracker.Domain.ValueObjects;

namespace projectTracker.Application.Interfaces
{
    public interface IRiskCalculatorService
    {
        ProjectHealth Calculate(ProgressMetricsDto metrics);
    }
}