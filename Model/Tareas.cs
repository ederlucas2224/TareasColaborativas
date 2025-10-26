using System.ComponentModel.DataAnnotations;

namespace Model
{
	public class Tareas
	{
		public Guid Id { get; set; }
		public string Titulo { get; set; } = null!;
		public string? Descripcion { get; set; }
		public string Estatus { get; set; } = null!;
		public string? AsignadoA { get; set; }
		public byte[]? Evidencia { get; set; }
		public string? Evidencia_fileName { get; set; }
		public string? Evidencia_Content_Type { get; set; }
		public DateTime Creado { get; set; }
		public DateTime? Actualizado { get; set; }
		public byte[]? RowVersion { get; set; }
	}
}
