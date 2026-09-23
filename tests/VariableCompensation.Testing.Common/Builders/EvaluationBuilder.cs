using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Testing.Common.Builders;

public sealed class EvaluationBuilder
{
    private long id = 1;
    private long employeeId = 10;
    private long evaluatorEmployeeId = 2;
    private long? controllerEmployeeId = 3;
    private short year = 2026;
    private byte quarter = 1;
    private EvaluationStatus status = EvaluationStatus.Draft;
    private int version = 1;
    private readonly List<EvaluationGoal> goals = [];
    private readonly List<EvaluationMeasure> measures = [];
    private bool agreedPlan;
    private bool conditionsFulfilled = true;
    private string? evaluatorComment;
    private string? conditionsNotMetComment;

    public EvaluationBuilder WithConditionsFulfilled(bool value)
    {
        this.conditionsFulfilled = value;
        return this;
    }

    public EvaluationBuilder WithEvaluatorComment(string? value)
    {
        this.evaluatorComment = value;
        return this;
    }

    public EvaluationBuilder WithConditionsNotMetComment(string? value)
    {
        this.conditionsNotMetComment = value;
        return this;
    }

    public EvaluationBuilder WithId(long value)
    {
        this.id = value;
        return this;
    }

    public EvaluationBuilder WithEmployee(long value)
    {
        this.employeeId = value;
        return this;
    }

    public EvaluationBuilder WithEvaluator(long value)
    {
        this.evaluatorEmployeeId = value;
        return this;
    }

    public EvaluationBuilder WithController(long? value)
    {
        this.controllerEmployeeId = value;
        return this;
    }

    public EvaluationBuilder WithStatus(EvaluationStatus value)
    {
        this.status = value;
        return this;
    }

    public EvaluationBuilder WithVersion(int value)
    {
        this.version = value;
        return this;
    }

    public EvaluationBuilder WithYearQuarter(short y, byte q)
    {
        this.year = y;
        this.quarter = q;
        return this;
    }

    /// <summary>Adds a condition and a criterion, so that together with the goals the plan counts as set.</summary>
    public EvaluationBuilder WithAgreedPlan()
    {
        this.agreedPlan = true;
        return this;
    }

    public EvaluationBuilder AddGoal(long ratingLevelId, decimal? weight = null, string description = "Goal")
    {
        this.goals.Add(new EvaluationGoal
        {
            Id = this.goals.Count + 1,
            Description = description,
            RatingLevelId = ratingLevelId,
            Weight = weight,
            SortOrder = this.goals.Count + 1,
        });
        return this;
    }

    public EvaluationBuilder AddMeasure(long ratingLevelId, long measureTypeId = 1)
    {
        this.measures.Add(new EvaluationMeasure
        {
            Id = this.measures.Count + 1,
            MeasureTypeId = measureTypeId,
            RatingLevelId = ratingLevelId,
            SortOrder = this.measures.Count + 1,
        });
        return this;
    }

    public Evaluation Build()
    {
        var evaluation = new Evaluation
        {
            Id = this.id,
            EmployeeId = this.employeeId,
            EvaluatorEmployeeId = this.evaluatorEmployeeId,
            ControllerEmployeeId = this.controllerEmployeeId,
            Year = this.year,
            Quarter = this.quarter,
            Status = this.status,
            Version = this.version,
            ConditionsFulfilled = this.conditionsFulfilled,
            EvaluatorComment = this.evaluatorComment,
            ConditionsNotMetComment = this.conditionsNotMetComment,
        };

        foreach (var goal in this.goals)
        {
            goal.EvaluationId = evaluation.Id;
            evaluation.Goals.Add(goal);
        }

        foreach (var measure in this.measures)
        {
            measure.EvaluationId = evaluation.Id;
            evaluation.Measures.Add(measure);
        }

        if (this.agreedPlan)
        {
            evaluation.Conditions.Add(new EvaluationCondition { Id = 1, EvaluationId = evaluation.Id, Description = "Uslov", SortOrder = 1 });
            evaluation.Criteria.Add(new EvaluationCriterion { Id = 1, EvaluationId = evaluation.Id, Description = "Kriterijum", SortOrder = 1 });
        }

        return evaluation;
    }
}
