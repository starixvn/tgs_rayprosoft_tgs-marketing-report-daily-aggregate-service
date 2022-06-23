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
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        var sql = @"
                WITH register AS (
	                SELECT
		                agent_id,
		                to_char(registered_on at time zone 'Asia/Shanghai', 'YYYY-MM-DD') AS report_date,
		                COUNT(1) AS register_count
	                FROM
		                dbo.players
	                WHERE
		                agent_id IS NOT NULL
		                and registered_on >= :start_time
		                and registered_on < :end_time
	                GROUP BY
		                agent_id,
		                to_char(registered_on at time zone 'Asia/Shanghai', 'YYYY-MM-DD')
                ),
                deposit AS (
	                SELECT
		                agent_id,
		                to_char(first_deposit_on  at time zone 'Asia/Shanghai', 'YYYY-MM-DD') AS report_date,
		                COUNT(1) AS first_deposit_count
	                FROM
		                dbo.players
	                WHERE
		                agent_id IS NOT NULL
		                and first_deposit_on >= :start_time
		                and first_deposit_on < :end_time
	                GROUP BY
		                agent_id,
		                to_char(first_deposit_on at time zone 'Asia/Shanghai', 'YYYY-MM-DD')
                ),
                gaming AS (
	                SELECT
		                agent_id ,
		                to_char(report_date at time zone 'Asia/Shanghai', 'YYYY-MM-DD') AS report_date,
		                COUNT(1) AS gaming_count
	                FROM report.player_aggregates ra
	                WHERE
		                agent_id IS NOT NULL
		                and report_date >= :start_time
		                and report_date < :end_time
	                GROUP BY
		                agent_id ,
		                to_char(report_date at time zone 'Asia/Shanghai', 'YYYY-MM-DD')
                )
                INSERT INTO report.marketing_report_daily_aggregates(agent_id, report_date, register_count, first_deposit_count, gaming_count)
                SELECT
	                DISTINCT
	                COALESCE(r.agent_id, d.agent_id, g.agent_id) AS agent_id,
	                to_date(COALESCE(r.report_date, d.report_date, g.report_date), 'YYYY-MM-DD') AS report_date,
	                COALESCE(r.register_count, 0) AS register_count,
	                COALESCE(d.first_deposit_count, 0) AS first_deposit_count,
	                COALESCE(g.gaming_count, 0) AS gaming_count
                FROM register AS r
                FULL JOIN deposit d ON r.agent_id = d.agent_id AND r.report_date = d.report_date
                FULL JOIN gaming g ON r.agent_id = g.agent_id AND r.report_date = g.report_date
                ON CONFLICT (agent_id, report_date) DO NOTHING
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
