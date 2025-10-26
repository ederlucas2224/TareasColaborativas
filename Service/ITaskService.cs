using Model.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
	public interface ITaskService
	{
		Task<TaskDto?> GetByIdAsync(Guid id);
		Task<(IEnumerable<TaskDto> Items, int Total)> GetPagedAsync(int page, int pageSize, DateTime? from = null, DateTime? to = null);
		Task<TaskDto> CreateAsync(CreateDto dto, byte[]? evidence = null, string? filename = null, string? contentType = null, string? createdBy = null);
		Task<TaskDto> UpdateAsync(Guid id, UpdateTaskDto dto, byte[]? evidence = null, string? filename = null, string? contentType = null, string? changedBy = null);
		Task DeleteAsync(Guid id);
		// Simulación de concurrencia
		Task SimulateConcurrentUpdatesAsync(Guid taskId, int concurrentUsers);
	}
}
