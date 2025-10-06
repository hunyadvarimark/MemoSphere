
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Subject
    {
    
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }

        public ICollection<Topic> Topics { get; set; }
    }
}
