using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
	public class Logs
	{
		public Guid TaskId { get; set; }
		public string? PreviousStatus { get; set; }
		public string? NewStatus { get; set; }
		public string? ChangedBy { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
