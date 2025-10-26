using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
	public interface ITaskRepository
	{
		Task<Tareas?> GetByIdAsync(Guid id);
		Task<IEnumerable<Tareas>> GetPagedAsync(int page, int pageSize, DateTime? from = null, DateTime? to = null);
		Task<int> GetCountAsync(DateTime? from = null, DateTime? to = null);
		Task CreateAsync(Tareas task, IDbTransaction? transaction = null);
		Task UpdateAsync(Tareas task, IDbTransaction? transaction = null);
		Task DeleteAsync(Guid id);
	}
}
