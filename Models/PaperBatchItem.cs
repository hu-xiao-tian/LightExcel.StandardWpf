using CommunityToolkit.Mvvm.ComponentModel;

namespace LightExcel.StandardWpf.Models;

/// <summary>纸质排程演示行模型（纯 MVVM ObservableObject）。</summary>
public sealed class PaperBatchItem : ObservableObject
{
    private int _serial;
    private string _fluteCode = "BC";
    private string _paperCode = "Q/9/2/9/E";
    private int _widthMm = 2950;
    private int _meters = 100;
    private string _shift = "早班";
    private DateTime _planDate = DateTime.Today;
    private bool _isApproved;
    private bool _isEnabled = true;
    private double _progress;

    public int Serial { get => _serial; set => SetProperty(ref _serial, value); }
    public string FluteCode { get => _fluteCode; set => SetProperty(ref _fluteCode, value); }
    public string PaperCode { get => _paperCode; set => SetProperty(ref _paperCode, value); }
    public int WidthMm { get => _widthMm; set => SetProperty(ref _widthMm, value); }
    public int Meters { get => _meters; set => SetProperty(ref _meters, value); }
    public string Shift { get => _shift; set => SetProperty(ref _shift, value); }
    public DateTime PlanDate { get => _planDate; set => SetProperty(ref _planDate, value); }
    public bool IsApproved { get => _isApproved; set => SetProperty(ref _isApproved, value); }
    public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }
    public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
}
