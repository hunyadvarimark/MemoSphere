using Moq;
using Core.Interfaces.Services;
using WPF.ViewModels.Quiz;

namespace MemoSphere.WPF.Tests
{
    [TestFixture]
    public class QuizViewModelTests
    {
        private Mock<IQuizService> _mockQuizService;
        private Mock<IQuestionService> _mockQuestionService;
        private Mock<IActiveLearningService> _mockActiveLearningService;
        private QuizViewModel _quizViewModel;

        [SetUp]
        public void Setup()
        {
            _mockQuizService = new Mock<IQuizService>();
            _mockQuestionService = new Mock<IQuestionService>();
            _mockActiveLearningService = new Mock<IActiveLearningService>();

            _quizViewModel = new QuizViewModel(
                _mockQuizService.Object,
                _mockQuestionService.Object,
                _mockActiveLearningService.Object
            );
        }

        [Test]
        public async Task ValidateTopicsForQuizAsync_WhenTopicIdsNull_ShouldSetCanStartQuizToFalse()
        {
            await _quizViewModel.ValidateTopicsForQuizAsync(null);

            Assert.That(_quizViewModel.CanStartQuiz, Is.False);
        }

        [Test]
        public async Task ValidateTopicsForQuizAsync_WhenTopicIdsEmpty_ShouldSetCanStartQuizToFalse()
        {
            var emptyList = new List<int>();

            await _quizViewModel.ValidateTopicsForQuizAsync(emptyList);

            Assert.That(_quizViewModel.CanStartQuiz, Is.False);
        }

        [Test]
        public async Task ValidateTopicsForQuizAsync_WhenQuestionCountIsLessThanThree_ShouldSetCanStartQuizToFalse()
        {
            var topicIds = new List<int> { 1, 2 };
            _mockQuizService.Setup(s => s.GetQuestionCountForTopicsAsync(topicIds))
                            .ReturnsAsync(2);

            await _quizViewModel.ValidateTopicsForQuizAsync(topicIds);

            Assert.That(_quizViewModel.CanStartQuiz, Is.False);
        }

        [Test]
        public async Task ValidateTopicsForQuizAsync_WhenQuestionCountIsThreeOrMore_ShouldSetCanStartQuizToTrue()
        {
            var topicIds = new List<int> { 1, 2 };
            _mockQuizService.Setup(s => s.GetQuestionCountForTopicsAsync(topicIds))
                    .ReturnsAsync(3);

            await _quizViewModel.ValidateTopicsForQuizAsync(topicIds);

            Assert.That(_quizViewModel.CanStartQuiz, Is.True);
        }

        [Test]
        public async Task ValidateTopicsForQuizAsync_WhenServiceThrowsException_ShouldCatchItAndSetCanStartQuizToFalse()
        {
            var topicIds = new List<int> { 1 };
            _mockQuizService.Setup(s => s.GetQuestionCountForTopicsAsync(topicIds))
                            .ThrowsAsync(new Exception("Database connection failed"));

            await _quizViewModel.ValidateTopicsForQuizAsync(topicIds);

            Assert.That(_quizViewModel.CanStartQuiz, Is.False);
        }

        [Test]
        public void ResetState_WhenCalled_ShouldResetQuizProgressProperties()
        {
            _quizViewModel.ResetState();

            Assert.That(_quizViewModel.QuizItems, Is.Empty);
            Assert.That(_quizViewModel.IsQuizFinished, Is.False);
            Assert.That(_quizViewModel.CorrectAnswers, Is.EqualTo(0));
            Assert.That(_quizViewModel.IsEvaluating, Is.False);
            Assert.That(_quizViewModel.CurrentItem, Is.Null);
        }
    }
}