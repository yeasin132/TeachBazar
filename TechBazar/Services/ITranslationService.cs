using System.Threading.Tasks;

namespace TechBazar.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string text, string targetLanguage);
        Task<string> GetTranslationAsync(string text, string targetLanguage);
    }
}
