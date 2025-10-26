using Dapper;
using System.Data;
using System.Text;
using Model;
using Microsoft.Extensions.Logging;
using Data;

namespace Repository
{
	public class TaskRepository : ITaskRepository
	{
		private readonly IDbConnectionFactory _db;
		private readonly ILogger<TaskRepository> _logger;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="db"></param>
		/// <param name="logger"></param>
		public TaskRepository(IDbConnectionFactory db, ILogger<TaskRepository> logger)
		{
			_db = db;
			_logger = logger;
		}
		/// <summary>
		/// Metodo que obtiene la tarea
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Tareas?> GetByIdAsync(Guid id)
		{
			Tareas? result = new();
			try
			{
				using var conn = _db.CreateConnection();
				string sql = @"SELECT *, Row_Version AS RowVersion FROM tasks WHERE Id = @Id";
				result = await conn.QuerySingleOrDefaultAsync<Tareas>(sql, new { Id = id });

				if (result != null)
				{
					_logger.LogInformation("Tarea encontrada: {TaskId}, RowVersion: {RowVersion}",
						result.Id, Convert.ToBase64String(result.RowVersion ?? new byte[0]));
				}
				else
				{
					_logger.LogWarning("Tarea no encontrada: {TaskId}", id);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Error al obtener la tarea " + ex.Message);
			}
			return result;
		}
		/// <summary>
		/// Metodo para obtener el paginado
		/// </summary>
		/// <param name="page"></param>
		/// <param name="pageSize"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		public async Task<IEnumerable<Tareas>?> GetPagedAsync(int page, int pageSize, DateTime? from = null, DateTime? to = null)
		{
			try
			{
				using var conn = _db.CreateConnection();
				var sb = new StringBuilder(@"
            SELECT *, Row_Version AS RowVersion 
            FROM tasks 
            WHERE 1=1
        ");

				if (from.HasValue)
					sb.Append(" AND Creado >= @From ");
				if (to.HasValue)
					sb.Append(" AND Creado <= @To ");

				sb.Append(" ORDER BY Creado DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");

				var parameters = new
				{
					From = from,
					To = to,
					Offset = (page - 1) * pageSize,
					PageSize = pageSize
				};

				return await conn.QueryAsync<Tareas>(sb.ToString(), parameters);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al obtener el paginado.");
				return null;
			}
		}

		/// <summary>
		/// Metodo que contabiliza las tareas
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public async Task<int> GetCountAsync(DateTime? from = null, DateTime? to = null)
		{
			try
			{
				using var conn = _db.CreateConnection();
				var sb = new StringBuilder("SELECT COUNT(1) FROM tasks WHERE 1=1 ");
				if (from.HasValue) sb.Append(" AND Creado >= @From ");
				if (to.HasValue) sb.Append(" AND Creado <= @To ");
				return await conn.ExecuteScalarAsync<int>(sb.ToString(), new { From = from, To = to });
			}
			catch (Exception ex)
			{
				_logger.LogError("Error al contabilizar las tareas " + ex.Message);
				throw new InvalidOperationException("Error al contabilizar las tareas " + ex.Message);
			}
		}
		/// <summary>
		/// Metodo para crear una tarea
		/// </summary>
		/// <param name="task"></param>
		/// <param name="transaction"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public async Task CreateAsync(Tareas task, IDbTransaction? transaction = null)
		{
			try
			{
				var sql = @"INSERT INTO tasks (Id, Titulo, Decripcion, Estatus, AsignadoA, Evidencia, Evidencia_fileName, 
						Evidencia_Content_Type, Creado, Actualizado) VALUES (@Id, @Titulo, @Descripcion, @Estatus, @AsignadoA, 
						@Evidencia, @Evidencia_fileName, @Evidencia_Content_Type, @Creado, @Actualizado);";
				if (transaction != null)
				{
					await transaction.Connection.ExecuteAsync(sql, task, transaction);
				}
				else
				{
					using var conn = _db.CreateConnection();
					await conn.ExecuteAsync(sql, task);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Error al crear la tarea " + ex.Message);
				throw new InvalidOperationException("Error al crear la tarea " + ex.Message);
			}
		}
		/// <summary>
		/// Metodo para actualizar Tarea
		/// </summary>
		/// <param name="task"></param>
		/// <param name="transaction"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="DBConcurrencyException"></exception>
		public async Task UpdateAsync(Tareas task, IDbTransaction? transaction = null)
		{
			try
			{
				_logger.LogInformation("Actualizando tarea {TaskId} con RowVersion: {RowVersion}",
					task.Id, task.RowVersion != null ? Convert.ToBase64String(task.RowVersion) : "NULL");

				// Verificar que RowVersion no sea null
				if (task.RowVersion == null)
				{
					_logger.LogError("RowVersion es NULL para tarea {TaskId}", task.Id);
					throw new ArgumentException("RowVersion no puede ser NULL para actualización");
				}

				// Usar rowversion para concurrency: WHERE id=@Id AND row_version=@RowVersion
				var sql = @"UPDATE tasks SET Titulo = @Titulo, Decripcion = @Descripcion, Estatus = @Estatus, 
							AsignadoA = @AsignadoA, Evidencia = @Evidencia, Evidencia_filename = @Evidencia_fileName,
							Evidencia_Content_Type = @Evidencia_Content_Type, Actualizado = @Actualizado WHERE Id = @Id AND 
							Row_Version = @RowVersion; SELECT @@ROWCOUNT;";

				int affected = 0;
				if (transaction != null)
				{
					affected = await transaction.Connection.ExecuteScalarAsync<int>(sql, task, transaction);
				}
				else
				{
					using var conn = _db.CreateConnection();
					affected = await conn.ExecuteScalarAsync<int>(sql, task);
				}

				_logger.LogInformation("Filas afectadas: {AffectedRows}", affected);

				if (affected == 0)
				{
					_logger.LogWarning("No se pudo actualizar la tarea {TaskId}. Posible conflicto de concurrencia.", task.Id);
					throw new DBConcurrencyException("Concurrency conflict on update.");
				}

				_logger.LogInformation("Actualización exitosa: {AffectedRows} fila(s) afectada(s)", affected);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al actualizar tarea {TaskId}", task.Id);
				throw;
			}
		}
		/// <summary>
		/// Metodo para eliminar Tarea
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task DeleteAsync(Guid id)
		{
			using var conn = _db.CreateConnection();
			var sql = "DELETE FROM tasks WHERE Id = @Id";
			await conn.ExecuteAsync(sql, new { Id = id });
		}

		// Método de prueba para actualizar sin RowVersion (solo para debug)
		public async Task UpdateWithoutConcurrencyAsync(Tareas task, IDbTransaction? transaction = null)
		{
			try
			{
				_logger.LogInformation("=== ACTUALIZACIÓN SIN CONCURRENCIA (DEBUG) ===");
				_logger.LogInformation("Tarea ID: {TaskId}", task.Id);
				_logger.LogInformation("Título: {Titulo}", task.Titulo);

				var sql = @"UPDATE tasks SET Titulo = @Titulo, Descripcion = @Descripcion, Estatus = @Estatus, 
								AsignadoA = @AsignadoA, Evidencia = @Evidencia, Evidencia_filename = @Evidencia_fileName,
								Evidencia_Content_Type = @Evidencia_Content_Type, Actualizado = @Actualizado WHERE Id = @Id; 
								SELECT @@ROWCOUNT;";

				_logger.LogInformation("Ejecutando SQL sin RowVersion: {Sql}", sql);

				int affected = 0;
				if (transaction != null)
				{
					affected = await transaction.Connection.ExecuteScalarAsync<int>(sql, task, transaction);
				}
				else
				{
					using var conn = _db.CreateConnection();
					affected = await conn.ExecuteScalarAsync<int>(sql, task);
				}

				_logger.LogInformation("Filas afectadas (sin concurrencia): {AffectedRows}", affected);

				if (affected == 0)
				{
					_logger.LogError("❌ NO SE PUDO ACTUALIZAR SIN CONCURRENCIA: 0 filas afectadas");
					_logger.LogError("Esto indica un problema con los nombres de columnas o la estructura de la tabla");
				}
				else
				{
					_logger.LogInformation("✅ ACTUALIZACIÓN SIN CONCURRENCIA EXITOSA: {AffectedRows} fila(s) afectada(s)", affected);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ ERROR al actualizar sin concurrencia tarea {TaskId}", task.Id);
				throw;
			}
		}
	}
}
