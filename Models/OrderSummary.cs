namespace LightExcel.StandardWpf.Models;

/// <summary>业务提供的汇总显示值。属性对应明细列，未赋值的属性留空。</summary>
public sealed class OrderSummary
{
    public string? OrderNo { get; init; }
    public string? CustName { get; init; }
    public string? OrderDate { get; init; }
    public string? DeliveryDate { get; init; }
    public string? OrderStatus { get; init; }
    public string? Priority { get; init; }
    public string? Layers { get; init; }
    public string? BoxL { get; init; }
    public string? BoxW { get; init; }
    public string? BoxH { get; init; }
    public string? BoardThickness { get; init; }
    public string? DevelopL { get; init; }
    public string? DevelopW { get; init; }
}
