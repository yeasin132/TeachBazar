using System.Threading.Tasks;

namespace TechBazar.Services
{
    public interface IMultiLanguageService
    {
        Task<string> GetValueAsync(string tableName, string columnName, long entityId, int languageId = 1);
        Task<bool> SetTranslationAsync(string tableName, string columnName, long entityId, int languageId, string translationValue);
        Task<System.Collections.Generic.Dictionary<string, string>> GetEntityTranslationsAsync(string tableName, long entityId, int languageId);
    }
}
