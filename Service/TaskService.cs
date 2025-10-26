using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Repository;
using Data;
using Model.Dtos;
using Model;

namespace Service
{
		public class TaskService : BaseService, ITaskService
		{
			private readonly ITaskRepository _repo;
			private readonly IDbConnectionFactory _dbFactory;
			private readonly ILogger<TaskService> _logger;
			private readonly Channel<AuditLogEntry> _auditChannel;

			public TaskService(ITaskRepository repo, IDbConnectionFactory dbFactory, ILogger<TaskService> logger, Channel<AuditLogEntry> auditChannel)
				: base(logger)
			{
				_repo = repo;
				_dbFactory = dbFactory;
				_logger = logger;
				_auditChannel = auditChannel;
			}

			public async Task<TaskDto?> GetByIdAsync(Guid id)
			{
				return await ExecuteWithLoggingAsync(
					async () =>
					{
						var e = await _repo.GetByIdAsync(id);
						if (e == null) return null;
						return MapToDto(e);
					},
					"GetTaskById",
					id
				);
			}

			public async Task<(IEnumerable<TaskDto> Items, int Total)> GetPagedAsync(int page, int pageSize, DateTime? from = null, DateTime? to = null)
			{
				return await ExecuteWithLoggingAsync(
					async () =>
					{
						var items = await _repo.GetPagedAsync(page, pageSize, from, to);
						var total = await _repo.GetCountAsync(from, to);
						return (items.Select(MapToDto), total);
					},
					"GetPagedTasks",
					$"Page:{page},Size:{pageSize}"
				);
			}

			public async Task<TaskDto> CreateAsync(CreateDto dto, byte[]? evidence = null, string? filename = null, string? contentType = null, string? createdBy = null)
			{
				return await ExecuteWithLoggingAsync(
					async () =>
					{
						var entity = new Tareas
						{
							Id = Guid.NewGuid(),
							Titulo = dto.Titulo,
							Descripcion = dto.Descripcion,
							Estatus = dto.Estatus,
							AsignadoA = dto.AsignadoA,
							Evidencia = evidence,
							Evidencia_fileName = filename,
							Evidencia_Content_Type = contentType,
							Creado = DateTime.UtcNow.AddHours(-6),
							Actualizado = null
						};

						using var conn = _dbFactory.CreateConnection();
						conn.Open();
						using var tx = conn.BeginTransaction();
						try
						{
							await _repo.CreateAsync(entity, tx);
							tx.Commit();
							return MapToDto(entity);
						}
						catch (Exception ex)
						{
							tx.Rollback();
							_logger.LogError(ex, "Error creando la tarea: {ErrorMessage}", ex.Message);
							throw;
						}
					},
					"CreateTask",
					null,
					createdBy
				);
			}

			public async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskDto dto, byte[]? evidence = null, string? filename = null, string? contentType = null, string? changedBy = null)
			{
				return await ExecuteWithLoggingAsync(
					async () =>
					{
						using var conn = _dbFactory.CreateConnection();
						conn.Open();
						using var tx = conn.BeginTransaction();
						try
						{
							var existing = await _repo.GetByIdAsync(id);
							if (existing == null) throw new KeyNotFoundException("Task not found");

							var previousStatus = existing.Estatus;

							// map updates
							existing.Titulo = dto.Title ?? existing.Titulo;
							existing.Descripcion = dto.Description ?? existing.Descripcion;
							existing.Estatus = dto.Status ?? existing.Estatus;
							existing.AsignadoA = dto.AssignedTo ?? existing.AsignadoA;
							if (evidence != null)
							{
								existing.Evidencia = evidence;
								existing.Evidencia_fileName = filename;
								existing.Evidencia_Content_Type = contentType;
							}
							existing.Actualizado = DateTime.UtcNow.AddHours(-6);

							await _repo.UpdateAsync(existing, tx);

							// enqueue audit log if status changed
							if (previousStatus != existing.Estatus)
							{
								var audit = new AuditLogEntry
								{
									TaskId = existing.Id,
									PreviousStatus = previousStatus,
									NewStatus = existing.Estatus,
									ChangedBy = changedBy ?? "system",
									Timestamp = DateTime.UtcNow
								};
								await _auditChannel.Writer.WriteAsync(audit);
							}

							tx.Commit();
							return MapToDto(existing);
						}
						catch (DBConcurrencyException ex)
						{
							tx.Rollback();
							_logger.LogWarning(ex, "Concurrency conflict updating task {TaskId}", id);
							throw;
						}
						catch (Exception ex)
						{
							tx.Rollback();
							_logger.LogError(ex, "Error updating task {TaskId}: {ErrorMessage}", id, ex.Message);
							throw;
						}
					},
					"UpdateTask",
					id,
					changedBy
				);
			}

