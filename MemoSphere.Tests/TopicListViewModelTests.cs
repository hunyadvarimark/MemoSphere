using Core.Entities;
using Core.Interfaces.Services;
using Moq;
using WPF.ViewModels.Topics;
using WPF.ViewModels.Subjects;

namespace MemoSphere.WPF.Tests;

public class TopicListViewModelTests
{
    private Mock<ITopicService> _topicServiceMock;
    private Mock<IActiveLearningService> _activeLearningServiceMock;
    private Mock<INoteShareService> _noteShareServiceMock;
    private TopicListViewModel _viewModel;

    private CrudOperationHandler _crudHandler;

    [SetUp]
    public void Setup()
    {
        _topicServiceMock = new Mock<ITopicService>();
        _activeLearningServiceMock = new Mock<IActiveLearningService>();
        _noteShareServiceMock = new Mock<INoteShareService>();
        _viewModel = new TopicListViewModel(_topicServiceMock.Object, _activeLearningServiceMock.Object, _noteShareServiceMock.Object);

        _activeLearningServiceMock
        .Setup(a => a.GetActiveTopicsAsync())
        .ReturnsAsync(new List<ActiveTopic>());

        var subjectServiceMock = new Mock<ISubjectService>();
        var subjectViewModelMock = new Mock<SubjectViewModel>(
            subjectServiceMock.Object,
            _noteShareServiceMock.Object
        );

        var mockDialogService = new Mock<IDialogService>();
        mockDialogService.Setup(d => d.AskConfirmation(It.IsAny<string>(), It.IsAny<string>()))
                         .Returns(DialogResult.Yes);


        _crudHandler = new CrudOperationHandler(
            null,
            _topicServiceMock.Object,
            null,
            null,
            mockDialogService.Object
        );
    }

    [Test]
    public async Task LoadTopicsAsync_SikeresLekeres_FeltoltiAVmListat()
    {

        int tesztSubjectId = 55;

        var mintaTopicok = new List<Topic>
    {
        new Topic { Id = 1, Title = "SQL Alapok", SubjectId = tesztSubjectId },
        new Topic { Id = 2, Title = "JOIN-ok és indexek", SubjectId = tesztSubjectId }
    };

        _topicServiceMock
            .Setup(t => t.GetTopicBySubjectIdAsync(It.IsAny<int>()))
            .ReturnsAsync(mintaTopicok);

        _activeLearningServiceMock
            .Setup(a => a.GetActiveTopicsAsync())
            .ReturnsAsync(new List<ActiveTopic>());

        await _viewModel.LoadTopicsAsync(tesztSubjectId);

        Assert.That(_viewModel.Topics.Count, Is.EqualTo(2));
        Assert.That(_viewModel.Topics[0].Title, Is.EqualTo("JOIN-ok és indexek"));
        Assert.That(_viewModel.Topics[1].Title, Is.EqualTo("SQL Alapok"));
    }
    [Test]
    public async Task LoadTopicsAsync_UresListaErkezik_AVmListajaIsUresMarad()
    {
        int tesztSubjectId = 55;
        _topicServiceMock.Setup(s => s.GetTopicBySubjectIdAsync(tesztSubjectId)).ReturnsAsync(new List<Topic>());

        _activeLearningServiceMock
            .Setup(a => a.GetActiveTopicsAsync())
            .ReturnsAsync(new List<ActiveTopic>());

        await _viewModel.LoadTopicsAsync(tesztSubjectId);

        Assert.That(_viewModel.Topics, Is.Empty);
    }
    
    [Test]
    public void RemoveTopic_LetezoIdEseten_TorliAElemetEsNullazzaAKijelölest()
    {
        var topic1 = new Topic { Id = 42, Title = "Matek" };
        var topic2 = new Topic { Id = 99, Title = "Kémia" };

        var vmTopic1 = new TopicViewModel(topic1);
        var vmTopic2 = new TopicViewModel(topic2);

        _viewModel.Topics.Add(vmTopic1);
        _viewModel.Topics.Add(vmTopic2);


        _viewModel.SelectedTopic = vmTopic2;

        _viewModel.RemoveTopic(99);


        Assert.That(_viewModel.Topics.Count, Is.EqualTo(1));

        Assert.That(_viewModel.Topics[0].Id, Is.EqualTo(42));

        Assert.That(_viewModel.SelectedTopic, Is.Null);
    }

    [Test]
    public async Task AddTopic_NevDuplikacioEseten_HibatDobEsNemMentAlapra()
    {

        _topicServiceMock
            .Setup(s => s.AddTopicAsync(It.IsAny<Topic>()))
            .ThrowsAsync(new InvalidOperationException("Már létezik ilyen nevű téma!"));

        var masodikMatek = new Topic { Id = 0, Title = "Matek" };


        Assert.CatchAsync<InvalidOperationException>(async () =>
            await _crudHandler.SaveTopicAsync(masodikMatek)
        );
    }

    [Test]
    public async Task AddTopic_SikeresMentesEseten_VisszaadjaAzElmentettEntitast()
    {
        var ujTopic = new Topic { Id = 0, Title = "Fizika", SubjectId = 1 };
        var elmentettTopic = new Topic { Id = 101, Title = "Fizika", SubjectId = 1 };

        _topicServiceMock
            .Setup(s => s.AddTopicAsync(It.IsAny<Topic>()))
            .ReturnsAsync(elmentettTopic);

        var result = await _crudHandler.SaveTopicAsync(ujTopic);

        _topicServiceMock
            .Setup(t => t.GetTopicBySubjectIdAsync(1))
            .ReturnsAsync(new List<Topic> { elmentettTopic });

        await _viewModel.LoadTopicsAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(101));

        Assert.That(_viewModel.Topics.Count, Is.EqualTo(1));
        Assert.That(_viewModel.Topics[0].Id, Is.EqualTo(101));
        Assert.That(_viewModel.Topics[0].Title, Is.EqualTo("Fizika"));
    }
}
