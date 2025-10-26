using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Dtos
{
	public class UpdateTaskDto
	{
		[MaxLength(250)]
		public string? Title { get; set; }

		public string? Description { get; set; }

		[RegularExpression("pendiente|en_proceso|finalizada",
			ErrorMessage = "El estado debe ser 'pendiente', 'en_proceso' o 'finalizada'.")]
		public string? Status { get; set; }

		[MaxLength(200)]
		public string? AssignedTo { get; set; }

		public byte[]? RowVersion { get; set; }
	}
}
