using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;

namespace PortalAcademico.Controllers
{
    [Authorize]
    public class MatriculasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MatriculasController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inscribirse(int cursoId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Debe estar autenticado para inscribirse en un curso.";
                return RedirectToAction("Login", "Account");
            }

            var curso = await _context.Cursos
                .Include(c => c.Matriculas.Where(m => 
                    m.Estado == EstadoMatricula.Confirmada || 
                    m.Estado == EstadoMatricula.Pendiente))
                .FirstOrDefaultAsync(c => c.Id == cursoId);

            if (curso == null)
            {
                TempData["Error"] = "El curso solicitado no existe.";
                return RedirectToAction("Catalogo", "Cursos");
            }

            if (!curso.Activo)
            {
                TempData["Error"] = $"El curso {curso.Codigo} no está activo actualmente.";
                return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
            }

            var yaMatriculado = await _context.Matriculas
                .AnyAsync(m => m.CursoId == cursoId 
                    && m.UsuarioId == user.Id 
                    && (m.Estado == EstadoMatricula.Confirmada 
                        || m.Estado == EstadoMatricula.Pendiente));

            if (yaMatriculado)
            {
                TempData["Error"] = $"Ya estás matriculado en el curso {curso.Codigo} - {curso.Nombre}.";
                return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
            }

            var matriculasActivas = curso.Matriculas
                .Count(m => m.Estado == EstadoMatricula.Confirmada || 
                            m.Estado == EstadoMatricula.Pendiente);

            if (matriculasActivas >= curso.CupoMaximo)
            {
                TempData["Error"] = $"El curso {curso.Codigo} - {curso.Nombre} no tiene cupos disponibles. " +
                    $"Cupo máximo: {curso.CupoMaximo}, Matriculados: {matriculasActivas}.";
                return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
            }

            var cursosMatriculados = await _context.Matriculas
                .Include(m => m.Curso)
                .Where(m => m.UsuarioId == user.Id 
                    && (m.Estado == EstadoMatricula.Confirmada 
                        || m.Estado == EstadoMatricula.Pendiente))
                .Select(m => m.Curso)
                .ToListAsync();

            foreach (var cursoExistente in cursosMatriculados)
            {
                bool haySolapamiento = HayConflictoHorario(
                    curso.HorarioInicio, curso.HorarioFin,
                    cursoExistente.HorarioInicio, cursoExistente.HorarioFin);

                if (haySolapamiento)
                {
                    TempData["Error"] = $"El horario del curso {curso.Codigo} " +
                        $"({curso.HorarioInicio:hh\\:mm} - {curso.HorarioFin:hh\\:mm}) " +
                        $"se solapa con el curso {cursoExistente.Codigo} - {cursoExistente.Nombre} " +
                        $"({cursoExistente.HorarioInicio:hh\\:mm} - {cursoExistente.HorarioFin:hh\\:mm}).";
                    return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
                }
            }

            var matricula = new Matricula
            {
                CursoId = cursoId,
                UsuarioId = user.Id,
                FechaRegistro = DateTime.UtcNow,
                Estado = EstadoMatricula.Pendiente
            };

            _context.Matriculas.Add(matricula);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"¡Te has inscrito exitosamente en {curso.Codigo} - {curso.Nombre}! " +
                $"Tu matrícula está en estado PENDIENTE y será revisada por el coordinador académico.";

            return RedirectToAction(nameof(MisCursos));
        }

        private bool HayConflictoHorario(
            TimeSpan inicio1, TimeSpan fin1,
            TimeSpan inicio2, TimeSpan fin2)
        {
            return inicio1 < fin2 && inicio2 < fin1;
        }

        public async Task<IActionResult> MisCursos()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var matriculas = await _context.Matriculas
                .Include(m => m.Curso)
                .Where(m => m.UsuarioId == user.Id)
                .OrderByDescending(m => m.FechaRegistro)
                .ToListAsync();

            return View(matriculas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Debe estar autenticado.";
                return RedirectToAction("Login", "Account");
            }

            var matricula = await _context.Matriculas
                .Include(m => m.Curso)
                .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == user.Id);

            if (matricula == null)
            {
                TempData["Error"] = "Matrícula no encontrada.";
                return RedirectToAction(nameof(MisCursos));
            }

            if (matricula.Estado == EstadoMatricula.Cancelada)
            {
                TempData["Warning"] = "Esta matrícula ya fue cancelada anteriormente.";
                return RedirectToAction(nameof(MisCursos));
            }

            matricula.Estado = EstadoMatricula.Cancelada;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Has cancelado tu matrícula en {matricula.Curso.Codigo} - {matricula.Curso.Nombre}.";

            return RedirectToAction(nameof(MisCursos));
        }
    }
}