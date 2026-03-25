namespace AuntiesCleaners.Client.Models;

public class WorkerPayReport
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<WorkerPayment> WorkerPayments { get; set; } = new();
    public decimal GrandTotal => WorkerPayments.Sum(wp => wp.Total);
}

public class WorkerPayment
{
    public string WorkerName { get; set; } = string.Empty;
    public decimal CleaningPay { get; set; }
    public decimal LaundryPay { get; set; }
    public decimal MaintenancePay { get; set; }
    public decimal LawnPay { get; set; }
    public decimal MiscPay { get; set; }
    public List<WorkerPayLineItem> CleaningItems { get; set; } = new();
    public List<WorkerPayLineItem> LaundryItems { get; set; } = new();
    public List<WorkerPayLineItem> MaintenanceItems { get; set; } = new();
    public List<WorkerPayLineItem> LawnItems { get; set; } = new();
    public List<WorkerPayLineItem> MiscItems { get; set; } = new();
    public decimal Total => CleaningPay + LaundryPay + MaintenancePay + LawnPay + MiscPay;
}

public class WorkerPayLineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
