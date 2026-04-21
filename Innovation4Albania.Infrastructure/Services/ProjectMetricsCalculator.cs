using Innovation4Albania.Domain.Entities;
using Innovation4Albania.Domain.Enums;

namespace Innovation4Albania.Infrastructure.Services;

internal static class ProjectMetricsCalculator
{
    public static InnovationProject WithDerivedMetrics(
        InnovationProject project,
        IEnumerable<ProjectTask> tasks,
        IEnumerable<WorkflowStep> workflowSteps,
        IEnumerable<ProjectMilestone> milestones)
    {
        var metrics = Calculate(project, tasks, workflowSteps, milestones);
        return new InnovationProject
        {
            Id = project.Id,
            Title = project.Title,
            MinistryId = project.MinistryId,
            Status = project.Status,
            StartDate = project.StartDate,
            DueDate = project.DueDate,
            Kpi = metrics.Kpi,
            OwnerName = project.OwnerName,
            Description = project.Description,
            ApprovalStage = project.ApprovalStage,
            RejectionReason = project.RejectionReason,
            CancellationReason = project.CancellationReason,
            Progress = metrics.Progress
        };
    }

    public static (int Progress, int Kpi) Calculate(
        InnovationProject project,
        IEnumerable<ProjectTask> tasks,
        IEnumerable<WorkflowStep> workflowSteps,
        IEnumerable<ProjectMilestone> milestones)
    {
        if (project.Status == ProjectStatus.Completed)
        {
            return (100, 100);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var taskList = tasks.ToList();
        var workflowList = workflowSteps.ToList();
        var milestoneList = milestones.ToList();

        var progressComponents = new List<(double Score, double Weight)>();
        if (taskList.Count > 0)
        {
            progressComponents.Add((CalculateTaskProgress(taskList, today), 0.45));
        }

        if (workflowList.Count > 0)
        {
            progressComponents.Add((workflowList.Average(step => Math.Clamp(step.Progress, 0, 100)), 0.35));
        }

        if (milestoneList.Count > 0)
        {
            progressComponents.Add((CalculateMilestoneProgress(milestoneList), 0.20));
        }

        var progress = progressComponents.Count == 0
            ? 0
            : (int)Math.Round(WeightedAverage(progressComponents));

        var kpiComponents = new List<(double Score, double Weight)>();
        if (progressComponents.Count > 0)
        {
            kpiComponents.Add((progress, 0.55));
        }

        if (taskList.Count > 0)
        {
            kpiComponents.Add((CalculateTaskDiscipline(taskList, today), 0.20));
        }

        kpiComponents.Add((CalculateScheduleAlignment(project, progress, today), 0.25));

        var kpi = progressComponents.Count == 0
            ? 0
            : (int)Math.Round(WeightedAverage(kpiComponents));

        return (Math.Clamp(progress, 0, 100), Math.Clamp(kpi, 0, 100));
    }

    private static double CalculateTaskProgress(IEnumerable<ProjectTask> tasks, DateOnly today) =>
        tasks.Average(task => NormalizeTaskStatus(task.Status) switch
        {
            "done" => 100,
            "in_progress" => task.Deadline.HasValue && task.Deadline.Value < today ? 45 : 60,
            "blocked" => task.Deadline.HasValue && task.Deadline.Value < today ? 5 : 20,
            _ => 0
        });

    private static double CalculateTaskDiscipline(IEnumerable<ProjectTask> tasks, DateOnly today) =>
        tasks.Average(task =>
        {
            var status = NormalizeTaskStatus(task.Status);
            var isOverdue = task.Deadline.HasValue && task.Deadline.Value < today && status != "done";
            if (status == "done")
            {
                return 100;
            }

            return status switch
            {
                "in_progress" => isOverdue ? 35 : 75,
                "blocked" => isOverdue ? 5 : 25,
                _ => isOverdue ? 15 : 55
            };
        });

    private static int CalculateMilestoneProgress(IReadOnlyList<ProjectMilestone> milestones)
    {
        var achieved = milestones.Count(item => item.AchievedAtUtc.HasValue);
        return milestones.Count == 0 ? 0 : (int)Math.Round((double)achieved / milestones.Count * 100);
    }

    private static int CalculateScheduleAlignment(InnovationProject project, int progress, DateOnly today)
    {
        var totalDays = Math.Max(1, project.DueDate.DayNumber - project.StartDate.DayNumber);
        var elapsedDays = Math.Clamp(today.DayNumber - project.StartDate.DayNumber, 0, totalDays);
        if (elapsedDays == 0)
        {
            return progress == 0 ? 100 : Math.Clamp(progress, 0, 100);
        }

        var expectedProgress = (int)Math.Round((double)elapsedDays / totalDays * 100);
        if (progress >= expectedProgress)
        {
            return 100;
        }

        var delay = expectedProgress - progress;
        return Math.Clamp(100 - (delay * 2), 0, 100);
    }

    private static double WeightedAverage(IEnumerable<(double Score, double Weight)> components)
    {
        var available = components.Where(item => item.Weight > 0).ToList();
        var totalWeight = available.Sum(item => item.Weight);
        return totalWeight <= 0
            ? 0
            : available.Sum(item => item.Score * item.Weight) / totalWeight;
    }

    private static string NormalizeTaskStatus(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "done" => "done",
            "in_progress" => "in_progress",
            "blocked" => "blocked",
            _ => "todo"
        };
}
