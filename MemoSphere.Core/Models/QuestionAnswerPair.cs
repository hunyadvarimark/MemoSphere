using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class QuestionAnswerPair
    {
        public string Question { get; set; }
        public string Answer { get; set; }

        public QuestionAnswerPair()
        {
            Question = string.Empty;
            Answer = string.Empty;
        }
    }
}
