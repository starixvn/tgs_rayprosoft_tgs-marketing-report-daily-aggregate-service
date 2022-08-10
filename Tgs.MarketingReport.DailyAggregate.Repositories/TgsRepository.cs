using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Tgs.MarketingReport.DailyAggregate.Core;
using Tgs.MarketingReport.DailyAggregate.Repositories.Entities;

namespace Tgs.MarketingReport.DailyAggregate.Repositories;

internal class TgsRepository : ITgsRepository
{
    private const string PlayerDailyGamingKey = "PlayerDailyGaming";
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

    public async ValueTask ProcessPlayerDailyGamingStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default)
    {
        var reportDateTime = new DateTimeOffset(
            processDate.ToDateTime(TimeOnly.MinValue),
            TimeSpan.FromHours(8D)).ToUniversalTime();

        var processData = await _context.PlayerDailyGamingStatistics
            .FromSqlInterpolated(
                @$"
                SELECT
                    {processDate} AS ReportDate,
                    PGA.player_code AS PlayerCode,
                    GP.code AS GpCode,
                    G.sub_game_provider_code AS SubGpCode,
                    Sum(PGA.win_loss) AS WinLossSummary
                FROM
                    report.player_game_rebate_aggregates AS PGA
                    JOIN
                    dbo.game_providers AS GP ON PGA.game_provider_id = GP.id
                    JOIN
                    dbo.games AS G ON PGA.game_provider_id = G.game_provider_id
                        AND PGA.game_id = G.id
                WHERE
                    PGA.report_date = {reportDateTime}
                    AND
                    PGA.agent_id IS NULL
                GROUP BY
                    PGA.player_code,
                    GP.code,
                    G.sub_game_provider_code")
            .AsNoTracking()
            .ToArrayAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var existsData = await _context.PlayerDailyGamingStatistics
            .Where(row => row.ReportDate == processDate)
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
}
