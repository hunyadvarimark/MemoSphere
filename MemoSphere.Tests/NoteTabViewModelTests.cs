using NUnit.Framework;
using Moq;
using Core.Entities;
using Core.Interfaces.Services;
using WPF.ViewModels.Notes;
using WPF.ViewModels.Questions;

namespace MemoSphere.WPF.Tests
{
    [TestFixture]
    public class NoteTabViewModelTests
    {
        private Mock<INoteService> _mockNoteService;
        private Mock<IQuestionService> _mockQuestionService;
        private Mock<IDocumentImportService> _mockDocumentImportService;
        private Mock<INoteShareService> _mockNoteShareService;

        private QuestionListViewModel _questionListVM;
        private CrudOperationHandler _crudHandler;

        [SetUp]
        public void Setup()
        {
            _mockNoteService = new Mock<INoteService>();
            _mockQuestionService = new Mock<IQuestionService>();
            _mockDocumentImportService = new Mock<IDocumentImportService>();
            _mockNoteShareService = new Mock<INoteShareService>();

            var mockDialogService = new Mock<IDialogService>();
            mockDialogService.Setup(d => d.AskConfirmation(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(DialogResult.Yes);

            _crudHandler = new CrudOperationHandler(
                new Mock<ISubjectService>().Object,
                new Mock<ITopicService>().Object,
                _mockNoteService.Object,
                _mockQuestionService.Object,
                mockDialogService.Object
            );

            _questionListVM = new QuestionListViewModel(_mockQuestionService.Object);
        }

        [Test]
        public void ContentChanged_WhenNewTextEntered_ShouldSetHasUnsavedChangesToTrue()
        {
            var note = new Note { Id = 1, TopicId = 5, Title = "Teszt Cím", Content = "Eredeti tartalom" };
            var noteTabVM = new NoteTabViewModel(
                note,
                _mockNoteService.Object,
                _questionListVM,
                _mockDocumentImportService.Object,
                null,
                _crudHandler,
                _mockNoteShareService.Object
            );

            // Act
            noteTabVM.Content = "Ez egy módosított szöveg.";

            // Assert
            Assert.That(noteTabVM.HasUnsavedChanges, Is.True);
        }

        [Test]
        public void MarkAsSaved_WhenCalled_ShouldResetHasUnsavedChangesToFalse()
        {
            // Arrange
            var note = new Note { Id = 1, TopicId = 5, Title = "Teszt Cím", Content = "Eredeti tartalom" };
            var noteTabVM = new NoteTabViewModel(note, _mockNoteService.Object, _questionListVM, _mockDocumentImportService.Object, null, _crudHandler, _mockNoteShareService.Object);

            noteTabVM.Content = "Gyors módosítás";

            // Act
            noteTabVM.MarkAsSaved();

            // Assert
            Assert.That(noteTabVM.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void CanSave_WhenTitleIsEmpty_ShouldReturnFalse()
        {
            // Arrange
            var note = new Note { Id = 1, TopicId = 5, Title = "Jó Cím", Content = "Jó tartalom" };
            var noteTabVM = new NoteTabViewModel(note, _mockNoteService.Object, _questionListVM, _mockDocumentImportService.Object, null, _crudHandler, _mockNoteShareService.Object);

            // Act
            noteTabVM.Title = "";

            // Assert
            bool canSave = noteTabVM.SaveCommand.CanExecute(null);
            Assert.That(canSave, Is.False);
        }

        [Test]
        public void CanSave_WhenContentIsEmpty_ShouldReturnFalse()
        {
            // Arrange
            var note = new Note { Id = 1, TopicId = 5, Title = "Jó Cím", Content = "Jó tartalom" };
            var noteTabVM = new NoteTabViewModel(note, _mockNoteService.Object, _questionListVM, _mockDocumentImportService.Object, null, _crudHandler, _mockNoteShareService.Object);

            // Act
            noteTabVM.Content = "";

            // Assert
            bool canSave = noteTabVM.SaveCommand.CanExecute(null);
            Assert.That(canSave, Is.False);
        }

        [Test]
        public void MarkdownDetection_WhenNoteOpenedWithLaTeXFormulas_ShouldSetIsMarkdownContentToTrue()
        {
            var note = new Note
            {
                Id = 1,
                TopicId = 5,
                Title = "Matek Jegyzet",
                Content = @"Képlet: $$ x = \frac{1}{2} $$"
            };

            var noteTabVM = new NoteTabViewModel(
                note,
                _mockNoteService.Object,
                _questionListVM,
                _mockDocumentImportService.Object,
                null,
                _crudHandler,
                _mockNoteShareService.Object
            );

            Assert.That(noteTabVM.IsMarkdownContent, Is.True);
        }
    }
}