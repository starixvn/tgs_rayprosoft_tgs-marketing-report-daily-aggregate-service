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
with register As (
	select
		agent_id,
		to_char(registered_on at time zone 'Asia/Shanghai', 'YYYY-MM-DD') as report_date,
		Count(1) as register_count
	from
		dbo.players
	where
		agent_id is not null
		and registered_on >= :start_time
		and registered_on < :end_time
	group by
		agent_id,
		to_char(registered_on at time zone 'Asia/Shanghai', 'YYYY-MM-DD')
),
deposit As (
	select
		agent_id,
		to_char(first_deposit_on  at time zone 'Asia/Shanghai', 'YYYY-MM-DD') as report_date,
		Count(1) as first_deposit_count
	from
		dbo.players
	where
		agent_id is not null
		and first_deposit_on >= :start_time
		and first_deposit_on < :end_time
	group by
		agent_id,
		to_char(first_deposit_on at time zone 'Asia/Shanghai', 'YYYY-MM-DD')
),
gaming As (
	select
		agent_id ,
		to_char(report_date at time zone 'Asia/Shanghai', 'YYYY-MM-DD') as report_date,
		count(1) as gaming_count
	from report.player_aggregates ra
	where
		agent_id is not null
		and report_date >= :start_time
		and report_date < :end_time
	group by
		agent_id ,
		to_char(report_date at time zone 'Asia/Shanghai', 'YYYY-MM-DD')
)
insert into report.marketing_report_daily_aggregates(agent_id, report_date, register_count, first_deposit_count, gaming_count)
select
	distinct
	coalesce(r.agent_id, d.agent_id, g.agent_id) as agent_id,
	to_date(coalesce(r.report_date, d.report_date, g.report_date), 'YYYY-MM-DD')  as report_date,
	coalesce(r.register_count, 0) as register_count,
	coalesce(d.first_deposit_count, 0) as first_deposit_count,
	coalesce(g.gaming_count, 0) as gaming_count
from register r
full join deposit d on r.agent_id = d.agent_id and r.report_date = d.report_date
full join gaming g on r.agent_id = g.agent_id and r.report_date = g.report_date
on conflict (agent_id, report_date) do nothing
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
