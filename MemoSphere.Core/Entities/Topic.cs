using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Topic
    {
    
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }

        public ICollection<Note> Notes { get; set; }
        public ICollection<Question> Questions { get; set; }

    }
}
