using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Dtos
{
	public class TaskDto
	{
		public Guid Id { get; set; }
		public string Titulo { get; set; } = null!;
		public string? Descripcion { get; set; }
		public string Estatus { get; set; } = null!;
		public string? AsignadoA { get; set; }
		public string? EvidenciaFilename { get; set; }
		public DateTime Creado { get; set; }
		public DateTime? Actualizado { get; set; }
		public byte[]? RowVersion { get; set; }
	}
}
