using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechBazar.Data;
using TechBazar.Models;

namespace TechBazar.Services
{
    public class MultiLanguageService : IMultiLanguageService
    {
        private readonly ApplicationDbContext _context;

        public MultiLanguageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetValueAsync(string tableName, string columnName, long entityId, int languageId = 1)
        {
            var translation = await _context.MultiLanguageTranslators
                .FirstOrDefaultAsync(t => t.TableName == tableName &&
                                        t.ColumnName == columnName &&
                                        t.EntityId == entityId &&
                                        t.LanguageId == languageId);

            return translation?.TranslationValue ?? string.Empty;
        }

        public async Task<bool> SetTranslationAsync(string tableName, string columnName, long entityId, int languageId, string translationValue)
        {
            var existingTranslation = await _context.MultiLanguageTranslators
                .FirstOrDefaultAsync(t => t.TableName == tableName &&
                                        t.ColumnName == columnName &&
                                        t.EntityId == entityId &&
                                        t.LanguageId == languageId);

            if (existingTranslation != null)
            {
                existingTranslation.TranslationValue = translationValue;
            }
            else
            {
                var newTranslation = new MultiLanguageTranslator
                {
                    TableName = tableName,
                    ColumnName = columnName,
                    EntityId = entityId,
                    LanguageId = languageId,
                    TranslationValue = translationValue
                };
                _context.MultiLanguageTranslators.Add(newTranslation);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<string, string>> GetEntityTranslationsAsync(string tableName, long entityId, int languageId)
        {
            var translations = await _context.MultiLanguageTranslators
                .Where(t => t.TableName == tableName &&
                           t.EntityId == entityId &&
                           t.LanguageId == languageId)
                .ToDictionaryAsync(t => t.ColumnName, t => t.TranslationValue);

            return translations;
        }
    }
}
