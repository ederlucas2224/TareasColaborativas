using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Dtos
{
	public class CreateDto
	{
		[Required]
		[MaxLength(250)]
		public string Titulo { get; set; } = null!;

		public string? Descripcion { get; set; }

		public string Estatus { get; set; } = "pendiente";

		[MaxLength(200)]
		public string? AsignadoA { get; set; }
	}
}
