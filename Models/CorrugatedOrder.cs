using System.ComponentModel;

namespace LightExcel.StandardWpf.Models;

/// <summary>瓦楞纸订单：100 个强类型属性，与数据库 corrugated_order 表一一对应。</summary>
public sealed class CorrugatedOrder
{
    public const int ColumnCount = 100;

    [DisplayName("订单号")]
    [ReadOnly(true)]
    public string OrderNo { get; set; } = "";

    [DisplayName("订单日期")]
    public DateTime OrderDate { get; set; }

    [DisplayName("客户编码")]
    public string CustCode { get; set; } = "";

    [DisplayName("客户名称")]
    public string CustName { get; set; } = "";

    [DisplayName("客户行业")]
    public string Industry { get; set; } = "";

    [DisplayName("交货日期")]
    public DateTime DeliveryDate { get; set; }

    [DisplayName("订单状态")]
    public string OrderStatus { get; set; } = "";

    [DisplayName("业务员")]
    public string Salesman { get; set; } = "";

    [DisplayName("生产厂区")]
    public string Plant { get; set; } = "";

    [DisplayName("优先级")]
    public string Priority { get; set; } = "";

    [DisplayName("产品名称")]
    public string Product { get; set; } = "";

    [DisplayName("箱型代码")]
    public string BoxType { get; set; } = "";

    [DisplayName("瓦楞层数")]
    public int Layers { get; set; }

    [DisplayName("楞型")]
    public string Flute { get; set; } = "";

    [DisplayName("箱长(mm)")]
    public int BoxL { get; set; }

    [DisplayName("箱宽(mm)")]
    public int BoxW { get; set; }

    [DisplayName("箱高(mm)")]
    public int BoxH { get; set; }

    [DisplayName("纸板厚度(mm)")]
    public double BoardThickness { get; set; }

    [DisplayName("展开长(mm)")]
    public int DevelopL { get; set; }

    [DisplayName("展开宽(mm)")]
    public int DevelopW { get; set; }

    [DisplayName("面积(m²)")]
    public double Area { get; set; }

    [DisplayName("容积(L)")]
    public double Volume { get; set; }

    [DisplayName("摇盖长度(mm)")]
    public int Flap { get; set; }

    [DisplayName("开槽深度(mm)")]
    public int SlotDepth { get; set; }

    [DisplayName("接舌宽度(mm)")]
    public int GlueTab { get; set; }

    [DisplayName("模切版号")]
    public string DieNo { get; set; } = "";

    [DisplayName("印刷方式")]
    public string PrintMethod { get; set; } = "";

    [DisplayName("印刷色数")]
    public int PrintColors { get; set; }

    [DisplayName("印版数量")]
    public int PlateCount { get; set; }

    [DisplayName("表面处理")]
    public string Surface { get; set; } = "";

    [DisplayName("面纸材质")]
    public string FacePaper { get; set; } = "";

    [DisplayName("面纸克重(g/m²)")]
    public int FaceGsm { get; set; }

    [DisplayName("里纸材质")]
    public string LinerPaper { get; set; } = "";

    [DisplayName("里纸克重(g/m²)")]
    public int LinerGsm { get; set; }

    [DisplayName("芯纸1材质")]
    public string Medium1Paper { get; set; } = "";

    [DisplayName("芯纸1克重(g/m²)")]
    public int Medium1Gsm { get; set; }

    [DisplayName("芯纸2材质")]
    public string Medium2Paper { get; set; } = "";

    [DisplayName("芯纸2克重(g/m²)")]
    public int Medium2Gsm { get; set; }

    [DisplayName("芯纸3材质")]
    public string Medium3Paper { get; set; } = "";

    [DisplayName("芯纸3克重(g/m²)")]
    public int Medium3Gsm { get; set; }

    [DisplayName("中隔纸材质")]
    public string SeparatorPaper { get; set; } = "";

    [DisplayName("中隔纸克重(g/m²)")]
    public int SeparatorGsm { get; set; }

    [DisplayName("原纸总克重(g/m²)")]
    public int TotalGsm { get; set; }

    [DisplayName("原纸单价(元/吨)")]
    public double PaperPrice { get; set; }

    [DisplayName("原纸成本(元)")]
    public double PaperCost { get; set; }

    [DisplayName("淀粉胶用量(kg)")]
    public double StarchKg { get; set; }

    [DisplayName("胶水成本(元)")]
    public double StarchCost { get; set; }

