using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BlockFactory.Core.Models.Sales
{
    public enum PaymentType
    {
        Cash,           // نقد
        Electronic,     // تحويل إلكتروني
        Credit,         // آجل
        Pledge,         // رهن
        Mixed           // دفع مختلط
    }

    public enum DeliveryType
    {
        Pickup,         // استلام من المصنع
        Delivery        // توصيل
    }

    public enum OrderStatus
    {
        Pending,        // قيد الانتظار
        InProduction,   // قيد الإنتاج
        Ready,          // جاهز
        Delivered,      // تم التسليم
        Cancelled       // ملغي
    }

    public enum PaymentStatus
    {
        Unpaid,         // غير مدفوع
        PartiallyPaid,  // مدفوع جزئياً
        FullyPaid       // مدفوع بالكامل
    }
}
