using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;
using PortalAcademico.Services;

namespace PortalAcademico.Controllers
{
    [Authorize(Roles = "Coordinador")]
    public class AdminCursosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICursosCacheService _cursosCacheService;

        public AdminCursosController(
            ApplicationDbContext context,
            ICursosCacheService cursosCacheService)
        {
            _context = context;
            _cursosCacheService = cursosCacheService;
        }

        public async Task<IActionResult> Index()
        {
            var cursos = await _context.Cursos
                .Include(c => c.Matriculas)
                .OrderBy(c => c.Codigo)
                .ToListAsync();
            
            return View(cursos);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Curso curso)
        {
            if (curso.HorarioFin <= curso.HorarioInicio)
            {
                ModelState.AddModelError(nameof(curso.HorarioFin), 
                    "El horario de fin debe ser posterior al horario de inicio");
            }

            if (curso.Creditos <= 0)
            {
                ModelState.AddModelError(nameof(curso.Creditos), 
                    "Los créditos deben ser mayores a 0");
            }

            if (ModelState.IsValid)
            {
                _context.Add(curso);
                await _context.SaveChangesAsync();
                
                await _cursosCacheService.InvalidarCacheAsync();
                
                TempData["Success"] = $"Curso {curso.Codigo} - {curso.Nombre} creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            
            return View(curso);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null)
            {
                return NotFound();
            }
            
            return View(curso);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Curso curso)
        {
            if (id != curso.Id)
            {
                return NotFound();
            }

            if (curso.HorarioFin <= curso.HorarioInicio)
            {
                ModelState.AddModelError(nameof(curso.HorarioFin), 
                    "El horario de fin debe ser posterior al horario de inicio");
            }

            if (curso.Creditos <= 0)
            {
                ModelState.AddModelError(nameof(curso.Creditos), 
                    "Los créditos deben ser mayores a 0");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(curso);
                    await _context.SaveChangesAsync();
                    
                    // ⭐ Invalidar caché
                    await _cursosCacheService.InvalidarCacheAsync();
                    
                    TempData["Success"] = $"Curso {curso.Codigo} actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CursoExists(curso.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            return View(curso);
        }

        private bool CursoExists(int id)
        {
            return _context.Cursos.Any(e => e.Id == id);
        }
    }
}