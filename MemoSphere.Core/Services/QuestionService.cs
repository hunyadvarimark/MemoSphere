using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Core.Enums;
using Core.Models;

namespace Core.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IQuestionGeneratorService _questionGeneratorService;

        public QuestionService(IUnitOfWork unitofWork, IQuestionGeneratorService questionGeneratorService)
        {
            _unitOfWork = unitofWork;
            _questionGeneratorService = questionGeneratorService;
        }

        public async Task DeleteQuestionAsync(int id)
        {
            var questionToDelete = await _unitOfWork.Questions.GetByIdAsync(id);
            if (questionToDelete == null)
            {
                throw new ArgumentException("A kérdés nem található.", nameof(id));
            }
            _unitOfWork.Questions.Remove(questionToDelete);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> GenerateAndSaveQuestionsAsync(int noteId)
        {
            var note = await _unitOfWork.Notes.GetByIdAsync(noteId);
            if (note == null)
            {
                throw new ArgumentException("A jegyzet nem található.", nameof(noteId));
            }

            var chunks = await _unitOfWork.NoteChunks.GetNoteChunksByNoteIdAsync(noteId);
            if (!chunks.Any())
            {
                Console.WriteLine($"Nincsenek 'chunks' a {noteId} azonosítójú jegyzethez.");
                return false;
            }

            foreach (var chunk in chunks)
            {
                var qaPairs = await _questionGeneratorService.GenerateQuestionsAsync(
                    chunk.Content,
                    "gemini-2.5-flash");

                if (qaPairs == null || !qaPairs.Any())
                {
                    Console.WriteLine("Az API nem generált kérdéseket.");
                    continue;
                }

                foreach (var pair in qaPairs)
                {
                    var wrongAnswers = await _questionGeneratorService.GenerateWrongAnswersAsync(
                        pair.Answer,
                        chunk.Content,
                        "gemini-2.5-flash");

                    if (wrongAnswers == null || !wrongAnswers.Any())
                    {
                        Console.WriteLine("Az API nem generált rossz válaszokat.");
                        continue;
                    }

                    var question = new Question
                    {
                        TopicId = note.TopicId,
                        Text = pair.Question,
                        QuestionType = QuestionType.MultipleChoice,
                        SourceNoteId = noteId,
                        Answers = new List<Answer>()
                    };

                    var correctAnswer = new Answer
                    {
                        Text = pair.Answer,
                        IsCorrect = true
                    };
                    question.Answers.Add(correctAnswer);

                    foreach (var wrongAnswerText in wrongAnswers)
                    {
                        var wrongAnswer = new Answer
                        {
                            Text = wrongAnswerText,
                            IsCorrect = false
                        };
                        question.Answers.Add(wrongAnswer);
                    }

                    await _unitOfWork.Questions.AddAsync(question);
                }
            }
            // Az összes változás mentése a végén
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Question>> GetQuestionsByTopicIdAsync(int topicId)
        {
            if (topicId <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(topicId));
            }
            return await _unitOfWork.Questions.GetQuestionsByTopicIdAsync(topicId);
        }
    }
}