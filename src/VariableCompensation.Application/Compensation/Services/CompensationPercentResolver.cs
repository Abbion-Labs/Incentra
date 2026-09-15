using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Application.Compensation.Services;

public static class CompensationPercentResolver
{
    public static decimal Resolve(decimal overallAverage, EvaluatorSettings settings)
    {
        if (overallAverage < settings.ThresholdDoesNotMeet)
        {
            return settings.PercentDoesNotMeet;
        }

        if (overallAverage < settings.ThresholdMeets)
        {
            return settings.PercentMeets;
        }

        if (overallAverage < settings.ThresholdGood)
        {
            return settings.PercentGood;
        }

        if (overallAverage < settings.ThresholdExceeds)
        {
            return settings.PercentExceeds;
        }

        return settings.PercentExceeds;
    }
}
