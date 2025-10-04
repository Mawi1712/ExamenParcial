using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;

namespace PortalAcademico.Controllers
{
    [Authorize(Roles = "Coordinador")]
    public class CoordinadorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoordinadorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Matriculas(int? id)
        {
            if (id == null) return NotFound();

            var curso = await _context.Cursos
                .FirstOrDefaultAsync(c => c.Id == id);

            if (curso == null) return NotFound();

            var matriculas = await _context.Matriculas
                .Include(m => m.Curso)
                .Where(m => m.CursoId == id)
                .OrderByDescending(m => m.FechaRegistro)
                .ToListAsync();

            ViewBag.Curso = curso;
            return View(matriculas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarMatricula(int id)
        {
            var matricula = await _context.Matriculas.FindAsync(id);
            if (matricula == null) return NotFound();

            matricula.Estado = EstadoMatricula.Confirmada;
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Matrícula confirmada exitosamente";
            return RedirectToAction(nameof(Matriculas), new { id = matricula.CursoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarMatricula(int id)
        {
            var matricula = await _context.Matriculas.FindAsync(id);
            if (matricula == null) return NotFound();

            matricula.Estado = EstadoMatricula.Cancelada;
            await _context.SaveChangesAsync();
            
            TempData["Warning"] = "Matrícula cancelada";
            return RedirectToAction(nameof(Matriculas), new { id = matricula.CursoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesactivarCurso(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();

            curso.Activo = false;
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Curso desactivado exitosamente";
            return RedirectToAction("Index", "AdminCursos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivarCurso(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();

            curso.Activo = true;
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Curso activado exitosamente";
            return RedirectToAction("Index", "AdminCursos");
        }
    }
}