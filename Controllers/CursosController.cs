using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.ViewModels;

namespace PortalAcademico.Controllers
{
    [Authorize]
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CursosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Catalogo(FiltrosCursoViewModel filtros)
        {
            if (filtros.CreditosMin.HasValue && filtros.CreditosMin < 0)
            {
                ModelState.AddModelError(nameof(filtros.CreditosMin), 
                    "Los créditos mínimos no pueden ser negativos");
            }

            if (filtros.CreditosMax.HasValue && filtros.CreditosMax < 0)
            {
                ModelState.AddModelError(nameof(filtros.CreditosMax), 
                    "Los créditos máximos no pueden ser negativos");
            }

            if (filtros.HorarioInicio.HasValue && filtros.HorarioFin.HasValue 
                && filtros.HorarioFin <= filtros.HorarioInicio)
            {
                ModelState.AddModelError(nameof(filtros.HorarioFin), 
                    "El horario de fin debe ser posterior al horario de inicio");
            }

            var query = _context.Cursos
                .Include(c => c.Matriculas)
                .Where(c => c.Activo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtros.Nombre))
            {
                query = query.Where(c => c.Nombre.Contains(filtros.Nombre) 
                    || c.Codigo.Contains(filtros.Nombre));
            }

            if (filtros.CreditosMin.HasValue)
            {
                query = query.Where(c => c.Creditos >= filtros.CreditosMin.Value);
            }

            if (filtros.CreditosMax.HasValue)
            {
                query = query.Where(c => c.Creditos <= filtros.CreditosMax.Value);
            }

            if (filtros.HorarioInicio.HasValue)
            {
                query = query.Where(c => c.HorarioInicio >= filtros.HorarioInicio.Value);
            }

            if (filtros.HorarioFin.HasValue)
            {
                query = query.Where(c => c.HorarioFin <= filtros.HorarioFin.Value);
            }

            var cursos = await query
                .OrderBy(c => c.Codigo)
                .ToListAsync();

            var viewModel = new CatalogoViewModel
            {
                Cursos = cursos,
                Filtros = filtros
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos
                .Include(c => c.Matriculas.Where(m => 
                    m.Estado == Models.EstadoMatricula.Confirmada || 
                    m.Estado == Models.EstadoMatricula.Pendiente))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (curso == null)
            {
                return NotFound();
            }

            var userId = User.Identity?.Name;
            var yaMatriculado = await _context.Matriculas
                .AnyAsync(m => m.CursoId == id 
                    && m.UsuarioId == userId 
                    && (m.Estado == Models.EstadoMatricula.Confirmada 
                        || m.Estado == Models.EstadoMatricula.Pendiente));

            ViewBag.YaMatriculado = yaMatriculado;

            return View(curso);
        }
    }
}