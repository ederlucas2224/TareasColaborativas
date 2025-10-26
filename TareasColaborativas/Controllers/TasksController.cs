using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Model.Dtos;
using Service;
using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;

namespace TareasColaborativas.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class TasksController : ControllerBase
	{
		private readonly ITaskService _service;
		public TasksController(ITaskService service)
		{
			_service = service;
		}

		[HttpGet("{id:guid}")]
		public async Task<IActionResult> GetById(Guid id, [FromQuery] bool includeRowVersion = false)
		{
			try
			{
				var t = await _service.GetByIdAsync(id);
				if (t == null) return NotFound();

				// Si se solicita RowVersion, incluirlo en la respuesta
				if (includeRowVersion)
				{
					var response = new
					{
						t.Id,
						t.Titulo,
						t.Descripcion,
						t.Estatus,
						t.AsignadoA,
						t.EvidenciaFilename,
						t.Creado,
						t.Actualizado,
						RowVersion = t.RowVersion != null ? Convert.ToBase64String(t.RowVersion) : null
					};
					return Ok(response);
				}

				return Ok(t);
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { message = "Tarea no encontrada", error = ex.Message });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? from = null, [FromQuery] string? to = null)
		{
			DateTime? fromDate = null;
			DateTime? toDate = null;

			if (!string.IsNullOrEmpty(from))
				fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);

			if (!string.IsNullOrEmpty(to))
				toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);

			var (items, total) = await _service.GetPagedAsync(page, pageSize, fromDate, toDate);
			return Ok(new { Items = items, Total = total });
		}

		[HttpPost]
		[RequestSizeLimit(50_000_000)]
		public async Task<IActionResult> Create([FromForm] CreateDto dto, IFormFile? evidence)
		{
			byte[]? content = null;
			string? filename = null;
			string? contentType = null;
			if (evidence != null)
			{
				using var ms = new MemoryStream();
				await evidence.CopyToAsync(ms);
				content = ms.ToArray();
				filename = evidence.FileName;
				contentType = evidence.ContentType;
			}

			if (dto.Estatus != "pendiente" && dto.Estatus != "en proceso" && dto.Estatus != "finalizado")
			{
				dto.Estatus = "pendiente";
			}

			var created = await _service.CreateAsync(dto, content, filename, contentType, createdBy: "api_user");
			return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
		}

		[HttpPut("{id:guid}")]
		[RequestSizeLimit(50_000_000)]
		public async Task<IActionResult> Update(Guid id, [FromForm] UpdateTaskDto dto, IFormFile? evidence)
		{
			try
			{
				byte[]? content = null;
				string? filename = null;
				string? contentType = null;
				if (evidence != null)
				{
					using var ms = new System.IO.MemoryStream();
					await evidence.CopyToAsync(ms);
					content = ms.ToArray();
					filename = evidence.FileName;
					contentType = evidence.ContentType;
				}

				var updated = await _service.UpdateAsync(id, dto, content, filename, contentType, changedBy: "api_user");
				return Ok(updated);
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
			catch (DBConcurrencyException)
			{
				return Conflict(new { message = "Concurrency conflict: record has been modified by another user." });
			}
			catch (Exception)
			{
				return StatusCode(500);
			}
		}

		[HttpDelete("{id:guid}")]
		public async Task<IActionResult> Delete(Guid id)
		{
			await _service.DeleteAsync(id);
			return NoContent();
		}


		// Endpoint para testing de concurrencia (solo en Development)
		[HttpPost("{id:guid}/test-concurrency")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IActionResult> TestConcurrency(Guid id, [FromQuery] int concurrentUsers = 3)
		{
			// Solo permitir en Development
			if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
			{
				return NotFound();
			}

			await _service.SimulateConcurrentUpdatesAsync(id, concurrentUsers);
			return Ok(new { message = $"Simulación de {concurrentUsers} usuarios concurrentes completada" });
		}

	}
}
