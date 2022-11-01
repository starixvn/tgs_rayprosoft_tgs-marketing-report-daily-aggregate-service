using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Tgs.MarketingReport.DailyAggregate.Core;
using Tgs.MarketingReport.DailyAggregate.Repositories.Entities;

namespace Tgs.MarketingReport.DailyAggregate.Repositories;

internal class TgsRepository : ITgsRepository
{
    private const string AgentDailyMarketingKey = "AgentDailyMarketing";
    private const string PlayerDailyGamingKey = "PlayerDailyGaming";
    private const string PlayerDailyRegisterKey = "PlayerDailyRegister";
    private readonly TgsContext _context;

    public TgsRepository(TgsContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async ValueTask GenerateDailyAggregateReportAsync(
        DateOnly processDate,
        CancellationToken cancellationToken = default)
    {
        var from = processDate.AddDays(-1);
        var to = processDate;

        var startTime = new DateTimeOffset(from.Year, from.Month, from.Day, 0, 0, 0, TimeSpan.FromHours(8))
            .UtcDateTime;

        var endTime = new DateTimeOffset(to.Year, to.Month, to.Day, 0, 0, 0, TimeSpan.FromHours(8))
            .UtcDateTime;

        var sql = @"
                WITH register AS (
                    SELECT
                        agent_id,
                        (registered_on at time zone 'Asia/Shanghai')::date AS report_date,
                        COUNT(1) AS register_count
                    FROM
                        dbo.players
                    WHERE
                        agent_id IS NOT NULL
                        and registered_on >= :start_time
                        and registered_on < :end_time
                    GROUP BY
                        agent_id,
                        (registered_on at time zone 'Asia/Shanghai')::date
                ),
                deposit AS (
                    SELECT
                        agent_id,
                        (first_deposit_on at time zone 'Asia/Shanghai')::date AS report_date,
                        COUNT(1) AS first_deposit_count
                    FROM
                        dbo.players
                    WHERE
                        agent_id IS NOT NULL
                        and first_deposit_on >= :start_time
                        and first_deposit_on < :end_time
                    GROUP BY
                        agent_id,
                        (first_deposit_on at time zone 'Asia/Shanghai')::date
                ),
                gaming AS (
                   SELECT
                       g.agent_id,
                       g.report_date,
                       COUNT(1) AS gaming_count
                   FROM (
                        SELECT DISTINCT
                            agent_id,
                            (report_date at time zone 'Asia/Shanghai')::date AS report_date,
                            player_code
                        FROM report.player_aggregates ra
                        WHERE
                            agent_id IS NOT NULL
                            and report_date >= :start_time
                            and report_date < :end_time
                            and bet_amount > 0
                    ) g
                    GROUP BY g.agent_id, g.report_date
                )
                INSERT INTO report.marketing_report_daily_aggregates(agent_id, report_date, register_count, first_deposit_count, gaming_count)
                SELECT
                    DISTINCT
                    COALESCE(m.agent_id, g.agent_id) AS agent_id,
                    COALESCE(m.report_date, g.report_date) AS report_date,
                    COALESCE(m.register_count, 0) AS register_count,
                    COALESCE(m.first_deposit_count, 0) AS first_deposit_count,
                    COALESCE(g.gaming_count, 0) AS gaming_count
                FROM
                    (SELECT
                        DISTINCT
                        COALESCE(r.agent_id, d.agent_id) AS agent_id,
                        COALESCE(r.report_date, d.report_date) AS report_date,
                        COALESCE(r.register_count, 0) AS register_count,
                        COALESCE(d.first_deposit_count, 0) AS first_deposit_count
                    FROM register AS r
                    FULL JOIN deposit d ON r.agent_id = d.agent_id AND r.report_date = d.report_date) AS m
                FULL JOIN gaming g ON m.agent_id = g.agent_id AND m.report_date = g.report_date
                ON CONFLICT (agent_id, report_date) DO
                UPDATE
                   SET register_count = excluded.register_count,
                       first_deposit_count = excluded.first_deposit_count,
                       gaming_count = excluded.gaming_count";

        var parameters = new NpgsqlParameter[]
        {
            new (":start_time", NpgsqlDbType.TimestampTz)
            {
                Value = startTime
            },
            new(":end_time", NpgsqlDbType.TimestampTz)
            {
                Value = endTime
            }
        };

        _ = await _context.Database.ExecuteSqlRawAsync(
             sql, parameters, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<DateOnly?> GetAgentDailyMarketingStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _context.MarketingStatisticsSettings
            .FirstOrDefaultAsync(
                setting => setting.RecordKey == AgentDailyMarketingKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var value = setting is null
            ? (DateTimeOffset?)null
            : new DateTimeOffset(setting.ProcessedTime).ToOffset(TimeSpan.FromHours(8D));

        return value is null
            ? null
            : DateOnly.FromDateTime(value.Value.DateTime);
    }

    public async ValueTask<DateOnly?> GetPlayerDailyGamingStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _context.MarketingStatisticsSettings
            .FirstOrDefaultAsync(
                setting => setting.RecordKey == PlayerDailyGamingKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var value = setting is null
            ? (DateTimeOffset?)null
            : new DateTimeOffset(setting.ProcessedTime).ToOffset(TimeSpan.FromHours(8D));

        return value is null
            ? null
            : DateOnly.FromDateTime(value.Value.DateTime);
    }

    public async ValueTask<DateOnly?> GetPlayerDailyRegisterStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _context.MarketingStatisticsSettings
            .FirstOrDefaultAsync(
                setting => setting.RecordKey == PlayerDailyRegisterKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var value = setting is null
            ? (DateTimeOffset?)null
            : new DateTimeOffset(setting.ProcessedTime).ToOffset(TimeSpan.FromHours(8D));

        return value is null
            ? null
            : DateOnly.FromDateTime(value.Value.DateTime);
    }

    public async ValueTask ProcessAgentDailyMarketingStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default)
    {
        var queryDate = processDate.AddDays(-1);

        var processData = await _context.AgentDailyMarketingStatistics
            .FromSqlInterpolated(
                @$"
                WITH statistics
                AS
                (
                    SELECT
                        COALESCE(RS.report_date, GS.report_date) AS report_date,
                        COALESCE(RS.player_code, GS.player_code) AS player_code,
                        CASE
                            WHEN RS.player_code IS NULL THEN false
                            ELSE true
                        END AS is_register,
                        CASE
                            WHEN GS.player_code IS NULL THEN false
                            ELSE true
                        END AS is_gaming
                    FROM
                        dbo.player_daily_register_statistics AS RS
                        FULL JOIN
                        (
                            SELECT DISTINCT
                                report_date,
                                player_code
                            FROM
                                dbo.player_daily_gaming_statistics
                            WHERE
                                report_date = {queryDate}
                        ) AS GS ON RS.player_code = GS.player_code
                            AND RS.report_date = GS.report_date
                    WHERE
                        RS.report_date = {queryDate}
                        OR
                        GS.report_date = {queryDate}
                )
                SELECT
                    P.agent_id,
                    P.brand_id,
                    S.report_date,
                    Count(1) filter ( where S.is_gaming = true ) AS gaming_count,
                    Count(1) filter ( where S.is_register = true ) AS register_count
                FROM
                    statistics AS S
                    join
                    dbo.players AS P ON S.player_code = P.player_code
                WHERE
                    P.agent_id IS NOT NULL
                GROUP BY
                    P.agent_id,
                    P.brand_id,
                    S.report_date")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var existsData = await _context.AgentDailyMarketingStatistics
            .Where(row => row.ReportDate == queryDate)
            .ToArrayAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var leftJoin = from input in processData
                       join db in existsData
                        on new { input.AgentId, input.ReportDate }
                            equals new { db.AgentId, db.ReportDate }
                        into g
                       from db in g.DefaultIfEmpty()
                       select new
                       {
                           Input = input,
                           Db = db
                       };

        var rightJoin = from db in existsData
                        join input in processData
                            on new { db.AgentId, db.ReportDate }
                                equals new { input.AgentId, input.ReportDate }
                            into g
                        from input in g.DefaultIfEmpty()
                        where input is null
                        select db;

        foreach (var p in leftJoin)
            if (p.Db is not null)
            {
                var modifyRow = p.Db;
                modifyRow.RegisterCount = p.Input.RegisterCount;
                modifyRow.GamingCount = p.Input.GamingCount;
            }
            else
                _ = await _context.AgentDailyMarketingStatistics
                    .AddAsync(p.Input, cancellationToken)
                    .ConfigureAwait(false);

        _context.RemoveRange(rightJoin);

        _ = await _context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask ProcessPlayerDailyGamingStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default)
    {
        var queryDate = processDate.AddDays(-1);
        var reportDateTime = new DateTimeOffset(
            queryDate.ToDateTime(TimeOnly.MinValue),
            TimeSpan.FromHours(8D)).ToUniversalTime();

        var processData = await _context.PlayerDailyGamingStatistics
            .FromSqlInterpolated(
                @$"
                SELECT
                    {queryDate} AS report_date,
                    PGA.player_code AS player_code,
                    GP.code AS gp_code,
                    G.sub_game_provider_code AS sub_gp_code,
                    Sum(PGA.win_loss) AS win_loss
                FROM
                    report.player_game_rebate_aggregates AS PGA
                    JOIN
                    dbo.game_providers AS GP ON PGA.game_provider_id = GP.id
                    JOIN
                    dbo.games AS G ON PGA.game_provider_id = G.game_provider_id
                        AND PGA.game_id = G.id
                WHERE
                    PGA.report_date = {reportDateTime}
                GROUP BY
                    PGA.player_code,
                    GP.code,
                    G.sub_game_provider_code")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var existsData = await _context.PlayerDailyGamingStatistics
            .Where(row => row.ReportDate == queryDate)
            .ToArrayAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var leftJoin = from input in processData
                       join db in existsData
                        on new { input.PlayerCode, input.ReportDate, input.GpCode, input.SubGpCode }
                            equals new { db.PlayerCode, db.ReportDate, db.GpCode, db.SubGpCode }
                        into g
                       from db in g.DefaultIfEmpty()
                       select new
                       {
                           Input = input,
                           Db = db
                       };

        var rightJoin = from db in existsData
                        join input in processData
                            on new { db.PlayerCode, db.ReportDate, db.GpCode, db.SubGpCode }
                                equals new { input.PlayerCode, input.ReportDate, input.GpCode, input.SubGpCode }
                            into g
                        from input in g.DefaultIfEmpty()
                        where input is null
                        select db;

        foreach (var p in leftJoin)
            if (p.Db is not null)
            {
                var modifyRow = p.Db;
                modifyRow.WinLoss = p.Input.WinLoss;
            }
            else
                _ = await _context.PlayerDailyGamingStatistics
                    .AddAsync(p.Input, cancellationToken)
                    .ConfigureAwait(false);

        _context.RemoveRange(rightJoin);

        _ = await _context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask ProcessPlayerDailyRegisterStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default)
    {
        var (From, To) = (processDate.AddDays(-1), processDate);
        var queryTimeRange = (
            From: new DateTimeOffset(
                From
                    .AddDays(-1)
                    .ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc),
                TimeSpan.Zero),
            To: new DateTimeOffset(
                To
                    .AddDays(-1)
                    .ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc),
                TimeSpan.Zero));

        var processData = await _context.PlayerDailyRegisterStatistics
            .FromSqlInterpolated(
                @$"
                SELECT
                    P.player_code,
                    {From} AS report_date,
                    COALESCE(PLR.current_login_count, 0) AS daily_login_count,
                    PIR.record_on AS last_login_on,
                    0 AS total_login_count
                FROM
                    dbo.players AS P
                    LEFT JOIN
                    (
                        SELECT
                            player_code,
                            Count(1) AS current_login_count
                        FROM
                            dbo.player_login_records
                        WHERE
                            created_on >= {queryTimeRange.From}
                            AND
                            created_on < {queryTimeRange.To}
                        GROUP BY
                            player_code
                    ) AS PLR ON P.player_code = PLR.player_code
                    LEFT JOIN
                    dbo.player_ip_records AS PIR ON P.player_code = PIR.player_code
                        AND PIR.ip_type = 2
                WHERE
                    P.registered_on >= {queryTimeRange.From}
                    AND
                    P.registered_on < {queryTimeRange.To}")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var existsData = await _context.PlayerDailyRegisterStatistics
            .Where(row => row.ReportDate == From)
            .ToArrayAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var leftJoin = from input in processData
                       join db in existsData
                        on new { input.PlayerCode, input.ReportDate }
                            equals new { db.PlayerCode, db.ReportDate }
                        into g
                       from db in g.DefaultIfEmpty()
                       select new
                       {
                           Input = input,
                           Db = db
                       };

        var rightJoin = from db in existsData
                        join input in processData
                            on new { db.PlayerCode, db.ReportDate }
                                equals new { input.PlayerCode, input.ReportDate }
                            into g
                        from input in g.DefaultIfEmpty()
                        where input is null
                        select db;

        foreach (var p in leftJoin)
            if (p.Db is not null)
            {
                var modifyRow = p.Db;
                modifyRow.LastLoginOn = p.Input.LastLoginOn;
                modifyRow.DailyLoginCount = p.Input.DailyLoginCount;
                modifyRow.TotalLoginCount = p.Input.TotalLoginCount;
            }
            else
                _ = await _context.PlayerDailyRegisterStatistics
                    .AddAsync(p.Input, cancellationToken)
                    .ConfigureAwait(false);

        _context.RemoveRange(rightJoin);

        _ = await _context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask SetAgentDailyMarketingStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var setting = await _context.MarketingStatisticsSettings
            .FirstOrDefaultAsync(
                setting => setting.RecordKey == AgentDailyMarketingKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? _context.MarketingStatisticsSettings.Add(new MarketingStatisticsSetting
            {
                RecordKey = AgentDailyMarketingKey
            }).Entity;

        setting.ProcessedTime = date
            .AddDays(-1)
            .ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc);

        _ = await _context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask SetPlayerDailyGamingStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var setting = await _context.MarketingStatisticsSettings
            .FirstOrDefaultAsync(
                setting => setting.RecordKey == PlayerDailyGamingKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? _context.MarketingStatisticsSettings.Add(new MarketingStatisticsSetting
            {
                RecordKey = PlayerDailyGamingKey
            }).Entity;

        setting.ProcessedTime = date
            .AddDays(-1)
            .ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc);

        _ = await _context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask SetPlayerDailyRegisterStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var setting = await _context.MarketingStatisticsSettings
            .FirstOrDefaultAsync(
                setting => setting.RecordKey == PlayerDailyRegisterKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? _context.MarketingStatisticsSettings.Add(new MarketingStatisticsSetting
            {
                RecordKey = PlayerDailyRegisterKey
            }).Entity;

        setting.ProcessedTime = date
            .AddDays(-1)
            .ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc);

        _ = await _context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
