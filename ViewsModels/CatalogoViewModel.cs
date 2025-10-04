namespace PortalAcademico.ViewModels
{
    public class CatalogoViewModel
    {
        public List<Models.Curso> Cursos { get; set; } = new();
        public FiltrosCursoViewModel Filtros { get; set; } = new();
    }

    public class FiltrosCursoViewModel
    {
        public string? Nombre { get; set; }
        public int? CreditosMin { get; set; }
        public int? CreditosMax { get; set; }
        public TimeSpan? HorarioInicio { get; set; }
        public TimeSpan? HorarioFin { get; set; }
    }
}