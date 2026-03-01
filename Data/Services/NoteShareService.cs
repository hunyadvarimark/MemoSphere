using Core.Entities;
using Core.Interfaces.Services;
using Core.Models;
using System.Reflection;
using System.Text.Json;

namespace MemoSphere.Data.Services
{
    public class NoteShareService : INoteShareService
    {
        private readonly INoteService _noteService;
        private readonly IQuestionService _questionService;
        private readonly IAuthService _authService;

        public NoteShareService(INoteService noteService, IQuestionService questionService, IAuthService authService)
        {
            _noteService = noteService;
            _questionService = questionService;
            _authService = authService;
        }
        
        
        public async Task ExportNoteToFileAsync(int noteId, string filePath)
        {
            var note = await _noteService.GetNoteByIdAsync(noteId);
            if (note == null) throw new Exception("A jegyzet nem található, vagy nincs jogosultság a megtekintéséhez.");

            var questions = await _questionService.GetQuestionsForNoteAsync(noteId);

            var exportDto = new NoteExportDto
            {
                Title = note.Title,
                Content = note.Content,
                Questions = questions.Select(q => new QuestionExportDto
                {
                    Text = q.Text,
                    QuestionType = q.QuestionType,
                    Answers = q.Answers?.Select(a => new AnswerExportDto
                    {
                        Text = a.Text,
                        IsCorrect = a.IsCorrect
                    }).ToList() ?? new()
                }).ToList()
            };

            var optinons = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(exportDto, optinons);
            await File.WriteAllTextAsync(filePath, jsonString);

        }

        public async Task ImportNoteFromFileAsync(string filePath, int targetTopicId)
        {
            var userId = _authService.GetCurrentUserId();
            if (userId == Guid.Empty) throw new Exception("Nincs bejelentkezve, nem lehet jegyzetet importálni.");

            if(!File.Exists(filePath)) throw new FileNotFoundException("A megadott fájl nem található.");

            string jsonString = await File.ReadAllTextAsync(filePath);
            var importDto = JsonSerializer.Deserialize<NoteExportDto>(jsonString);

            if(importDto == null) throw new Exception("A fájl formátuma érvénytelen, nem lehet importálni.");

            var newNote = new Note
            {
                Title = importDto.Title,
                Content = importDto.Content,
                TopicId = targetTopicId,
                UserId = userId
            };

            var savedNote = await _noteService.AddNoteAsync(newNote);

            if (importDto.Questions != null && importDto.Questions.Any())
            {
                var questionsToSave = importDto.Questions.Select(qDto => new Question
                {
                    Text = qDto.Text,
                    QuestionType = qDto.QuestionType,
                    TopicId = targetTopicId,
                    SourceNoteId = savedNote.Id,
                    UserId = userId,
                    IsActive = true,
                    Answers = qDto.Answers.Select(aDto => new Answer
                    {
                        Text = aDto.Text,
                        IsCorrect = aDto.IsCorrect,
                        UserId = userId
                    }).ToList()
                }).ToList();

                await _questionService.SaveQuestionsAsync(questionsToSave);
            }
        }
    }
}