			public async Task DeleteAsync(Guid id)
			{
				await ExecuteWithLoggingAsync(
					async () => await _repo.DeleteAsync(id),
					"DeleteTask",
					id
				);
			}

			public async Task SimulateConcurrentUpdatesAsync(Guid taskId, int concurrentUsers)
			{
				_logger.LogInformation("=== INICIANDO SIMULACIÓN DE CONCURRENCIA ===");
				_logger.LogInformation("Tarea ID: {TaskId}", taskId);
				_logger.LogInformation("Usuarios concurrentes: {ConcurrentUsers}", concurrentUsers);

				var tasks = new List<Task>();
				var successCount = 0;
				var conflictCount = 0;

				for (int i = 0; i < concurrentUsers; i++)
				{
					var userIndex = i + 1;
					tasks.Add(Task.Run(async () =>
					{
						try
						{
							_logger.LogInformation("Usuario {UserIndex} iniciando actualización", userIndex);

							// Obtener el registro actual
							var t = await _repo.GetByIdAsync(taskId);
							if (t == null)
							{
								_logger.LogWarning("Usuario {UserIndex}: Tarea no encontrada", userIndex);
								return;
							}

							// cambiar estado de forma alternada
							var newStatus = (t.Estatus == "pendiente") ? "en_proceso" : (t.Estatus == "en_proceso" ? "finalizada" : "pendiente");
							_logger.LogInformation("Usuario {UserIndex}: Cambiando de '{OldStatus}' a '{NewStatus}'", userIndex, t.Estatus, newStatus);

							var updateDto = new UpdateTaskDto { Status = newStatus, RowVersion = t.RowVersion };

							// Intentar actualizar
							await UpdateAsync(taskId, updateDto, changedBy: $"sim_user_{userIndex}");
							
							Interlocked.Increment(ref successCount);
							_logger.LogInformation("✅ Usuario {UserIndex}: Actualización exitosa", userIndex);
						}
						catch (DBConcurrencyException ex)
						{
							Interlocked.Increment(ref conflictCount);
							_logger.LogWarning("⚠️ Usuario {UserIndex}: Conflicto de concurrencia - {Message}", userIndex, ex.Message);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "❌ Usuario {UserIndex}: Error inesperado", userIndex);
						}
					}));
				}

				await Task.WhenAll(tasks);

				_logger.LogInformation("=== SIMULACIÓN COMPLETADA ===");
				_logger.LogInformation("Actualizaciones exitosas: {SuccessCount}", successCount);
				_logger.LogInformation("Conflictos de concurrencia: {ConflictCount}", conflictCount);
				_logger.LogInformation("Total de intentos: {TotalAttempts}", concurrentUsers);
			}

			private TaskDto MapToDto(Tareas e) => new TaskDto
			{
				Id = e.Id,
				Titulo = e.Titulo,
				Descripcion = e.Descripcion,
				Estatus = e.Estatus,
				AsignadoA = e.AsignadoA,
				EvidenciaFilename = e.Evidencia_fileName,
				Creado = e.Creado,
				Actualizado = e.Actualizado,
				RowVersion = e.RowVersion
			};
		}

		public class AuditLogEntry
		{
			public Guid TaskId { get; set; }
			public string? PreviousStatus { get; set; }
			public string? NewStatus { get; set; }
			public string? ChangedBy { get; set; }
			public DateTime Timestamp { get; set; }
		}
}