    [DisplayName("边角料率(%)")]
    public double TrimRate { get; set; }

    [DisplayName("废纸回收(kg)")]
    public double WastePaper { get; set; }

    [DisplayName("材料成本(元)")]
    public double MaterialCost { get; set; }

    [DisplayName("耐破强度(kPa)")]
    public int BurstStrength { get; set; }

    [DisplayName("边压强度(N/m)")]
    public int Ect { get; set; }

    [DisplayName("抗压强度(N)")]
    public int Bct { get; set; }

    [DisplayName("戳穿强度(J)")]
    public double Puncture { get; set; }

    [DisplayName("含水率(%)")]
    public double Moisture { get; set; }

    [DisplayName("施胶度(mm)")]
    public double Sizing { get; set; }

    [DisplayName("粘合强度(N/m)")]
    public int BondStrength { get; set; }

    [DisplayName("纸板定量(g/m²)")]
    public int BoardGsm { get; set; }

    [DisplayName("厚度公差(mm)")]
    public double ThicknessTol { get; set; }

    [DisplayName("翘曲度(mm)")]
    public double Warp { get; set; }

    [DisplayName("检测日期")]
    public DateTime TestDate { get; set; }

    [DisplayName("检测员")]
    public string Inspector { get; set; } = "";

    [DisplayName("检测结论")]
    public string Verdict { get; set; } = "";

    [DisplayName("执行标准")]
    public string Standard { get; set; } = "";

    [DisplayName("质量等级")]
    public string Grade { get; set; } = "";

    [DisplayName("生产工单号")]
    public string WorkOrder { get; set; } = "";

    [DisplayName("生产线")]
    public string Line { get; set; } = "";

    [DisplayName("班次")]
    public string Shift { get; set; } = "";

    [DisplayName("生产日期")]
    public DateTime ProdDate { get; set; }

    [DisplayName("计划数量(片)")]
    public int PlanQty { get; set; }

    [DisplayName("实际产量(片)")]
    public int ActualQty { get; set; }

    [DisplayName("损耗数量(片)")]
    public int LossQty { get; set; }

    [DisplayName("成品率(%)")]
    public double YieldRate { get; set; }

    [DisplayName("生产工时(h)")]
    public double WorkHours { get; set; }

    [DisplayName("换版时间(min)")]
    public int SetupMin { get; set; }

    [DisplayName("运行速度(m/min)")]
    public int RunSpeed { get; set; }

    [DisplayName("停机次数")]
    public int StopCount { get; set; }

    [DisplayName("停机时长(min)")]
    public int StopMin { get; set; }

    [DisplayName("能耗(kWh)")]
    public double EnergyKwh { get; set; }

    [DisplayName("单位能耗(kWh/千片)")]
    public double EnergyUnit { get; set; }

    [DisplayName("单价(元/片)")]
    public double UnitPrice { get; set; }

    [DisplayName("订单金额(元)")]
    public double Amount { get; set; }

    [DisplayName("人工成本(元)")]
    public double LaborCost { get; set; }

    [DisplayName("能耗成本(元)")]
    public double EnergyCost { get; set; }

    [DisplayName("折旧成本(元)")]
    public double Depreciation { get; set; }

    [DisplayName("制造费用(元)")]
    public double Overhead { get; set; }

    [DisplayName("运输成本(元)")]
    public double Freight { get; set; }

    [DisplayName("总成本(元)")]
    public double TotalCost { get; set; }

    [DisplayName("毛利(元)")]
    public double GrossProfit { get; set; }

    [DisplayName("毛利率(%)")]
    public double GrossMargin { get; set; }

    [DisplayName("付款方式")]
    public string Payment { get; set; } = "";

    [DisplayName("账期(天)")]
    public int CreditDays { get; set; }

    [DisplayName("发票类型")]
    public string Invoice { get; set; } = "";

    [DisplayName("折扣率(%)")]
    public double Discount { get; set; }

    [DisplayName("含税单价(元)")]
    public double PriceTax { get; set; }

    [DisplayName("包装方式")]
    public string Packing { get; set; } = "";

    [DisplayName("每托数量(片)")]
    public int PerPallet { get; set; }

    [DisplayName("托盘数")]
    public int Pallets { get; set; }

    [DisplayName("运输方式")]
    public string Transport { get; set; } = "";

    [DisplayName("送货区域")]
    public string Region { get; set; } = "";

