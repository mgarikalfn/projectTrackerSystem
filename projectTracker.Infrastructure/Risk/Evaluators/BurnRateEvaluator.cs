using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Infrastructure.Risk.Evaluators
{
    public class BurnRateEvaluator
    {
        public double Evaluate(decimal completed, decimal total)
        {
            if (total == 0) return 1.0;
            var ratio = completed / total;

            return (double)(ratio switch
            {
                >= 0.8m => 0.1,
                >= 0.5m => 0.4,
                >= 0.3m => 0.7,
                _ => 1.0
            });
        }
    }

}
