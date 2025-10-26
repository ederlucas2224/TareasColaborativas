using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dapper;
using Data;
using Serilog;

namespace Service
{
	public class AuditBackgroundService : BackgroundService
		{
			private readonly Channel<AuditLogEntry> _channel;
			private readonly IDbConnectionFactory _dbFactory;
			private readonly ILogger<AuditBackgroundService> _logger;

			public AuditBackgroundService(Channel<AuditLogEntry> channel, IDbConnectionFactory dbFactory, ILogger<AuditBackgroundService> logger)
			{
				_channel = channel;
				_dbFactory = dbFactory;
				_logger = logger;
			}

			protected override async Task ExecuteAsync(CancellationToken stoppingToken)
			{
				await foreach (var entry in _channel.Reader.ReadAllAsync(stoppingToken))
				{
					try
					{
						using var conn = _dbFactory.CreateConnection();
						var sql = @"INSERT INTO audit_logs (task_id, previous_status, new_status, changed_by, timestamp)
                                VALUES (@TaskId, @PreviousStatus, @NewStatus, @ChangedBy, @Timestamp)";
						await conn.ExecuteAsync(sql, entry);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to persist audit log for task {TaskId}", entry.TaskId);
					}
				}
			}
		}
}
