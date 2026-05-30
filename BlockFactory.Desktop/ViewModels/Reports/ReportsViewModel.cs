using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Diagnostics;
using System.IO;

namespace BlockFactory.Desktop.ViewModels.Reports
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly IReportService _reportService;

        public ReportsViewModel(IReportService reportService)
        {
            _reportService = reportService;
            ReportDate = DateTime.Today;
            FromDate = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month, 1);
            ToDate = DateTime.Today;
            SalaryReportMonth = DateTime.Today.Month;
            SalaryReportYear = DateTime.Today.Year;
            InitializeCommands();
        }

        // ─── Properties ─────────────────────────────
        private int _salaryReportMonth;
        public int SalaryReportMonth
        {
            get => _salaryReportMonth;
            set => SetProperty(ref _salaryReportMonth, value);
        }

        private int _salaryReportYear;
        public int SalaryReportYear
        {
            get => _salaryReportYear;
            set => SetProperty(ref _salaryReportYear, value);
        }

        private DateTime _reportDate;
        public DateTime ReportDate
        {
            get => _reportDate;
            set => SetProperty(ref _reportDate, value);
        }

        private DateTime _fromDate;
        public DateTime FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        private DateTime _toDate;
        public DateTime ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand PrintDailySalesCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SaveDailySalesCommand { get; private set; }
            = null!;
        public AsyncRelayCommand PrintCustomerDebtCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SaveCustomerDebtCommand { get; private set; }
            = null!;
        public AsyncRelayCommand PrintProductionCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SaveProductionCommand { get; private set; }
            = null!;
        public AsyncRelayCommand PrintInventoryCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SaveInventoryCommand { get; private set; }
            = null!;
        public AsyncRelayCommand PrintSalarySheetCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SaveSalarySheetCommand { get; private set; }
            = null!;

        // Months & Years lists for salary report
        public List<MonthItem> SalaryMonths { get; } = Enumerable
            .Range(1, 12)
            .Select(m => new MonthItem(
                new DateTime(2024, m, 1)
                    .ToString("MMMM",
                    new System.Globalization.CultureInfo("ar-SA")), m))
            .ToList();

        public List<int> SalaryYears { get; } = Enumerable
            .Range(DateTime.Today.Year - 2, 4)
            .ToList();

        private void InitializeCommands()
        {
            // مبيعات اليوم
            PrintDailySalesCommand = new AsyncRelayCommand(
                async _ =>
                {
                    await ExecuteReportAsync(
                        async () => await _reportService
                            .GenerateDailySalesPdfAsync(ReportDate),
                        print: true);
                });

            SaveDailySalesCommand = new AsyncRelayCommand(
                async _ =>
                {
                    await ExecuteReportAsync(
                        async () => await _reportService
                            .GenerateDailySalesPdfAsync(ReportDate),
                        print: false,
                        filename:
                            $"مبيعات_{ReportDate:yyyyMMdd}.pdf");
                });

            // ديون العملاء
            PrintCustomerDebtCommand = new AsyncRelayCommand(
                async _ =>
                {
                    await ExecuteReportAsync(
                        async () => await _reportService
                            .GenerateCustomerDebtPdfAsync(),
                        print: true);
                });

            SaveCustomerDebtCommand = new AsyncRelayCommand(
                async _ =>
                {
                    await ExecuteReportAsync(
                        async () => await _reportService
                            .GenerateCustomerDebtPdfAsync(),
                        print: false,
                        filename:
                            $"ديون_عملاء_{DateTime.Today:yyyyMMdd}.pdf");
                });

            // الإنتاج
            PrintProductionCommand = new AsyncRelayCommand(
                async _ =>
                {
                    await ExecuteReportAsync(
                        async () => await _reportService
                            .GenerateProductionPdfAsync(FromDate, ToDate),
                        print: true);
                });

            SaveProductionCommand = new AsyncRelayCommand(
                async _ =>
                {
                    await ExecuteReportAsync(
                        async () => await _reportService
                            .GenerateProductionPdfAsync(FromDate, ToDate),
                        print: false,
                        filename:
                            $"انتاج_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.pdf");
                });

            // المخزون
            PrintInventoryCommand = new AsyncRelayCommand(
                async _ =>
                {
                    await ExecuteReportAsync(
                        async () => await _reportService
                            .GenerateInventoryPdfAsync(),
                        print: true);
                });

            SaveInventoryCommand = new AsyncRelayCommand(
                async _ =>
                {
                    await ExecuteReportAsync(
                        async () => await _reportService
                            .GenerateInventoryPdfAsync(),
                        print: false,
                        filename:
                            $"مخزون_{DateTime.Today:yyyyMMdd}.pdf");
                });

            // كشف الرواتب
            PrintSalarySheetCommand = new AsyncRelayCommand(
                async _ =>
                {
                    await ExecuteReportAsync(
                        async () => await _reportService
                            .GenerateSalarySheetPdfAsync(
                                SalaryReportMonth,
                                SalaryReportYear),
                        print: true);
                });

            SaveSalarySheetCommand = new AsyncRelayCommand(
                async _ =>
                {
                    var monthName = SalaryMonths
                        .First(m => m.Value == SalaryReportMonth).Name;
                    await ExecuteReportAsync(
                        async () => await _reportService
                            .GenerateSalarySheetPdfAsync(
                                SalaryReportMonth,
                                SalaryReportYear),
                        print: false,
                        filename:
                            $"كشف_الرواتب_{monthName}_{SalaryReportYear}.pdf");
                });
        }

        // ─── Execute ─────────────────────────────────
        private async Task ExecuteReportAsync(
            Func<Task<byte[]>> generatePdf,
            bool print,
            string? filename = null)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري تهيئة التقرير...";

                var pdfBytes = await generatePdf();

                if (pdfBytes.Length == 0)
                {
                    ShowError("لا توجد بيانات لهذه الفترة");
                    return;
                }

                if (print)
                {
                    StatusMessage = "جاري الطباعة...";
                    await _reportService.PrintReportAsync(pdfBytes);
                    ShowSuccess(
                        "تم فتح ملف PDF. اطبع من القارئ (مثلاً Ctrl+P) إن أردت الطباعة على ورق.");
                }
                else
                {
                    // حفظ الملف
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = filename ?? "report.pdf",
                        DefaultExt = ".pdf",
                        Filter = "PDF Files|*.pdf"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        await File.WriteAllBytesAsync(
                            dialog.FileName, pdfBytes);

                        ShowSuccess($"تم حفظ التقرير: {dialog.FileName}");

                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = dialog.FileName,
                                UseShellExecute = true,
                                WorkingDirectory = Path.GetDirectoryName(
                                    dialog.FileName)
                                    ?? Environment.GetFolderPath(
                                        Environment.SpecialFolder.UserProfile)
                            });
                        }
                        catch (Exception openEx)
                        {
                            ShowError(
                                "تم الحفظ لكن تعذّر فتح الملف تلقائياً. " +
                                "افتح الملف من المكان الذي اخترته. " +
                                openEx.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
    }

    public record MonthItem(string Name, int Value);
}
