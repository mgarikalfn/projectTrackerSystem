using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Application.Dto;
using projectTracker.Domain.ValueObjects;

namespace projectTracker.Application.Interfaces
{
    public interface IRiskCalculatorService
    {
        ProjectHealth Calculate(ProgressMetricsDto metrics);
    }
}
