namespace AuntiesCleaners.Client.Models;

public class BillingReport
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<BillingSection> Sections { get; set; } = new();
    public decimal GrandTotal => Sections.Sum(s => s.Subtotal);
}

public class BillingSection
{
    public string Name { get; set; } = string.Empty;
    public List<BillingLineItem> LineItems { get; set; } = new();
    public decimal Subtotal => LineItems.Sum(li => li.Amount);
}

public class BillingLineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
