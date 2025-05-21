using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Application.Dto;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.ValueObjects;

namespace projectTracker.Infrastructure.Risk
{
    public class RiskCalculationService : IRiskCalculatorService
    {
        private readonly CurrentProjectRiskCalculator _calculator = new();

       

        public ProjectHealth Calculate(ProgressMetricsDto metrics)
        {
            return _calculator.Calculate(metrics);
        }
    }
}
