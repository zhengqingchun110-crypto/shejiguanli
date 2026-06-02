using System.Diagnostics;
using System.IO;
using DecorationProjectScheduler.App.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DecorationProjectScheduler.App.Services;

public static class PdfExportService
{
    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        FontManager.RegisterFont(File.OpenRead(@"C:\Windows\Fonts\msyh.ttc"));
        FontManager.RegisterFont(File.OpenRead(@"C:\Windows\Fonts\msyhbd.ttc"));
    }

    public static string ExportDepartmentSchedule(string departmentName, IEnumerable<EmployeeWorkGroup> groups)
    {
        var rows = new List<string[]>();
        foreach (var group in groups)
        {
            if (group.ProjectRows.Count == 0)
            {
                rows.Add([departmentName, group.EmployeeName, "", "", "", ""]);
                continue;
            }

            rows.AddRange(group.ProjectRows.Select(row => new[]
            {
                departmentName,
                group.EmployeeName,
                row.ProjectName,
                row.CurrentTask,
                row.SubmissionDate.ToString("yyyy-MM-dd"),
                row.DaysUntilSubmissionText
            }));
        }

        return ExportTable(
            $"{departmentName}人员排期",
            "凡响智道项目管理",
            ["部门", "姓名", "项目", "当前工作", "提交日期", "距离提交"],
            rows,
            "人员排期导出",
            departmentName);
    }

    public static string ExportProjectList(string title, IEnumerable<ProjectSummary> projects, string folderName, string fileNamePrefix)
    {
        var rows = projects.Select(project => new[]
        {
            project.Name,
            project.ProjectType,
            project.OperatorNames,
            project.Status,
            project.CurrentStage,
            $"{project.StartDate:yyyy-MM-dd} 至 {project.EndDate:yyyy-MM-dd}",
            project.TaskCount.ToString()
        });

        return ExportTable(
            title,
            $"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm}",
            ["项目名称", "类型", "操作人员", "状态", "当前阶段", "周期", "任务数"],
            rows,
            folderName,
            fileNamePrefix);
    }

    public static string ExportProjectDetail(ProjectSummary project, string projectType, string operators, string summary, string taskPlan)
    {
        var exportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "项目导出");
        Directory.CreateDirectory(exportDirectory);

        var safeName = string.Join("_", project.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var filePath = Path.Combine(exportDirectory, $"{safeName}-项目详情-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(34);
                page.DefaultTextStyle(text => text.FontFamily("Microsoft YaHei").FontSize(10).FontColor("#27303F"));

                page.Header().Column(column =>
                {
                    column.Item().Text(project.Name).FontSize(24).SemiBold().FontColor("#141821");
                    column.Item().PaddingTop(6).Text($"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor("#667085");
                    column.Item().PaddingTop(12).LineHorizontal(1).LineColor("#DADFE8");
                });

                page.Content().PaddingTop(18).Column(column =>
                {
                    column.Spacing(14);
                    column.Item().Element(Card).Column(card =>
                    {
                        card.Item().Text("基础信息").FontSize(16).SemiBold();
                        card.Item().PaddingTop(8).Text($"项目类型：{TextOrEmpty(projectType)}");
                        card.Item().Text($"操作人员：{TextOrEmpty(operators)}");
                        card.Item().Text($"项目周期：{project.StartDate:yyyy-MM-dd} 至 {project.EndDate:yyyy-MM-dd}");
                        card.Item().Text($"当前阶段：{TextOrEmpty(project.CurrentStage)}");
                    });

                    column.Item().Element(Card).Column(card =>
                    {
                        card.Item().Text("项目详情").FontSize(16).SemiBold();
                        card.Item().PaddingTop(8).Text(TextOrEmpty(summary)).LineHeight(1.35f);
                    });

                    column.Item().Element(Card).Column(card =>
                    {
                        card.Item().Text("跟进计划").FontSize(16).SemiBold();
                        card.Item().PaddingTop(8).Text(TextOrEmpty(taskPlan)).LineHeight(1.35f);
                    });
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("第 ");
                    text.CurrentPageNumber();
                    text.Span(" 页 / 共 ");
                    text.TotalPages();
                    text.Span(" 页");
                });
            });
        }).GeneratePdf(filePath);

        return filePath;
    }

    public static void OpenFile(string filePath)
    {
        Process.Start(new ProcessStartInfo(filePath)
        {
            UseShellExecute = true
        });
    }

    private static string ExportTable(
        string title,
        string subtitle,
        IReadOnlyList<string> headers,
        IEnumerable<string[]> rows,
        string folderName,
        string fileNamePrefix)
    {
        var exportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), folderName);
        Directory.CreateDirectory(exportDirectory);

        var safeName = string.Join("_", fileNamePrefix.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var filePath = Path.Combine(exportDirectory, $"{safeName}-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        var tableRows = rows.ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(text => text.FontFamily("Microsoft YaHei").FontSize(9).FontColor("#27303F"));

                page.Header().Column(column =>
                {
                    column.Item().Text(title).FontSize(22).SemiBold().FontColor("#141821");
                    column.Item().PaddingTop(4).Text(subtitle).FontSize(9).FontColor("#667085");
                    column.Item().PaddingTop(12).LineHorizontal(1).LineColor("#DADFE8");
                });

                page.Content().PaddingTop(16).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in headers)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var text in headers)
                        {
                            header.Cell().Element(HeaderCell).Text(text).SemiBold();
                        }
                    });

                    foreach (var row in tableRows)
                    {
                        for (var i = 0; i < headers.Count; i++)
                        {
                            var value = i < row.Length ? row[i] : string.Empty;
                            table.Cell().Element(BodyCell).Text(value).FontSize(8.5f);
                        }
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("第 ");
                    text.CurrentPageNumber();
                    text.Span(" 页 / 共 ");
                    text.TotalPages();
                    text.Span(" 页");
                });
            });
        }).GeneratePdf(filePath);

        return filePath;
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background("#F0F3F8")
            .Border(0.6f)
            .BorderColor("#DADFE8")
            .PaddingVertical(8)
            .PaddingHorizontal(6);

    private static IContainer BodyCell(IContainer container) =>
        container
            .Border(0.6f)
            .BorderColor("#E5E9F0")
            .PaddingVertical(7)
            .PaddingHorizontal(6);

    private static IContainer Card(IContainer container) =>
        container
            .Background("#F7F8FA")
            .Border(0.6f)
            .BorderColor("#E5E9F0")
            .Padding(16);

    private static string TextOrEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? "空" : value.Trim();
}
