using Xunit;

namespace PortalAcademico.Tests
{
    public class MatriculasValidationsTests
    {
        [Theory]
        [InlineData("08:00", "10:00", "09:00", "11:00", true)]  
        [InlineData("08:00", "10:00", "10:00", "12:00", false)] 
        [InlineData("08:00", "10:00", "10:01", "12:00", false)] 
        [InlineData("14:00", "16:00", "08:00", "10:00", false)] 
        public void HayConflictoHorario_DebeDetectarSolapamientos(
            string inicio1, string fin1, 
            string inicio2, string fin2, 
            bool esperaSolapamiento)
        {
            var ts1Inicio = TimeSpan.Parse(inicio1);
            var ts1Fin = TimeSpan.Parse(fin1);
            var ts2Inicio = TimeSpan.Parse(inicio2);
            var ts2Fin = TimeSpan.Parse(fin2);

            bool resultado = ts1Inicio < ts2Fin && ts2Inicio < ts1Fin;

            Assert.Equal(esperaSolapamiento, resultado);
        }
    }
}