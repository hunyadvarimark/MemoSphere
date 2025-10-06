using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Note
    {
    
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; }

        public int TopicId { get; set; }
        public Topic Topic { get; set; }
    }
}
