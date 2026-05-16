using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.DTOs.Reports;
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Sales;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BlockFactory.Core.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _uow;

        // اسم المصنع للطباعة
        private const string FactoryName = "مصنع البلوك";
        private const string Currency = "ر.ي";

        public ReportService(IUnitOfWork uow)
        {
            _uow = uow;
            // ترخيص QuestPDF المجاني
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ═══════════════════════════════════════════
        // جمع البيانات
        // ═══════════════════════════════════════════

        public async Task<InvoiceReportDto?> GetInvoiceDataAsync(int orderId)
        {
            var order = await _uow.Orders.GetOrderWithDetailsAsync(orderId);
            if (order == null) return null;

            return new InvoiceReportDto
            {
                InvoiceNumber = order.Invoice?.InvoiceNumber
                    ?? $"INV-{orderId}",
                OrderNumber = order.OrderNumber,
                InvoiceDate = order.Invoice?.InvoiceDate ?? DateTime.Now,
                CustomerName = order.Customer?.FullName ?? "-",
                CustomerPhone = order.Customer?.Phone,
                CustomerAddress = order.Customer?.Address,
                PaymentType = GetPaymentTypeAr(order.PaymentType),
                DeliveryType = order.DeliveryType == DeliveryType.Delivery
                    ? "توصيل" : "استلام",
                DeliveryCost = order.DeliveryCost,
                SubTotal = order.SubTotal,
                Discount = order.Discount,
                TotalAmount = order.TotalAmount,
                PaidAmount = order.PaidAmount,
                RemainingAmount = order.RemainingAmount,
                PaymentStatus = GetPaymentStatusAr(order.PaymentStatus),
                DueDate = order.DueDate,
                Notes = order.Notes,
                Items = order.Items.Select(i => new InvoiceItemDto
                {
                    ProductName = i.Product?.Name ?? "-",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };
        }

        public async Task<DailySalesReportDto> GetDailySalesAsync(
            DateTime date)
        {
            var orders = await _uow.Orders.Query()
                .Include(o => o.Customer)
                .Where(o => o.OrderDate.Date == date.Date &&
                            !o.IsDeleted)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            return new DailySalesReportDto
            {
                ReportDate = date,
                TotalOrders = orders.Count,
                TotalSales = orders.Sum(o => o.TotalAmount),
                TotalCollected = orders.Sum(o => o.PaidAmount),
                TotalRemaining = orders.Sum(o => o.RemainingAmount),
                CashOrders = orders.Count(
                    o => o.PaymentType == PaymentType.Cash),
                CreditOrders = orders.Count(
                    o => o.PaymentType == PaymentType.Credit),
                PledgeOrders = orders.Count(
                    o => o.PaymentType == PaymentType.Pledge),
                Orders = orders.Select(o => new SalesOrderRowDto
                {
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.Customer?.FullName ?? "-",
                    TotalAmount = o.TotalAmount,
                    PaidAmount = o.PaidAmount,
                    RemainingAmount = o.RemainingAmount,
                    PaymentType = GetPaymentTypeAr(o.PaymentType),
                    Status = GetPaymentStatusAr(o.PaymentStatus)
                }).ToList()
            };
        }

        public async Task<CustomerDebtReportDto> GetCustomerDebtReportAsync()
        {
            var customers = await _uow.Customers.Query()
                .Include(c => c.Orders)
                .Where(c => c.TotalDebt > 0 && !c.IsDeleted)
                .OrderByDescending(c => c.TotalDebt)
                .ToListAsync();

            return new CustomerDebtReportDto
            {
                ReportDate = DateTime.Today,
                TotalDebt = customers.Sum(c => c.TotalDebt),
                CustomersCount = customers.Count,
                Rows = customers.Select(c => new CustomerDebtRowDto
                {
                    CustomerName = c.FullName,
                    Phone = c.Phone,
                    TotalDebt = c.TotalDebt,
                    UnpaidOrders = c.Orders.Count(
                        o => o.PaymentStatus != PaymentStatus.FullyPaid),
                    LastOrderDate = c.Orders
                        .OrderByDescending(o => o.OrderDate)
                        .FirstOrDefault()?.OrderDate
                }).ToList()
            };
        }

        public async Task<ProductionReportDto> GetProductionReportAsync(
            DateTime from, DateTime to)
        {
            var records = await _uow.Productions.Query()
                .Include(p => p.Product)
                .Where(p =>
                    p.ProductionDate.Date >= from.Date &&
                    p.ProductionDate.Date <= to.Date &&
                    !p.IsDeleted)
                .OrderBy(p => p.ProductionDate)
                .ThenBy(p => p.Product.Name)
                .ToListAsync();

            return new ProductionReportDto
            {
                FromDate = from,
                ToDate = to,
                TotalProduced = records.Sum(r => r.QuantityProduced),
                TotalDefective = records.Sum(r => r.QuantityDefective),
                TotalNet = records.Sum(r => r.QuantityNet),
                Rows = records.Select(r => new ProductionRowDto
                {
                    Date = r.ProductionDate,
                    ProductName = r.Product?.Name ?? "-",
                    Shift = r.Shift ==
                        Models.Production.ProductionShift.Morning
                        ? "صباحي" : "مسائي",
                    Produced = r.QuantityProduced,
                    Defective = r.QuantityDefective,
                    Net = r.QuantityNet
                }).ToList()
            };
        }

        public async Task<InventoryReportDto> GetInventoryReportAsync()
        {
            var stocks = await _uow.Inventory.Query()
                .Include(s => s.Product)
                    .ThenInclude(p => p.ProductType)
                .OrderBy(s => s.Product.ProductTypeId)
                .ThenBy(s => s.Product.Size)
                .ToListAsync();

            return new InventoryReportDto
            {
                ReportDate = DateTime.Today,
                TotalProducts = stocks.Count,
                TotalBlocks = stocks.Sum(s => s.QuantityAvailable),
                LowStockCount = stocks.Count(
                    s => s.QuantityAvailable <= s.MinimumThreshold),
                Rows = stocks.Select(s => new InventoryRowDto
                {
                    ProductType = s.Product?.ProductType?.Name ?? "-",
                    ProductName = s.Product?.Name ?? "-",
                    QuantityAvailable = s.QuantityAvailable,
                    MinThreshold = s.MinimumThreshold,
                    Status = s.QuantityAvailable <= 0
                        ? "نفذ"
                        : s.QuantityAvailable <= s.MinimumThreshold
                            ? "منخفض"
                            : "جيد"
                }).ToList()
            };
        }

        // ═══════════════════════════════════════════
        // توليد PDF
        // ═══════════════════════════════════════════

        public async Task<byte[]> GenerateInvoicePdfAsync(int orderId)
        {
            var data = await GetInvoiceDataAsync(orderId);
            if (data == null)
                return Array.Empty<byte>();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(15, Unit.Millimetre);
                    page.DefaultTextStyle(x =>
                        x.FontFamily("Arial")
                         .FontSize(9));

                    page.Content().Column(col =>
                    {
                        // ─── رأس الفاتورة ────────────
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item()
                                    .Text(FactoryName)
                                    .Bold()
                                    .FontSize(16)
                                    .FontColor(Color.FromHex("#1E3A5F"));

                                c.Item()
                                    .Text($"فاتورة رقم: {data.InvoiceNumber}")
                                    .FontSize(10)
                                    .FontColor(Color.FromHex("#E67E22"));
                            });

                            row.ConstantItem(100).Column(c =>
                            {
                                c.Item()
                                    .AlignLeft()
                                    .Text("فاتورة بيع")
                                    .Bold()
                                    .FontSize(14)
                                    .FontColor(Color.FromHex("#1E3A5F"));

                                c.Item()
                                    .AlignLeft()
                                    .Text(data.InvoiceDate
                                        .ToString("dd/MM/yyyy"))
                                    .FontSize(9);
                            });
                        });

                        col.Item().PaddingVertical(5)
                            .LineHorizontal(1)
                            .LineColor(Color.FromHex("#1E3A5F"));

                        // ─── بيانات العميل ────────────
                        col.Item().Padding(5).Background(
                            Color.FromHex("#F8F9FA")).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem()
                                    .Text($"العميل: {data.CustomerName}")
                                    .Bold();
                                r.RelativeItem()
                                    .AlignLeft()
                                    .Text($"الهاتف: {data.CustomerPhone ?? "-"}");
                            });

                            c.Item().Row(r =>
                            {
                                r.RelativeItem()
                                    .Text($"طريقة الدفع: {data.PaymentType}");
                                r.RelativeItem()
                                    .AlignLeft()
                                    .Text($"التوصيل: {data.DeliveryType}");
                            });

                            if (data.DueDate.HasValue)
                            {
                                c.Item().Text(
                                    $"تاريخ الاستحقاق: " +
                                    $"{data.DueDate.Value:dd/MM/yyyy}")
                                    .FontColor(Color.FromHex("#E74C3C"));
                            }
                        });

                        col.Item().PaddingVertical(5);

                        // ─── جدول المنتجات ────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(4);  // المنتج
                                cols.RelativeColumn(1.5f); // الكمية
                                cols.RelativeColumn(2);  // السعر
                                cols.RelativeColumn(2);  // الإجمالي
                            });

                            // رأس الجدول
                            table.Header(header =>
                            {
                                void HeaderCell(string text)
                                {
                                    header.Cell()
                                        .Background(
                                            Color.FromHex("#1E3A5F"))
                                        .Padding(4)
                                        .AlignCenter()
                                        .Text(text)
                                        .FontColor(Colors.White)
                                        .Bold()
                                        .FontSize(9);
                                }

                                HeaderCell("المنتج");
                                HeaderCell("الكمية");
                                HeaderCell("السعر");
                                HeaderCell("الإجمالي");
                            });

                            // صفوف المنتجات
                            bool isEven = false;
                            foreach (var item in data.Items)
                            {
                                var bg = isEven
                                    ? Color.FromHex("#F8F9FA")
                                    : Colors.White;
                                isEven = !isEven;

                                void DataCell(string text,
                                    bool isCenter = false)
                                {
                                    var cell = table.Cell()
                                        .Background(bg)
                                        .Padding(4);

                                    if (isCenter)
                                        cell.AlignCenter()
                                            .Text(text).FontSize(9);
                                    else
                                        cell.Text(text).FontSize(9);
                                }

                                DataCell(item.ProductName);
                                DataCell(item.Quantity.ToString(), true);
                                DataCell(
                                    $"{item.UnitPrice:N0} {Currency}",
                                    true);
                                DataCell(
                                    $"{item.TotalPrice:N0} {Currency}",
                                    true);
                            }
                        });

                        col.Item().PaddingVertical(5);

                        // ─── الإجماليات ───────────────
                        col.Item().AlignLeft().Column(c =>
                        {
                            void TotalRow(string label,
                                decimal amount,
                                bool isBold = false,
                                string? color = null)
                            {
                                c.Item().Row(r =>
                                {
                                    r.ConstantItem(120)
                                        .Text(label)
                                        .Bold();

                                    var amountText = r
                                        .ConstantItem(100)
                                        .AlignLeft()
                                        .Text($"{amount:N0} {Currency}");

                                    if (isBold)
                                        amountText.Bold().FontSize(11);

                                    if (color != null)
                                        amountText.FontColor(
                                            Color.FromHex(color));
                                });
                            }

                            TotalRow("المجموع الفرعي:", data.SubTotal);

                            if (data.Discount > 0)
                                TotalRow("الخصم:", data.Discount,
                                    color: "#E74C3C");

                            if (data.DeliveryCost > 0)
                                TotalRow("التوصيل:", data.DeliveryCost);

                            c.Item().LineHorizontal(0.5f)
                                .LineColor(Color.FromHex("#DDE1E7"));

                            TotalRow("الإجمالي:", data.TotalAmount,
                                true, "#1E3A5F");
                            TotalRow("المدفوع:", data.PaidAmount,
                                color: "#27AE60");

                            if (data.RemainingAmount > 0)
                                TotalRow("المتبقي:", data.RemainingAmount,
                                    color: "#E74C3C");
                        });

                        col.Item().PaddingVertical(5)
                            .LineHorizontal(0.5f)
                            .LineColor(Color.FromHex("#DDE1E7"));

                        // ─── ملاحظات ─────────────────
                        if (!string.IsNullOrEmpty(data.Notes))
                        {
                            col.Item().Text($"ملاحظات: {data.Notes}")
                                .FontSize(8)
                                .FontColor(Color.FromHex("#7F8C8D"));
                        }

                        // ─── توقيع ───────────────────
                        col.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("توقيع المستلم:")
                                    .FontSize(8);
                                c.Item().PaddingTop(15)
                                    .LineHorizontal(0.5f);
                            });

                            row.ConstantItem(30);

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignLeft()
                                    .Text("ختم المصنع:")
                                    .FontSize(8);
                                c.Item().PaddingTop(15)
                                    .LineHorizontal(0.5f);
                            });
                        });

                        // ─── تذييل ───────────────────
                        col.Item().PaddingTop(10)
                            .AlignCenter()
                            .Text($"{FactoryName} — شكراً لتعاملكم معنا")
                            .FontSize(8)
                            .FontColor(Color.FromHex("#7F8C8D"));
                    });
                });
            }).GeneratePdf();
        }

        public async Task<byte[]> GenerateDailySalesPdfAsync(DateTime date)
        {
            var data = await GetDailySalesAsync(date);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15, Unit.Millimetre);
                    page.DefaultTextStyle(x =>
                        x.FontFamily("Arial").FontSize(9));

                    page.Content().Column(col =>
                    {
                        // ─── الرأس ────────────────────
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(FactoryName)
                                    .Bold().FontSize(18)
                                    .FontColor(Color.FromHex("#1E3A5F"));
                                c.Item()
                                    .Text("تقرير المبيعات اليومية")
                                    .FontSize(12)
                                    .FontColor(Color.FromHex("#7F8C8D"));
                            });

                            row.ConstantItem(120).Column(c =>
                            {
                                c.Item().AlignLeft()
                                    .Text(date.ToString("dd/MM/yyyy"))
                                    .Bold().FontSize(14);
                                c.Item().AlignLeft()
                                    .Text($"طُبع: " +
                                    $"{DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8)
                                    .FontColor(Color.FromHex("#7F8C8D"));
                            });
                        });

                        col.Item().PaddingVertical(6)
                            .LineHorizontal(2)
                            .LineColor(Color.FromHex("#1E3A5F"));

                        // ─── بطاقات الملخص ────────────
                        col.Item().Row(row =>
                        {
                            void SummaryCard(string label,
                                string value, string color)
                            {
                                row.RelativeItem()
                                    .Padding(4)
                                    .Background(Color.FromHex(color))
                                    .Padding(8)
                                    .Column(c =>
                                    {
                                        c.Item()
                                            .Text(label)
                                            .FontSize(8)
                                            .FontColor(Colors.White);
                                        c.Item()
                                            .Text(value)
                                            .Bold().FontSize(13)
                                            .FontColor(Colors.White);
                                    });
                            }

                            SummaryCard("إجمالي الطلبات",
                                data.TotalOrders.ToString(), "#1E3A5F");
                            SummaryCard("إجمالي المبيعات",
                                $"{data.TotalSales:N0} {Currency}",
                                "#E67E22");
                            SummaryCard("المحصّل",
                                $"{data.TotalCollected:N0} {Currency}",
                                "#27AE60");
                            SummaryCard("المتبقي",
                                $"{data.TotalRemaining:N0} {Currency}",
                                "#E74C3C");
                        });

                        col.Item().PaddingVertical(8);

                        // ─── جدول الطلبات ─────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(1.5f);
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1.5f);
                                cols.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                void H(string t)
                                {
                                    header.Cell()
                                        .Background(
                                            Color.FromHex("#1E3A5F"))
                                        .Padding(5)
                                        .AlignCenter()
                                        .Text(t)
                                        .FontColor(Colors.White)
                                        .Bold().FontSize(8);
                                }

                                H("رقم الطلب");
                                H("العميل");
                                H("الإجمالي");
                                H("المحصّل");
                                H("المتبقي");
                                H("الدفع");
                                H("الحالة");
                            });

                            bool even = false;
                            foreach (var order in data.Orders)
                            {
                                var bg = even
                                    ? Color.FromHex("#F8F9FA")
                                    : Colors.White;
                                even = !even;

                                void D(string t,
                                    bool center = false,
                                    string? clr = null)
                                {
                                    var cell = table.Cell()
                                        .Background(bg)
                                        .Padding(4);

                                    var txt = center
                                        ? cell.AlignCenter().Text(t)
                                        : cell.Text(t);

                                    txt.FontSize(8);
                                    if (clr != null)
                                        txt.FontColor(Color.FromHex(clr));
                                }

                                D(order.OrderNumber);
                                D(order.CustomerName);
                                D($"{order.TotalAmount:N0}", true);
                                D($"{order.PaidAmount:N0}", true,
                                    "#27AE60");
                                D($"{order.RemainingAmount:N0}", true,
                                    order.RemainingAmount > 0
                                        ? "#E74C3C" : null);
                                D(order.PaymentType, true);
                                D(order.Status, true);
                            }
                        });

                        // ─── الإجمالي ─────────────────
                        col.Item().PaddingTop(8).AlignLeft().Row(r =>
                        {
                            r.ConstantItem(200)
                                .Background(Color.FromHex("#F0F2F5"))
                                .Padding(8)
                                .Column(c =>
                                {
                                    void TRow(string lbl, string val,
                                        string? clr = null)
                                    {
                                        c.Item().Row(rw =>
                                        {
                                            rw.RelativeItem()
                                                .Text(lbl).Bold();
                                            var t = rw.ConstantItem(80)
                                                .AlignLeft().Text(val);
                                            if (clr != null)
                                                t.FontColor(
                                                    Color.FromHex(clr));
                                        });
                                    }

                                    TRow("إجمالي المبيعات:",
                                        $"{data.TotalSales:N0} {Currency}");
                                    TRow("إجمالي المحصّل:",
                                        $"{data.TotalCollected:N0} {Currency}",
                                        "#27AE60");
                                    TRow("إجمالي المتبقي:",
                                        $"{data.TotalRemaining:N0} {Currency}",
                                        "#E74C3C");
                                });
                        });
                    });

                    page.Footer().AlignCenter()
                        .Text(x =>
                        {
                            x.Span("صفحة ").FontSize(8);
                            x.CurrentPageNumber().FontSize(8);
                            x.Span(" من ").FontSize(8);
                            x.TotalPages().FontSize(8);
                        });
                });
            }).GeneratePdf();
        }

        public async Task<byte[]> GenerateCustomerDebtPdfAsync()
        {
            var data = await GetCustomerDebtReportAsync();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15, Unit.Millimetre);
                    page.DefaultTextStyle(x =>
                        x.FontFamily("Arial").FontSize(9));

                    page.Content().Column(col =>
                    {
                        // الرأس
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(FactoryName)
                                    .Bold().FontSize(18)
                                    .FontColor(Color.FromHex("#1E3A5F"));
                                c.Item().Text("تقرير ديون العملاء")
                                    .FontSize(12)
                                    .FontColor(Color.FromHex("#E74C3C"));
                            });
                            row.ConstantItem(120).Column(c =>
                            {
                                c.Item().AlignLeft()
                                    .Text(DateTime.Today.ToString(
                                        "dd/MM/yyyy"))
                                    .Bold().FontSize(12);
                                c.Item().AlignLeft()
                                    .Text($"عدد العملاء: " +
                                    $"{data.CustomersCount}")
                                    .FontSize(9);
                            });
                        });

                        col.Item().PaddingVertical(6)
                            .LineHorizontal(2)
                            .LineColor(Color.FromHex("#E74C3C"));

                        // إجمالي الديون
                        col.Item()
                            .Background(Color.FromHex("#FDEDEC"))
                            .Padding(10)
                            .Text(
                            $"إجمالي الديون: " +
                            $"{data.TotalDebt:N0} {Currency}")
                            .Bold().FontSize(13)
                            .FontColor(Color.FromHex("#E74C3C"));

                        col.Item().PaddingVertical(6);

                        // جدول الديون
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1.5f);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                void H(string t)
                                {
                                    header.Cell()
                                        .Background(
                                            Color.FromHex("#E74C3C"))
                                        .Padding(5)
                                        .AlignCenter()
                                        .Text(t)
                                        .FontColor(Colors.White)
                                        .Bold().FontSize(8);
                                }

                                H("العميل");
                                H("الهاتف");
                                H("الدين");
                                H("الطلبات");
                                H("آخر طلب");
                            });

                            bool even = false;
                            foreach (var row in data.Rows)
                            {
                                var bg = even
                                    ? Color.FromHex("#FEF9F9")
                                    : Colors.White;
                                even = !even;

                                void D(string t,
                                    bool center = false,
                                    string? clr = null)
                                {
                                    var cell = table.Cell()
                                        .Background(bg).Padding(4);
                                    var txt = center
                                        ? cell.AlignCenter().Text(t)
                                        : cell.Text(t);
                                    txt.FontSize(8);
                                    if (clr != null)
                                        txt.FontColor(Color.FromHex(clr));
                                }

                                D(row.CustomerName);
                                D(row.Phone ?? "-", true);
                                D($"{row.TotalDebt:N0} {Currency}",
                                    true, "#E74C3C");
                                D(row.UnpaidOrders.ToString(), true);
                                D(row.LastOrderDate.HasValue
                                    ? row.LastOrderDate.Value
                                        .ToString("dd/MM/yyyy")
                                    : "-", true);
                            }
                        });
                    });
                });
            }).GeneratePdf();
        }

        public async Task<byte[]> GenerateProductionPdfAsync(
            DateTime from, DateTime to)
        {
            var data = await GetProductionReportAsync(from, to);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15, Unit.Millimetre);
                    page.DefaultTextStyle(x =>
                        x.FontFamily("Arial").FontSize(9));

                    page.Content().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(FactoryName)
                                    .Bold().FontSize(18)
                                    .FontColor(Color.FromHex("#1E3A5F"));
                                c.Item().Text("تقرير الإنتاج")
                                    .FontSize(12)
                                    .FontColor(Color.FromHex("#7F8C8D"));
                            });
                            row.ConstantItem(150).Column(c =>
                            {
                                c.Item().AlignLeft().Text(
                                    $"من: {from:dd/MM/yyyy} " +
                                    $"إلى: {to:dd/MM/yyyy}")
                                    .Bold().FontSize(10);
                            });
                        });

                        col.Item().PaddingVertical(5)
                            .LineHorizontal(2)
                            .LineColor(Color.FromHex("#1E3A5F"));

                        // ملخص
                        col.Item().Row(row =>
                        {
                            void Card(string lbl, string val,
                                string color)
                            {
                                row.RelativeItem()
                                    .Padding(4)
                                    .Background(Color.FromHex(color))
                                    .Padding(8)
                                    .Column(c =>
                                    {
                                        c.Item().Text(lbl).FontSize(8)
                                            .FontColor(Colors.White);
                                        c.Item().Text(val)
                                            .Bold().FontSize(13)
                                            .FontColor(Colors.White);
                                    });
                            }

                            Card("إجمالي المنتج",
                                data.TotalProduced.ToString(), "#1E3A5F");
                            Card("التالف",
                                data.TotalDefective.ToString(), "#E74C3C");
                            Card("الصافي",
                                data.TotalNet.ToString(), "#27AE60");
                        });

                        col.Item().PaddingVertical(8);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(1.5f);
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(1.5f);
                                cols.RelativeColumn(1.5f);
                                cols.RelativeColumn(1.5f);
                                cols.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                void H(string t)
                                {
                                    header.Cell()
                                        .Background(
                                            Color.FromHex("#1E3A5F"))
                                        .Padding(5)
                                        .AlignCenter()
                                        .Text(t)
                                        .FontColor(Colors.White)
                                        .Bold().FontSize(8);
                                }

                                H("التاريخ");
                                H("المنتج");
                                H("الوردية");
                                H("المنتج");
                                H("التالف");
                                H("الصافي");
                            });

                            bool even = false;
                            foreach (var row in data.Rows)
                            {
                                var bg = even
                                    ? Color.FromHex("#F8F9FA")
                                    : Colors.White;
                                even = !even;

                                void D(string t,
                                    bool center = true,
                                    string? clr = null)
                                {
                                    var cell = table.Cell()
                                        .Background(bg).Padding(4);
                                    var txt = center
                                        ? cell.AlignCenter().Text(t)
                                        : cell.Text(t);
                                    txt.FontSize(8);
                                    if (clr != null)
                                        txt.FontColor(Color.FromHex(clr));
                                }

                                D(row.Date.ToString("dd/MM"));
                                D(row.ProductName, false);
                                D(row.Shift);
                                D(row.Produced.ToString());
                                D(row.Defective.ToString(),
                                    true, "#E74C3C");
                                D(row.Net.ToString(),
                                    true, "#27AE60");
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("صفحة ").FontSize(8);
                        x.CurrentPageNumber().FontSize(8);
                        x.Span(" من ").FontSize(8);
                        x.TotalPages().FontSize(8);
                    });
                });
            }).GeneratePdf();
        }

        public async Task<byte[]> GenerateInventoryPdfAsync()
        {
            var data = await GetInventoryReportAsync();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15, Unit.Millimetre);
                    page.DefaultTextStyle(x =>
                        x.FontFamily("Arial").FontSize(9));

                    page.Content().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(FactoryName)
                                    .Bold().FontSize(18)
                                    .FontColor(Color.FromHex("#1E3A5F"));
                                c.Item().Text("تقرير المخزون")
                                    .FontSize(12)
                                    .FontColor(Color.FromHex("#7F8C8D"));
                            });
                            row.ConstantItem(100).Column(c =>
                            {
                                c.Item().AlignLeft()
                                    .Text(DateTime.Today.ToString(
                                        "dd/MM/yyyy"))
                                    .Bold().FontSize(12);
                            });
                        });

                        col.Item().PaddingVertical(5)
                            .LineHorizontal(2)
                            .LineColor(Color.FromHex("#1E3A5F"));

                        col.Item().Row(row =>
                        {
                            void Card(string lbl, string val,
                                string color)
                            {
                                row.RelativeItem()
                                    .Padding(4)
                                    .Background(Color.FromHex(color))
                                    .Padding(8).Column(c =>
                                    {
                                        c.Item().Text(lbl)
                                            .FontSize(8)
                                            .FontColor(Colors.White);
                                        c.Item().Text(val)
                                            .Bold().FontSize(13)
                                            .FontColor(Colors.White);
                                    });
                            }

                            Card("إجمالي البلوك",
                                $"{data.TotalBlocks:N0}", "#1E3A5F");
                            Card("منتجات منخفضة",
                                data.LowStockCount.ToString(), "#F39C12");
                        });

                        col.Item().PaddingVertical(8);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                void H(string t)
                                {
                                    header.Cell()
                                        .Background(
                                            Color.FromHex("#1E3A5F"))
                                        .Padding(5)
                                        .AlignCenter()
                                        .Text(t)
                                        .FontColor(Colors.White)
                                        .Bold().FontSize(8);
                                }

                                H("النوع");
                                H("المنتج");
                                H("الكمية");
                                H("الحد الأدنى");
                                H("الحالة");
                            });

                            bool even = false;
                            foreach (var row in data.Rows)
                            {
                                var statusColor =
                                    row.Status == "نفذ"
                                        ? "#E74C3C"
                                        : row.Status == "منخفض"
                                            ? "#F39C12"
                                            : "#27AE60";

                                var bg = even
                                    ? Color.FromHex("#F8F9FA")
                                    : Colors.White;
                                even = !even;

                                void D(string t,
                                    bool center = false,
                                    string? clr = null)
                                {
                                    var cell = table.Cell()
                                        .Background(bg).Padding(4);
                                    var txt = center
                                        ? cell.AlignCenter().Text(t)
                                        : cell.Text(t);
                                    txt.FontSize(8);
                                    if (clr != null)
                                        txt.FontColor(Color.FromHex(clr));
                                }

                                D(row.ProductType);
                                D(row.ProductName);
                                D(row.QuantityAvailable.ToString(),
                                    true);
                                D(row.MinThreshold.ToString(), true);
                                D(row.Status, true, statusColor);
                            }
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════
        // الطباعة
        // ═══════════════════════════════════════════

        public async Task PrintInvoiceAsync(int orderId)
        {
            var pdfBytes = await GenerateInvoicePdfAsync(orderId);
            await PrintReportAsync(pdfBytes);
        }

        public async Task PrintReportAsync(byte[] pdfBytes)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
                return;

            var dir = Path.Combine(Path.GetTempPath(), "BlockFactory_Print");
            Directory.CreateDirectory(dir);

            var tempFile = Path.Combine(
                dir,
                $"report_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.pdf");

            await File.WriteAllBytesAsync(tempFile, pdfBytes);
            var fullPath = Path.GetFullPath(tempFile);

            OpenPdfWithDefaultApp(fullPath);

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(10));
                try
                {
                    File.Delete(fullPath);
                }
                catch
                {
                    // تجاهل — قد يكون الملف ما زال مفتوحاً
                }
            });
        }

        /// <summary>
        /// يفتح ملف PDF باستخدام الارتباط الافتراضي في ويندوز.
        /// لا نستخدم Verb=print لأنه يفشل إذا لم يُسجَّل معالج طباعة لـ PDF.
        /// </summary>
        private static void OpenPdfWithDefaultApp(string fullPath)
        {
            Exception? firstError = null;

            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(fullPath)
                        ?? Environment.GetFolderPath(
                            Environment.SpecialFolder.UserProfile)
                });

                if (p != null)
                    return;
            }
            catch (Exception ex)
            {
                firstError = ex;
            }

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // احتياطي: يربط الملف بـ «فتح» الافتراضي حتى لو فشل الاستدعاء المباشر
                    using var p = Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments =
                            "/c start \"\" \"" +
                            fullPath.Replace("\"", "\\\"") + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });

                    if (p != null)
                        return;
                }
                catch (Exception ex)
                {
                    firstError ??= ex;
                }
            }

            throw new InvalidOperationException(
                "تعذّر فتح ملف PDF. من إعدادات ويندوز: «التطبيقات الافتراضية» ← " +
                "«اختيار التطبيقات الافتراضية حسب نوع الملف» ← عيّن قارئ PDF " +
                "(مثل Microsoft Edge أو Adobe Reader) لملفات ‎.pdf. " +
                (firstError != null
                    ? $"({firstError.Message})"
                    : string.Empty),
                firstError);
        }

        // ─── Helpers ────────────────────────────────
        private static string GetPaymentTypeAr(PaymentType t) => t switch
        {
            PaymentType.Cash       => "نقد",
            PaymentType.Electronic => "تحويل",
            PaymentType.Credit     => "آجل",
            PaymentType.Pledge     => "رهن",
            PaymentType.Mixed      => "مختلط",
            _ => "-"
        };

        private static string GetPaymentStatusAr(PaymentStatus s) => s switch
        {
            PaymentStatus.FullyPaid     => "مسدد",
            PaymentStatus.PartiallyPaid => "جزئي",
            PaymentStatus.Unpaid        => "غير مسدد",
            _ => "-"
        };
    }
}
