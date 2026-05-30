using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

using BlockFactory.Core.DTOs.HR;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;

namespace BlockFactory.Desktop.ViewModels.HR
{
    public class SalariesViewModel : BaseViewModel
    {
        private readonly IHRService _hrService;
        private readonly IReportService _reportService;

        public SalariesViewModel(IHRService hrService, IReportService reportService)
        {
            _hrService = hrService;
            _reportService = reportService;
            SelectedMonth = DateTime.Today.Month;
            SelectedYear = DateTime.Today.Year;
            InitializeCommands();
        }

        public bool CanManageSalaries =>
            CurrentSession.Instance.HasPermission("ManageSalaries");

        // ─── Properties ─────────────────────────────
        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set => SetProperty(ref _selectedMonth, value);
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set => SetProperty(ref _selectedYear, value);
        }

        private MonthlySalarySheetDto? _salarySheet;
        public MonthlySalarySheetDto? SalarySheet
        {
            get => _salarySheet;
            set => SetProperty(ref _salarySheet, value);
        }

        public ObservableCollection<SalaryDto> Salaries { get; }
            = new();

        private SalaryDto? _selectedSalary;
        public SalaryDto? SelectedSalary
        {
            get => _selectedSalary;
            set => SetProperty(ref _selectedSalary, value);
        }

        // نموذج صرف الراتب
        private bool _isPayFormVisible;
        public bool IsPayFormVisible
        {
            get => _isPayFormVisible;
            set => SetProperty(ref _isPayFormVisible, value);
        }

        private decimal _payAmount;
        public decimal PayAmount
        {
            get => _payAmount;
            set => SetProperty(ref _payAmount, value);
        }

        private string? _payNotes;
        public string? PayNotes
        {
            get => _payNotes;
            set => SetProperty(ref _payNotes, value);
        }

        // قوائم الشهور والسنوات
        public List<MonthItem> Months { get; } = Enumerable
            .Range(1, 12)
            .Select(m => new MonthItem(
                new DateTime(2024, m, 1)
                    .ToString("MMMM",
                    new System.Globalization.CultureInfo("ar-SA")), m))
            .ToList();

        public List<int> Years { get; } = Enumerable
            .Range(DateTime.Today.Year - 2, 4)
            .ToList();

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand LoadCommand { get; private set; } = null!;
        public AsyncRelayCommand GenerateCommand { get; private set; }
            = null!;
        public RelayCommand ShowPayFormCommand { get; private set; } = null!;
        public AsyncRelayCommand SavePayCommand { get; private set; } = null!;
        public RelayCommand CancelPayCommand { get; private set; } = null!;
        public AsyncRelayCommand AddBonusCommand { get; private set; }
            = null!;
        public AsyncRelayCommand PrintSalarySheetCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SaveSalarySheetCommand { get; private set; }
            = null!;

        private void InitializeCommands()
        {
            LoadCommand = new AsyncRelayCommand(
                async _ => await LoadAsync());

            GenerateCommand = new AsyncRelayCommand(
                async _ => await GenerateAsync(),
                _ => CurrentSession.Instance.HasPermission("ManageSalaries"));

            ShowPayFormCommand = new RelayCommand(
                _ =>
                {
                    if (SelectedSalary == null) return;
                    PayAmount = SelectedSalary.RemainingAmount;
                    PayNotes = null;
                    IsPayFormVisible = true;
                    ClearMessages();
                },
                _ => SelectedSalary != null &&
                     SelectedSalary.Status != "مصروف" &&
                     CurrentSession.Instance.HasPermission("ManageSalaries"));

            SavePayCommand = new AsyncRelayCommand(
                async _ => await SavePayAsync(),
                _ => PayAmount > 0 &&
                     SelectedSalary != null &&
                     CurrentSession.Instance.HasPermission("ManageSalaries"));

            CancelPayCommand = new RelayCommand(_ =>
            {
                IsPayFormVisible = false;
                ClearMessages();
            });

            AddBonusCommand = new AsyncRelayCommand(
                async _ => await AddBonusAsync(),
                _ => SelectedSalary != null &&
                     SelectedSalary.Status != "مصروف" &&
                     CurrentSession.Instance.HasPermission("ManageSalaries"));

            PrintSalarySheetCommand = new AsyncRelayCommand(
                async _ => await ExecuteSalaryReportAsync(print: true));

            SaveSalarySheetCommand = new AsyncRelayCommand(
                async _ => await ExecuteSalaryReportAsync(print: false));
        }

        // ─── Load ────────────────────────────────────
        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;

                SalarySheet = await _hrService
                    .GetMonthlySalarySheetAsync(
                    SelectedMonth, SelectedYear);

                Salaries.Clear();
                foreach (var s in SalarySheet.Salaries)
                    Salaries.Add(s);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ─── توليد الرواتب ───────────────────────────
        private async Task GenerateAsync()
        {
            var confirm = MessageBox.Show(
                $"هل تريد إنشاء كشف رواتب " +
                $"{Months.First(m => m.Value == SelectedMonth).Name} " +
                $"{SelectedYear}؟\n" +
                "سيتم خصم جميع السلف المعلقة تلقائياً.",
                "تأكيد إنشاء الكشف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                var result = await _hrService.GenerateMonthlySalariesAsync(
                    new GenerateSalariesDto
                    {
                        Month = SelectedMonth,
                        Year = SelectedYear
                    });

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    await LoadAsync();
                }
                else ShowError(result.Message);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ─── صرف الراتب ─────────────────────────────
        private async Task SavePayAsync()
        {
            if (SelectedSalary == null) return;

            try
            {
                IsLoading = true;

                var result = await _hrService.PaySalaryAsync(
                    new PaySalaryDto
                    {
                        SalaryId = SelectedSalary.Id,
                        PayAmount = PayAmount,
                        Notes = PayNotes
                    });

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    IsPayFormVisible = false;
                    await LoadAsync();
                }
                else ShowError(result.Message);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddBonusAsync()
        {
            if (SelectedSalary == null) return;

            // InputDialog بسيط
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "أدخل مبلغ المكافأة (ر.ي):",
                "إضافة مكافأة",
                "0");

            if (!decimal.TryParse(input, out decimal amount) ||
                amount <= 0) return;

            var result = await _hrService.AddBonusAsync(
                SelectedSalary.Id, amount, "مكافأة يدوية");

            if (result.Success)
            {
                ShowSuccess(result.Message);
                await LoadAsync();
            }
            else ShowError(result.Message);
        }

        private async Task ExecuteSalaryReportAsync(bool print)
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                var pdfBytes = await _reportService.GenerateSalarySheetPdfAsync(
                    SelectedMonth, SelectedYear);

                if (pdfBytes.Length == 0)
                {
                    ShowError("لا توجد بيانات لهذا الشهر");
                    return;
                }

                if (print)
                {
                    await _reportService.PrintReportAsync(pdfBytes);
                    ShowSuccess(
                        "تم فتح ملف PDF. اطبع من القارئ (مثلاً Ctrl+P) إن أردت الطباعة على ورق.");
                }
                else
                {
                    var monthName = Months
                        .First(m => m.Value == SelectedMonth).Name;
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = $"كشف_الرواتب_{monthName}_{SelectedYear}.pdf",
                        DefaultExt = ".pdf",
                        Filter = "PDF Files|*.pdf"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        await File.WriteAllBytesAsync(dialog.FileName, pdfBytes);
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
                        catch { }
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
            }
        }
    }

    public record MonthItem(string Name, int Value);
}
