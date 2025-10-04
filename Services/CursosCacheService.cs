using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortalAcademico.Services
{
    public interface ICursosCacheService
    {
        Task<List<Curso>> ObtenerCursosActivosAsync();
        Task InvalidarCacheAsync();
    }

    public class CursosCacheService : ICursosCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CursosCacheService> _logger;
        private const string CACHE_KEY = "cursos_activos";
        private const int CACHE_DURATION_SECONDS = 60;

        public CursosCacheService(
            IDistributedCache cache,
            ApplicationDbContext context,
            ILogger<CursosCacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Curso>> ObtenerCursosActivosAsync()
        {
            try
            {
                // Intent: devolver cursos desde caché si existe
                var cached = await _cache.GetStringAsync(CACHE_KEY);
                if (!string.IsNullOrEmpty(cached))
                {
                    _logger.LogDebug("Cursos recuperados desde caché");
                    var opcionesDeserializacion = new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.IgnoreCycles,
                        MaxDepth = 128,
                        WriteIndented = false
                    };
                    return JsonSerializer.Deserialize<List<Curso>>(cached, opcionesDeserializacion) ?? new List<Curso>();
                }

                // Si no hay caché, cargar desde la base de datos
                // Usamos AsNoTracking para lecturas y evitar problemas de seguimiento de EF
                var cursos = await _context.Cursos
                    .AsNoTracking()
                    .Where(c => c.Activo)
                    .ToListAsync();

                // Serializar y almacenar en caché
                var opciones = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CACHE_DURATION_SECONDS)
                };

                var opcionesSerializacion = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    MaxDepth = 128,
                    WriteIndented = false
                };

                var cursosSerializados = JsonSerializer.Serialize(cursos, opcionesSerializacion);
                await _cache.SetStringAsync(CACHE_KEY, cursosSerializados, opciones);

                _logger.LogInformation($"Cursos guardados en caché por {CACHE_DURATION_SECONDS} segundos");

                return cursos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cursos activos (se consultará directamente BD)");
                // En caso de error de caché, intentar retornar desde BD directamente
                return await _context.Cursos
                    .AsNoTracking()
                    .Where(c => c.Activo)
                    .ToListAsync();
            }
        }

        public async Task InvalidarCacheAsync()
        {
            await _cache.RemoveAsync(CACHE_KEY);
            _logger.LogInformation("Caché de cursos invalidado");
        }
    }
}
