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
        var exportGroups = BuildPersonnelExportGroups([(departmentName, groups)]);
        return ExportPersonnelWorkTable(
            $"{departmentName}人员排期",
            "凡响智道项目管理",
            exportGroups,
            "人员排期导出",
            departmentName);
    }

    public static string ExportAllPersonnelWork(IEnumerable<DepartmentWorkPage> departmentPages)
    {
        var exportGroups = BuildPersonnelExportGroups(departmentPages.Select(page =>
            (page.DepartmentName, (IEnumerable<EmployeeWorkGroup>)page.EmployeeGroups)));

        return ExportPersonnelWorkTable(
            "所有人员工作",
            $"凡响智道项目管理 | 导出时间：{DateTime.Now:yyyy-MM-dd HH:mm}",
            exportGroups,
            "人员排期导出",
            "所有人员工作");
    }

    private static List<PersonnelExportGroup> BuildPersonnelExportGroups(
        IEnumerable<(string DepartmentName, IEnumerable<EmployeeWorkGroup> Groups)> departments)
    {
        return departments
            .SelectMany(department => department.Groups.Select(group => new PersonnelExportGroup(
                string.IsNullOrWhiteSpace(department.DepartmentName) ? "未分部门" : department.DepartmentName.Trim(),
                TextOrEmpty(group.EmployeeName),
                group.ProjectRows.Count == 0
                    ? [new PersonnelExportRow("", "", "", "")]
                    : group.ProjectRows
                        .Select(row => new PersonnelExportRow(
                            row.ProjectName,
                            row.CurrentTask,
                            row.SubmissionDate.ToString("yyyy-MM-dd"),
                            row.DaysUntilSubmissionText))
                        .ToList())))
            .ToList();
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

    public static string ExportProjectDetail(
        ProjectSummary project,
        string projectType,
        string operators,
        string summary,
        string taskPlan,
        IEnumerable<ActiveProjectWorkItem> workItems)
    {
        var exportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "项目导出");
        Directory.CreateDirectory(exportDirectory);

        var safeName = string.Join("_", project.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var filePath = Path.Combine(exportDirectory, $"{safeName}-项目详情-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        var workItemList = workItems.ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigureProjectDetailPage(page);

                page.Header().Element(ProjectHeader).Column(header =>
                {
                    header.Item().Text(project.Name).FontSize(18).SemiBold().FontColor("#141821");
                    header.Item().PaddingTop(6).Text($"项目详情 | 导出时间：{DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(7.4f)
                        .FontColor("#667085");
                });

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(9);

                    column.Item().Element(HeroCard).Column(hero =>
                    {
                        hero.Spacing(5);
                        hero.Item().Text("项目基础信息").FontSize(10).SemiBold().FontColor("#141821");
                        hero.Item().Text($"项目类型：{TextOrEmpty(projectType)}").FontSize(8.2f);
                        hero.Item().Text($"操作人员：{TextOrEmpty(operators)}").FontSize(8.2f);
                    });

                    column.Item().Element(SectionCard).Column(section =>
                    {
                        section.Spacing(5);
                        section.Item().Text("项目详情").FontSize(11).SemiBold().FontColor("#141821");
                        section.Item().Text(TextOrEmpty(summary)).FontSize(8.2f).LineHeight(1.32f);
                    });

                    column.Item().Element(SectionCard).Column(section =>
                    {
                        section.Spacing(5);
                        section.Item().Text("跟进计划").FontSize(11).SemiBold().FontColor("#141821");
                        section.Item().Text(TextOrEmpty(taskPlan)).FontSize(8.2f).LineHeight(1.32f);
                    });

                    column.Item().Column(section =>
                    {
                        section.Spacing(7);
                        section.Item().Text("正在进行的工作").FontSize(11).SemiBold().FontColor("#141821");

                        if (workItemList.Count == 0)
                        {
                            section.Item().Element(WorkCard).Text("空").FontSize(8).SemiBold();
                            return;
                        }

                        foreach (var work in workItemList)
                        {
                            section.Item().Element(WorkCard).Column(workCard =>
                            {
                                workCard.Spacing(5);
                                workCard.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(left =>
                                    {
                                        left.Spacing(3);
                                        left.Item().Text($"{TextOrEmpty(work.EmployeeName)}  |  {TextOrEmpty(work.DepartmentName)}")
                                            .FontSize(8.6f)
                                            .SemiBold()
                                            .FontColor("#141821");
                                        left.Item().Text(TextOrEmpty(work.TaskName)).FontSize(8.2f).LineHeight(1.28f);
                                    });

                                    row.ConstantItem(108).AlignRight().Column(right =>
                                    {
                                        right.Spacing(3);
                                        right.Item().AlignRight().Text($"提交：{work.SubmissionDate:yyyy-MM-dd}")
                                            .FontSize(7.5f)
                                            .SemiBold()
                                            .FontColor("#344054");
                                        right.Item().AlignRight().Text(TextOrEmpty(work.DaysUntilSubmission))
                                            .FontSize(7.5f)
                                            .FontColor("#667085");
                                    });
                                });
                            });
                        }
                    });
                });

                BuildPageFooter(page);
            });
        }).GeneratePdf(filePath);

        return filePath;
    }

    public static string ExportActiveProjectDetails(IEnumerable<ActiveProjectExportItem> projects)
    {
        var exportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "项目导出");
        Directory.CreateDirectory(exportDirectory);

        var filePath = Path.Combine(exportDirectory, $"进行中项目详情-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        var projectItems = projects.ToList();

        Document.Create(container =>
        {
            if (projectItems.Count == 0)
            {
                container.Page(page =>
                {
                    ConfigureProjectDetailPage(page);
                    page.Header().Element(ProjectHeader).Column(header =>
                    {
                        header.Item().Text("当前正在进行的项目").FontSize(18).SemiBold().FontColor("#141821");
                        header.Item().PaddingTop(5).Text($"凡响智道项目管理 | 导出时间：{DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(7.4f)
                            .FontColor("#667085");
                    });
                    page.Content().PaddingTop(10).Element(SectionCard).Text("当前没有正在进行的项目。").FontSize(10).SemiBold();
                    BuildPageFooter(page);
                });
                return;
            }

            foreach (var item in projectItems)
            {
                var project = item.Project;
                container.Page(page =>
                {
                    ConfigureProjectDetailPage(page);

                    page.Header().Element(ProjectHeader).Column(header =>
                    {
                        header.Item().Text(project.Name).FontSize(18).SemiBold().FontColor("#141821");
                        header.Item().PaddingTop(6).Text($"进行中项目详情 | 导出时间：{DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(7.4f)
                            .FontColor("#667085");
                    });

                    page.Content().PaddingTop(10).Column(column =>
                    {
                        column.Spacing(9);

                        column.Item().Element(HeroCard).Column(hero =>
                        {
                            hero.Spacing(5);
                            hero.Item().Text("项目基础信息").FontSize(10).SemiBold().FontColor("#141821");
                            hero.Item().Text($"项目类型：{TextOrEmpty(project.ProjectType)}").FontSize(8.2f);
                            hero.Item().Text($"操作人员：{TextOrEmpty(project.OperatorNames)}").FontSize(8.2f);
                        });

                        column.Item().Element(SectionCard).Column(section =>
                        {
                            section.Spacing(5);
                            section.Item().Text("项目详情").FontSize(11).SemiBold().FontColor("#141821");
                            section.Item().Text(TextOrEmpty(item.ProjectDetail)).FontSize(8.2f).LineHeight(1.32f);
                        });

                        column.Item().Element(SectionCard).Column(section =>
                        {
                            section.Spacing(5);
                            section.Item().Text("跟进计划").FontSize(11).SemiBold().FontColor("#141821");
                            section.Item().Text(TextOrEmpty(item.TaskPlan)).FontSize(8.2f).LineHeight(1.32f);
                        });

                        column.Item().Column(section =>
                        {
                            section.Spacing(7);
                            section.Item().Text("正在进行的工作").FontSize(11).SemiBold().FontColor("#141821");

                            if (item.WorkItems.Count == 0)
                            {
                                section.Item().Element(WorkCard).Text("空").FontSize(8).SemiBold();
                                return;
                            }

                            foreach (var work in item.WorkItems)
                            {
                                section.Item().Element(WorkCard).Column(workCard =>
                                {
                                    workCard.Spacing(5);
                                    workCard.Item().Row(row =>
                                    {
                                        row.RelativeItem().Column(left =>
                                        {
                                            left.Spacing(3);
                                            left.Item().Text($"{TextOrEmpty(work.EmployeeName)}  |  {TextOrEmpty(work.DepartmentName)}")
                                                .FontSize(8.6f)
                                                .SemiBold()
                                                .FontColor("#141821");
                                            left.Item().Text(TextOrEmpty(work.TaskName)).FontSize(8.2f).LineHeight(1.28f);
                                        });

                                        row.ConstantItem(108).AlignRight().Column(right =>
                                        {
                                            right.Spacing(3);
                                            right.Item().AlignRight().Text($"提交：{work.SubmissionDate:yyyy-MM-dd}")
                                                .FontSize(7.5f)
                                                .SemiBold()
                                                .FontColor("#344054");
                                            right.Item().AlignRight().Text(TextOrEmpty(work.DaysUntilSubmission))
                                                .FontSize(7.5f)
                                                .FontColor("#667085");
                                        });
                                    });
                                });
                            }
                        });

                    });

                    BuildPageFooter(page);
                });
            }
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

    private static string ExportPersonnelWorkTable(
        string title,
        string subtitle,
        IReadOnlyList<PersonnelExportGroup> groups,
        string folderName,
        string fileNamePrefix)
    {
        var exportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), folderName);
        Directory.CreateDirectory(exportDirectory);

        var safeName = string.Join("_", fileNamePrefix.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var filePath = Path.Combine(exportDirectory, $"{safeName}-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(text => text.FontFamily("Microsoft YaHei").FontSize(7.6f).FontColor("#27303F"));

                page.Header().Column(column =>
                {
                    column.Item().Text(title).FontSize(17).SemiBold().FontColor("#141821");
                    column.Item().PaddingTop(3).Text(subtitle).FontSize(7.4f).FontColor("#667085");
                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor("#DADFE8");
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(72);
                        columns.RelativeColumn(1.25f);
                        columns.RelativeColumn(2.25f);
                        columns.ConstantColumn(76);
                        columns.ConstantColumn(72);
                    });

                    table.Header(header =>
                    {
                        foreach (var text in new[] { "部门", "姓名", "项目", "当前工作", "提交日期", "距离提交" })
                        {
                            header.Cell().Element(HeaderCell).Text(text).FontSize(7.8f).SemiBold();
                        }
                    });

                    foreach (var group in groups)
                    {
                        var rowCount = Math.Max(1, group.Rows.Count);
                        table.Cell().RowSpan((uint)rowCount).Element(BodyCell).AlignMiddle().Text(group.DepartmentName).FontSize(7.3f).SemiBold();
                        table.Cell().RowSpan((uint)rowCount).Element(BodyCell).AlignMiddle().Text(group.EmployeeName).FontSize(7.5f).SemiBold();

                        foreach (var row in group.Rows)
                        {
                            table.Cell().Element(BodyCell).Text(TextOrEmpty(row.ProjectName)).FontSize(7.3f);
                            table.Cell().Element(BodyCell).Text(TextOrEmpty(row.CurrentTask)).FontSize(7.3f).LineHeight(1.25f);
                            table.Cell().Element(BodyCell).AlignCenter().Text(TextOrEmpty(row.SubmissionDate)).FontSize(7.2f);
                            table.Cell().Element(BodyCell).AlignCenter().Text(TextOrEmpty(row.DaysUntilSubmission)).FontSize(7.2f);
                        }
                    }
                });

                BuildPageFooter(page);
            });
        }).GeneratePdf(filePath);

        return filePath;
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
                page.Margin(20);
                page.DefaultTextStyle(text => text.FontFamily("Microsoft YaHei").FontSize(7.8f).FontColor("#27303F"));

                page.Header().Column(column =>
                {
                    column.Item().Text(title).FontSize(17).SemiBold().FontColor("#141821");
                    column.Item().PaddingTop(3).Text(subtitle).FontSize(7.4f).FontColor("#667085");
                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor("#DADFE8");
                });

                page.Content().PaddingTop(10).Table(table =>
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
                            header.Cell().Element(HeaderCell).Text(text).FontSize(7.8f).SemiBold();
                        }
                    });

                    foreach (var row in tableRows)
                    {
                        for (var i = 0; i < headers.Count; i++)
                        {
                            var value = i < row.Length ? row[i] : string.Empty;
                            table.Cell().Element(BodyCell).Text(value).FontSize(7.2f).LineHeight(1.25f);
                        }
                    }
                });

                BuildPageFooter(page);
            });
        }).GeneratePdf(filePath);

        return filePath;
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background("#F0F3F8")
            .Border(0.6f)
            .BorderColor("#DADFE8")
            .PaddingVertical(5)
            .PaddingHorizontal(4);

    private static IContainer BodyCell(IContainer container) =>
        container
            .Border(0.6f)
            .BorderColor("#E5E9F0")
            .PaddingVertical(4)
            .PaddingHorizontal(4);

    private static void ConfigureProjectDetailPage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(26);
        page.DefaultTextStyle(text => text.FontFamily("Microsoft YaHei").FontSize(8.8f).FontColor("#27303F"));
    }

    private static void BuildPageFooter(PageDescriptor page)
    {
        page.Footer().AlignRight().Text(text =>
        {
            text.Span("第 ");
            text.CurrentPageNumber();
            text.Span(" 页 / 共 ");
            text.TotalPages();
            text.Span(" 页");
        });
    }

    private static IContainer ProjectHeader(IContainer container) =>
        container
            .PaddingBottom(12)
            .BorderBottom(1)
            .BorderColor("#DADFE8");

    private static IContainer HeroCard(IContainer container) =>
        container
            .Background("#EEF5FF")
            .Border(0.8f)
            .BorderColor("#C7D7F2")
            .Padding(12);

    private static IContainer SectionCard(IContainer container) =>
        container
            .Background("#FFFFFF")
            .Border(0.8f)
            .BorderColor("#E5E9F0")
            .Padding(12);

    private static IContainer WorkCard(IContainer container) =>
        container
            .Background("#F8FAFC")
            .Border(0.8f)
            .BorderColor("#E5E9F0")
            .Padding(10);

    private static IContainer Card(IContainer container) =>
        container
            .Background("#F7F8FA")
            .Border(0.6f)
            .BorderColor("#E5E9F0")
            .Padding(12);

    private static string TextOrEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? "空" : value.Trim();

    private sealed record PersonnelExportGroup(string DepartmentName, string EmployeeName, List<PersonnelExportRow> Rows);

    private sealed record PersonnelExportRow(string ProjectName, string CurrentTask, string SubmissionDate, string DaysUntilSubmission);
}