    /// <summary>按数据库列顺序填充（SELECT * 的列序与建表顺序一致）。</summary>
    public static CorrugatedOrder FromReader(Microsoft.Data.Sqlite.SqliteDataReader r) => new()
    {
        OrderNo = r.IsDBNull(0) ? "" : r.GetString(0),
        OrderDate = r.IsDBNull(1) ? default : DateTime.Parse(r.GetString(1)),
        CustCode = r.IsDBNull(2) ? "" : r.GetString(2),
        CustName = r.IsDBNull(3) ? "" : r.GetString(3),
        Industry = r.IsDBNull(4) ? "" : r.GetString(4),
        DeliveryDate = r.IsDBNull(5) ? default : DateTime.Parse(r.GetString(5)),
        OrderStatus = r.IsDBNull(6) ? "" : r.GetString(6),
        Salesman = r.IsDBNull(7) ? "" : r.GetString(7),
        Plant = r.IsDBNull(8) ? "" : r.GetString(8),
        Priority = r.IsDBNull(9) ? "" : r.GetString(9),
        Product = r.IsDBNull(10) ? "" : r.GetString(10),
        BoxType = r.IsDBNull(11) ? "" : r.GetString(11),
        Layers = r.IsDBNull(12) ? 0 : r.GetInt32(12),
        Flute = r.IsDBNull(13) ? "" : r.GetString(13),
        BoxL = r.IsDBNull(14) ? 0 : r.GetInt32(14),
        BoxW = r.IsDBNull(15) ? 0 : r.GetInt32(15),
        BoxH = r.IsDBNull(16) ? 0 : r.GetInt32(16),
        BoardThickness = r.IsDBNull(17) ? 0 : r.GetDouble(17),
        DevelopL = r.IsDBNull(18) ? 0 : r.GetInt32(18),
        DevelopW = r.IsDBNull(19) ? 0 : r.GetInt32(19),
        Area = r.IsDBNull(20) ? 0 : r.GetDouble(20),
        Volume = r.IsDBNull(21) ? 0 : r.GetDouble(21),
        Flap = r.IsDBNull(22) ? 0 : r.GetInt32(22),
        SlotDepth = r.IsDBNull(23) ? 0 : r.GetInt32(23),
        GlueTab = r.IsDBNull(24) ? 0 : r.GetInt32(24),
        DieNo = r.IsDBNull(25) ? "" : r.GetString(25),
        PrintMethod = r.IsDBNull(26) ? "" : r.GetString(26),
        PrintColors = r.IsDBNull(27) ? 0 : r.GetInt32(27),
        PlateCount = r.IsDBNull(28) ? 0 : r.GetInt32(28),
        Surface = r.IsDBNull(29) ? "" : r.GetString(29),
        FacePaper = r.IsDBNull(30) ? "" : r.GetString(30),
        FaceGsm = r.IsDBNull(31) ? 0 : r.GetInt32(31),
        LinerPaper = r.IsDBNull(32) ? "" : r.GetString(32),
        LinerGsm = r.IsDBNull(33) ? 0 : r.GetInt32(33),
        Medium1Paper = r.IsDBNull(34) ? "" : r.GetString(34),
        Medium1Gsm = r.IsDBNull(35) ? 0 : r.GetInt32(35),
        Medium2Paper = r.IsDBNull(36) ? "" : r.GetString(36),
        Medium2Gsm = r.IsDBNull(37) ? 0 : r.GetInt32(37),
        Medium3Paper = r.IsDBNull(38) ? "" : r.GetString(38),
        Medium3Gsm = r.IsDBNull(39) ? 0 : r.GetInt32(39),
        SeparatorPaper = r.IsDBNull(40) ? "" : r.GetString(40),
        SeparatorGsm = r.IsDBNull(41) ? 0 : r.GetInt32(41),
        TotalGsm = r.IsDBNull(42) ? 0 : r.GetInt32(42),
        PaperPrice = r.IsDBNull(43) ? 0 : r.GetDouble(43),
        PaperCost = r.IsDBNull(44) ? 0 : r.GetDouble(44),
        StarchKg = r.IsDBNull(45) ? 0 : r.GetDouble(45),
        StarchCost = r.IsDBNull(46) ? 0 : r.GetDouble(46),
        TrimRate = r.IsDBNull(47) ? 0 : r.GetDouble(47),
        WastePaper = r.IsDBNull(48) ? 0 : r.GetDouble(48),
        MaterialCost = r.IsDBNull(49) ? 0 : r.GetDouble(49),
        BurstStrength = r.IsDBNull(50) ? 0 : r.GetInt32(50),
        Ect = r.IsDBNull(51) ? 0 : r.GetInt32(51),
        Bct = r.IsDBNull(52) ? 0 : r.GetInt32(52),
        Puncture = r.IsDBNull(53) ? 0 : r.GetDouble(53),
        Moisture = r.IsDBNull(54) ? 0 : r.GetDouble(54),
        Sizing = r.IsDBNull(55) ? 0 : r.GetDouble(55),
        BondStrength = r.IsDBNull(56) ? 0 : r.GetInt32(56),
        BoardGsm = r.IsDBNull(57) ? 0 : r.GetInt32(57),
        ThicknessTol = r.IsDBNull(58) ? 0 : r.GetDouble(58),
        Warp = r.IsDBNull(59) ? 0 : r.GetDouble(59),
        TestDate = r.IsDBNull(60) ? default : DateTime.Parse(r.GetString(60)),
        Inspector = r.IsDBNull(61) ? "" : r.GetString(61),
        Verdict = r.IsDBNull(62) ? "" : r.GetString(62),
        Standard = r.IsDBNull(63) ? "" : r.GetString(63),
        Grade = r.IsDBNull(64) ? "" : r.GetString(64),
        WorkOrder = r.IsDBNull(65) ? "" : r.GetString(65),
        Line = r.IsDBNull(66) ? "" : r.GetString(66),
        Shift = r.IsDBNull(67) ? "" : r.GetString(67),
        ProdDate = r.IsDBNull(68) ? default : DateTime.Parse(r.GetString(68)),
        PlanQty = r.IsDBNull(69) ? 0 : r.GetInt32(69),
        ActualQty = r.IsDBNull(70) ? 0 : r.GetInt32(70),
        LossQty = r.IsDBNull(71) ? 0 : r.GetInt32(71),
        YieldRate = r.IsDBNull(72) ? 0 : r.GetDouble(72),
        WorkHours = r.IsDBNull(73) ? 0 : r.GetDouble(73),
        SetupMin = r.IsDBNull(74) ? 0 : r.GetInt32(74),
        RunSpeed = r.IsDBNull(75) ? 0 : r.GetInt32(75),
        StopCount = r.IsDBNull(76) ? 0 : r.GetInt32(76),
        StopMin = r.IsDBNull(77) ? 0 : r.GetInt32(77),
        EnergyKwh = r.IsDBNull(78) ? 0 : r.GetDouble(78),
        EnergyUnit = r.IsDBNull(79) ? 0 : r.GetDouble(79),
        UnitPrice = r.IsDBNull(80) ? 0 : r.GetDouble(80),
        Amount = r.IsDBNull(81) ? 0 : r.GetDouble(81),
        LaborCost = r.IsDBNull(82) ? 0 : r.GetDouble(82),
        EnergyCost = r.IsDBNull(83) ? 0 : r.GetDouble(83),
        Depreciation = r.IsDBNull(84) ? 0 : r.GetDouble(84),
        Overhead = r.IsDBNull(85) ? 0 : r.GetDouble(85),
        Freight = r.IsDBNull(86) ? 0 : r.GetDouble(86),
        TotalCost = r.IsDBNull(87) ? 0 : r.GetDouble(87),
        GrossProfit = r.IsDBNull(88) ? 0 : r.GetDouble(88),
        GrossMargin = r.IsDBNull(89) ? 0 : r.GetDouble(89),
        Payment = r.IsDBNull(90) ? "" : r.GetString(90),
        CreditDays = r.IsDBNull(91) ? 0 : r.GetInt32(91),
        Invoice = r.IsDBNull(92) ? "" : r.GetString(92),
        Discount = r.IsDBNull(93) ? 0 : r.GetDouble(93),
        PriceTax = r.IsDBNull(94) ? 0 : r.GetDouble(94),
        Packing = r.IsDBNull(95) ? "" : r.GetString(95),
        PerPallet = r.IsDBNull(96) ? 0 : r.GetInt32(96),
        Pallets = r.IsDBNull(97) ? 0 : r.GetInt32(97),
        Transport = r.IsDBNull(98) ? "" : r.GetString(98),
        Region = r.IsDBNull(99) ? "" : r.GetString(99)
    };
}