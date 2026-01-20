using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechBazar.Models
{
    public class MultiLanguageTranslator
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int LanguageId { get; set; } // 1=English (default), 2=Bangla, 3=Arabic

        [Required]
        [MaxLength(100)]
        public required string TableName { get; set; } // e.g., 'Product', 'Category'

        [Required]
        [MaxLength(100)]
        public required string ColumnName { get; set; } // e.g., 'Name', 'Description'

        [Required]
        public long EntityId { get; set; } // Id of the entity

        public required string TranslationValue { get; set; } // translated text
    }
}
