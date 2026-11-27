
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LightExcel.StandardWpf.Models;
using Microsoft.Data.Sqlite;

namespace LightExcel.StandardWpf.Services;

/// <summary>
/// SQLite 订单数据仓储。
///
/// 压力测试模式：
/// 1. 默认只读取数据库前 20 列；需要全字段时把 maxColumns 传 100。
/// 2. 不分页，行数上限由 maxRows 决定（默认 100 万行）。
/// 3. 一次性加载所选数据到内存。
/// 4. 不修改 CorrugatedOrder.cs。
/// 5. 使用后台线程避免阻塞 WPF UI。
/// </summary>
public sealed class OrderRepository
{
    private const string TableName = "corrugated_order";

    private readonly string _connectionString;
    private readonly int _maxColumns;
    private readonly int _maxRows;

    /// <param name="databasePath">SQLite 文件路径。</param>
    /// <param name="maxColumns">
    /// 读取前几列，1..100。取 100 时走 <see cref="CorrugatedOrder.FromReader"/> 全字段映射；
    /// 小于 100 时只映射前 20 个属性（原来的压测快路径）。
    /// </param>
    /// <param name="maxRows">SQL 的 LIMIT，最多加载多少行。</param>
    public OrderRepository(string databasePath, int maxColumns = 20, int maxRows = 1_000_000)
    {
        if (maxColumns is < 1 or > CorrugatedOrder.ColumnCount)
            throw new ArgumentOutOfRangeException(
                nameof(maxColumns), maxColumns,
                $"列数必须在 1..{CorrugatedOrder.ColumnCount} 之间。");

        if (maxRows < 1)
            throw new ArgumentOutOfRangeException(nameof(maxRows), maxRows, "行数上限必须大于 0。");

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        _maxColumns = maxColumns;
        _maxRows = maxRows;

        DatabasePath = databasePath;
    }

    /// <summary>数据库文件路径，界面上用于提示当前数据源。</summary>
    public string DatabasePath { get; }

    /// <summary>
    /// 最近一次成功加载的记录数量。
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// 后台线程加载全部订单。
    /// </summary>
    public Task<List<CorrugatedOrder>> LoadAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => LoadAll(cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// 读取前 <see cref="_maxColumns"/> 列，最多 <see cref="_maxRows"/> 行。
    /// </summary>
    private List<CorrugatedOrder> LoadAll(
        CancellationToken cancellationToken)
    {
        var orders = new List<CorrugatedOrder>(Math.Min(_maxRows, 100_000));

        using var connection =
            new SqliteConnection(_connectionString);

        connection.Open();

        // 第一步：获取数据库实际字段名。
        var columns = new List<string>(_maxColumns);

        using (var schemaCommand = connection.CreateCommand())
        {
            schemaCommand.CommandText =
                $"PRAGMA table_info(\"{TableName}\")";

            using var schemaReader =
                schemaCommand.ExecuteReader();

            while (schemaReader.Read() &&
                   columns.Count < _maxColumns)
            {
                cancellationToken.ThrowIfCancellationRequested();

                columns.Add(schemaReader.GetString(1));
            }
        }

        if (columns.Count < _maxColumns)
        {
            throw new InvalidOperationException(
                $"数据库字段数量不足{_maxColumns}列，实际只有{columns.Count}列。");
        }

        // 第二步：构建 SQL。
        string columnNames = string.Join(", ",
            columns.ConvertAll(name =>
                "\"" + name.Replace("\"", "\"\"") + "\""));

        using var command = connection.CreateCommand();

        command.CommandText =
      $"SELECT {columnNames} " +
      $"FROM \"{TableName}\" ORDER BY rowid LIMIT {_maxRows}";

        // 第三步：查询全部记录。
        using var reader = command.ExecuteReader();

        if (reader.FieldCount != columns.Count)
        {
            throw new InvalidOperationException(
                $"查询列数错误：期望{columns.Count}列，实际{reader.FieldCount}列。");
        }

        // 全字段模式：100 列按位置直接映射到模型（FromReader 内部按 0..99 取值）。
        if (columns.Count == CorrugatedOrder.ColumnCount)
        {
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                orders.Add(CorrugatedOrder.FromReader(reader));
            }

            Count = orders.Count;
            return orders;
        }

        // Only this load owns the lookup. The resulting orders share matching
        // text, and the lookup itself can be collected after loading.
        const int maximumSharedValues = 4096;
        var sharedText = new HashSet<string>(StringComparer.Ordinal);
        string ReadSharedText(int column)
        {
            if (reader.IsDBNull(column)) return string.Empty;
            string value = reader.GetString(column);
            if (value.Length is 0 or > 256) return value;
            if (sharedText.TryGetValue(value, out var existing)) return existing;
            if (sharedText.Count < maximumSharedValues) sharedText.Add(value);
            return value;
        }

        // 第四步：逐行转换成 CorrugatedOrder。
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 只映射前20列，不再调用原来的FromReader。
            var order = new CorrugatedOrder
            {
                OrderNo = reader.IsDBNull(0) ? "" : reader.GetString(0),

                OrderDate = reader.IsDBNull(1)
                    ? default
                    : DateTime.Parse(reader.GetString(1)),

                CustCode = ReadSharedText(2),
                CustName = ReadSharedText(3),
                Industry = ReadSharedText(4),

                DeliveryDate = reader.IsDBNull(5)
                    ? default
                    : DateTime.Parse(reader.GetString(5)),

                OrderStatus = ReadSharedText(6),
                Salesman = ReadSharedText(7),
                Plant = ReadSharedText(8),
                Priority = ReadSharedText(9),
                Product = ReadSharedText(10),
                BoxType = ReadSharedText(11),

                Layers = reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
                Flute = ReadSharedText(13),

                BoxL = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                BoxW = reader.IsDBNull(15) ? 0 : reader.GetInt32(15),
                BoxH = reader.IsDBNull(16) ? 0 : reader.GetInt32(16),

                BoardThickness = reader.IsDBNull(17)
                    ? 0
                    : reader.GetDouble(17),

                DevelopL = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
                DevelopW = reader.IsDBNull(19) ? 0 : reader.GetInt32(19)
            };

            orders.Add(order);
        }

        Count = orders.Count;

        return orders;
    }
}
