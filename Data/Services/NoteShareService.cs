using Core.Entities;
using Core.Interfaces.Services;
using Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MemoSphere.Data.Services
{
    public class NoteShareService : INoteShareService
    {
        private readonly INoteService _noteService;
        private readonly IQuestionService _questionService;
        private readonly IAuthService _authService;
        private readonly ITopicService _topicService;
        private readonly ISubjectService _subjectService;

        public NoteShareService(
            INoteService noteService,
            IQuestionService questionService,
            IAuthService authService,
            ITopicService topicService,
            ISubjectService subjectService)
        {
            _noteService = noteService;
            _questionService = questionService;
            _authService = authService;
            _topicService = topicService;
            _subjectService = subjectService;
        }

        // --- EXPORTÁLÁSI MŰVELETEK ---

        public async Task ExportNoteToFileAsync(int noteId, string filePath)
        {
            // Olyan metódust használunk, ami beemeli a kérdéseket is a navigációs tulajdonságon keresztül
            var note = await _noteService.GetNoteByIdAsync(noteId);
            if (note == null) throw new Exception("A jegyzet nem található.");

            // Ha a NoteService.GetNoteByIdAsync nem hozza a Questions-t, itt manuálisan lekérjük
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

            await SaveToFileAsync(exportDto, filePath);
        }

        public async Task ExportTopicToFileAsync(int topicId, string filePath)
        {
            var topic = await _topicService.GetTopicWithHierarchyAsync(topicId); //
            if (topic == null) throw new Exception("Témakör nem található.");

            var exportDto = MapTopicToDto(topic);
            await SaveToFileAsync(exportDto, filePath);
        }

        public async Task ExportSubjectToFileAsync(int subjectId, string filePath)
        {
            var subject = await _subjectService.GetSubjectWithHierarchyAsync(subjectId); //
            if (subject == null) throw new Exception("Tantárgy nem található.");

            var exportDto = new SubjectExportDto
            {
                Title = subject.Title,
                Topics = subject.Topics.Select(MapTopicToDto).ToList()
            };

            await SaveToFileAsync(exportDto, filePath);
        }

        // --- IMPORTÁLÁSI MŰVELETEK ---

        public async Task ImportNoteFromFileAsync(string filePath, int targetTopicId)
        {
            var userId = GetUserIdOrThrow();
            var jsonString = await File.ReadAllTextAsync(filePath);
            var dto = JsonSerializer.Deserialize<NoteExportDto>(jsonString);

            if (dto == null) throw new Exception("Érvénytelen fájlformátum.");

            await CreateNoteFromDtoAsync(dto, targetTopicId, userId);
        }

        public async Task ImportTopicFromFileAsync(string filePath, int targetSubjectId)
        {
            var userId = GetUserIdOrThrow();
            var jsonString = await File.ReadAllTextAsync(filePath);
            var dto = JsonSerializer.Deserialize<TopicExportDto>(jsonString);

            if (dto == null) throw new Exception("Érvénytelen témakör fájl.");

            // 1. Témakör létrehozása
            var newTopic = await _topicService.AddTopicAsync(new Topic
            {
                Title = dto.Title,
                SubjectId = targetSubjectId,
                UserId = userId
            });

            // 2. Jegyzetek és kérdések létrehozása
            foreach (var noteDto in dto.Notes)
            {
                await CreateNoteFromDtoAsync(noteDto, newTopic.Id, userId);
            }
        }

        public async Task ImportSubjectFromFileAsync(string filePath)
        {
            var userId = GetUserIdOrThrow();
            var jsonString = await File.ReadAllTextAsync(filePath);
            var dto = JsonSerializer.Deserialize<SubjectExportDto>(jsonString);

            if (dto == null) throw new Exception("Érvénytelen tantárgy fájl.");

            // 1. Tantárgy létrehozása
            var newSubject = await _subjectService.AddSubjectAsync(dto.Title);

            // 2. Témakörök bejárása
            foreach (var topicDto in dto.Topics)
            {
                var newTopic = await _topicService.AddTopicAsync(new Topic
                {
                    Title = topicDto.Title,
                    SubjectId = newSubject.Id,
                    UserId = userId
                });

                foreach (var noteDto in topicDto.Notes)
                {
                    await CreateNoteFromDtoAsync(noteDto, newTopic.Id, userId);
                }
            }
        }

        // --- PRIVÁT SEGÉDMETÓDUSOK ---

        private async Task CreateNoteFromDtoAsync(NoteExportDto dto, int topicId, Guid userId)
        {
            var newNote = new Note
            {
                Title = dto.Title,
                Content = dto.Content,
                TopicId = topicId,
                UserId = userId
            };

            var savedNote = await _noteService.AddNoteAsync(newNote);

            if (dto.Questions != null && dto.Questions.Any())
            {
                var questionsToSave = dto.Questions.Select(qDto => new Question
                {
                    Text = qDto.Text,
                    QuestionType = qDto.QuestionType,
                    TopicId = topicId,
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

        private TopicExportDto MapTopicToDto(Topic t) => new TopicExportDto
        {
            Title = t.Title,
            Notes = t.Notes.Select(MapNoteToDto).ToList()
        };

        private NoteExportDto MapNoteToDto(Note n) => new NoteExportDto
        {
            Title = n.Title,
            Content = n.Content,
            // Feltételezzük, hogy a Questions be van töltve az Include-al
            Questions = n.Questions?.Select(q => new QuestionExportDto
            {
                Text = q.Text,
                QuestionType = q.QuestionType,
                Answers = q.Answers?.Select(a => new AnswerExportDto
                {
                    Text = a.Text,
                    IsCorrect = a.IsCorrect
                }).ToList() ?? new()
            }).ToList() ?? new()
        };

        private async Task SaveToFileAsync(object data, string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(filePath, jsonString);
        }

        private Guid GetUserIdOrThrow()
        {
            var userId = _authService.GetCurrentUserId();
            if (userId == Guid.Empty) throw new Exception("Nincs bejelentkezve.");
            return userId;
        }
    }
}