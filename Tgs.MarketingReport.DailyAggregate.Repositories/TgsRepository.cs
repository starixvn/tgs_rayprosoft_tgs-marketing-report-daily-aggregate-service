using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Tgs.MarketingReport.DailyAggregate.Repositories.Entities;
using Tgs.MarketingReport.DailyAggregate.Core;

namespace Tgs.MarketingReport.DailyAggregate.Repositories;

internal class TgsRepository : ITgsRepository
{
    private readonly TgsContext _context;

    public TgsRepository(
        TgsContext context)
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
            .UtcDateTime.ToUniversalTime();

        var endTime = new DateTimeOffset(to.Year, to.Month, to.Day, 0, 0, 0, TimeSpan.FromHours(8))
            .UtcDateTime.ToUniversalTime();

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
                       gaming_count = excluded.gaming_count
                ";

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
}
