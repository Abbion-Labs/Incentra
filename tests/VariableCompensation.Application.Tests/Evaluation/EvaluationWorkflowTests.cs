using FluentAssertions;
using EvaluationWorkflowService = VariableCompensation.Application.Evaluation.Services.EvaluationWorkflow;
using EvaluationWorkflowAction = VariableCompensation.Application.Evaluation.Services.EvaluationWorkflowAction;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Builders;

namespace VariableCompensation.Application.Tests.EvaluationWorkflowTests;

[Trait("Category", "Unit")]
public class EvaluationWorkflowTests
{
    [Theory]
    [InlineData(EvaluationStatus.Draft, "Submit", true, EvaluationStatus.Submitted)]
    [InlineData(EvaluationStatus.Submitted, "StartReview", true, EvaluationStatus.UnderReview)]
    [InlineData(EvaluationStatus.UnderReview, "Approve", true, EvaluationStatus.Approved)]
    [InlineData(EvaluationStatus.Submitted, "ReturnForRevision", true, EvaluationStatus.Draft)]
    [InlineData(EvaluationStatus.UnderReview, "ReturnForRevision", true, EvaluationStatus.Draft)]
    [InlineData(EvaluationStatus.Approved, "Submit", false, EvaluationStatus.Approved)]
    [InlineData(EvaluationStatus.Draft, "Approve", false, EvaluationStatus.Draft)]
    public void TryGetNextStatus_ReturnsExpected(
        EvaluationStatus current,
        string actionName,
        bool expectedSuccess,
        EvaluationStatus expectedNextWhenSuccess)
    {
        var action = ParseAction(actionName);
        var success = EvaluationWorkflowService.TryGetNextStatus(current, action, out var next, out var error);

        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            next.Should().Be(expectedNextWhenSuccess);
            error.Should().BeNull();
        }
        else
        {
            error.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ApplyTransition_Submit_SetsSubmittedAtAndIncrementsVersion()
    {
        var evaluation = new EvaluationBuilder().WithVersion(1).Build();
        var userId = 42L;

        EvaluationWorkflowService.ApplyTransition(evaluation, EvaluationStatus.Draft, EvaluationStatus.Submitted, userId, null);

        evaluation.Status.Should().Be(EvaluationStatus.Submitted);
        evaluation.Version.Should().Be(2);
        evaluation.SubmittedAt.Should().NotBeNull();
        evaluation.StatusHistory.Should().ContainSingle(h =>
            h.FromStatus == nameof(EvaluationStatus.Draft) &&
            h.ToStatus == nameof(EvaluationStatus.Submitted) &&
            h.ChangedByUserId == userId);
    }

    [Fact]
    public void ApplyTransition_ReturnForRevision_ResetsWorkflowDatesAndStoresComment()
    {
        var evaluation = new EvaluationBuilder()
            .WithStatus(EvaluationStatus.UnderReview)
            .Build();
        evaluation.SubmittedAt = DateTime.UtcNow.AddDays(-2);
        evaluation.ReviewedAt = DateTime.UtcNow.AddDays(-1);
        evaluation.ApprovedAt = DateTime.UtcNow;

        EvaluationWorkflowService.ApplyTransition(
            evaluation,
            EvaluationStatus.UnderReview,
            EvaluationStatus.Draft,
            7,
            "Dopuniti merila");

        evaluation.Status.Should().Be(EvaluationStatus.Draft);
        evaluation.SubmittedAt.Should().BeNull();
        evaluation.ReviewedAt.Should().BeNull();
        evaluation.ApprovedAt.Should().BeNull();
        evaluation.ControllerComment.Should().Be("Dopuniti merila");
        evaluation.RejectionReason.Should().Be("Dopuniti merila");
    }

    [Fact]
    public void CanEdit_OnlyDraft_ReturnsTrue()
    {
        EvaluationWorkflowService.CanEdit(EvaluationStatus.Draft).Should().BeTrue();
        EvaluationWorkflowService.CanEdit(EvaluationStatus.Submitted).Should().BeFalse();
        EvaluationWorkflowService.CanEdit(EvaluationStatus.Approved).Should().BeFalse();
    }

    private static EvaluationWorkflowAction ParseAction(string actionName) =>
        actionName switch
        {
            "Submit" => EvaluationWorkflowAction.Submit,
            "StartReview" => EvaluationWorkflowAction.StartReview,
            "Approve" => EvaluationWorkflowAction.Approve,
            "ReturnForRevision" => EvaluationWorkflowAction.ReturnForRevision,
            _ => throw new ArgumentOutOfRangeException(nameof(actionName)),
        };
}
