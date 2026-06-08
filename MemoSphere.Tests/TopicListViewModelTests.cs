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

        var subjectServiceMock = new Mock<ISubjectService>();
        var subjectViewModelMock = new Mock<SubjectViewModel>(
            subjectServiceMock.Object,
            _noteShareServiceMock.Object
        );

        _crudHandler = new CrudOperationHandler(
            null,
            _topicServiceMock.Object,
            null,
            null,
            null,
            _viewModel,
            null
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

    //[Test]
    //public async Task AddTopic_NevDuplikacioEseten_HibatDobEsNemMentAlapra()
    //{
    //    // ==========================================
    //    // 1. ARRANGE
    //    // ==========================================
    //    int tesztSubjectId = 55;

    //    // GOLYÓÁLLÓ: Bármilyen névvel, bármilyen subjectId-val és bármilyen excludeId-val hívják meg, 
    //    // a Moq azt fogja mondani, hogy IGEN, LÉTEZIK A DUPLIKÁCIÓ (true)!
    //    _topicServiceMock
    //        .Setup(s => s.TopicExistsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>()))
    //        .ReturnsAsync(true);

    //    // BIZTONSÁGI ÖV: Ha a kód mégis továbbmenne a mentés utáni UI frissítésre (LoadTopicsAsync),
    //    // felkészítjük az ActiveLearningService-t is, hogy ne dobjon NullReference / ArgumentNull hibát!
    //    _activeLearningServiceMock
    //        .Setup(a => a.GetActiveTopicsAsync())
    //        .ReturnsAsync(new List<ActiveTopic>());

    //    // Gyártunk egy igazi Subject-et és egy igazi SubjectViewModel-et az ID-val
    //    var kamuSubjectEntitas = new Subject { Id = tesztSubjectId, Title = "Kamu Tantárgy" };
    //    var igaziSubjectVm = new SubjectViewModel(kamuSubjectEntitas);

    //    // Létrehozunk egy igazi SubjectListViewModel-t
    //    var subjectServiceMock = new Mock<ISubjectService>();
    //    var igaziSubjectListVm = new SubjectListViewModel(
    //        subjectServiceMock.Object,
    //        _noteShareServiceMock.Object
    //    );

    //    // Beállítjuk rajta a kijelölést kézzel
    //    igaziSubjectListVm.SelectedSubject = igaziSubjectVm;

    //    // Összerakunk egy egyedi handlert, ami megkapja ezt a valódi ViewModel-t
    //    var izolaltHandler = new CrudOperationHandler(
    //        null,
    //        _topicServiceMock.Object,
    //        null,
    //        null,
    //        igaziSubjectListVm,
    //        _viewModel,
    //        null
    //    );

    //    // A menteni kívánt új téma objektum
    //    var masodikMatek = new Topic
    //    {
    //        Id = 0,
    //        Title = "Matek",
    //        SubjectId = tesztSubjectId,
    //        Subject = kamuSubjectEntitas
    //    };

    //    // ==========================================
    //    // 2. ACT & ASSERT
    //    // ==========================================
    //    Assert.CatchAsync<InvalidOperationException>(async () =>
    //        await izolaltHandler.SaveTopicAsync(masodikMatek)
    //    );
    //}
}
