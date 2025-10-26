using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Service
{
    public abstract class BaseService
    {
        protected readonly ILogger _logger;

        protected BaseService(ILogger logger)
        {
            _logger = logger;
        }

        protected async Task<T> ExecuteWithLoggingAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            object? entityId = null,
            string? userId = null)
        {
            try
            {
                _logger.LogInformation("Iniciando {Operation} {EntityId} {UserId}", 
                    operationName, entityId, userId);

                var result = await operation();
                
                _logger.LogInformation("Completado exitosamente {Operation} {EntityId} {UserId}", 
                    operationName, entityId, userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en {Operation} {EntityId} {UserId}: {ErrorMessage}", 
                    operationName, entityId, userId, ex.Message);
                throw;
            }
        }

        protected async Task ExecuteWithLoggingAsync(
            Func<Task> operation,
            string operationName,
            object? entityId = null,
            string? userId = null)
        {
            try
            {
                _logger.LogInformation("Iniciando {Operation} {EntityId} {UserId}", 
                    operationName, entityId, userId);

                await operation();
                
                _logger.LogInformation("Completado exitosamente {Operation} {EntityId} {UserId}", 
                    operationName, entityId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en {Operation} {EntityId} {UserId}: {ErrorMessage}", 
                    operationName, entityId, userId, ex.Message);
                throw;
            }
        }
    }
}
