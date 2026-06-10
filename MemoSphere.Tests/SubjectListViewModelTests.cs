using Moq;
using Core.Entities;
using Core.Interfaces.Services;
using WPF.ViewModels.Subjects;


namespace MemoSphere.WPF.Tests
{
    [TestFixture]
    public class SubjectListViewModelTests
    {
        private Mock<ISubjectService> _subjectServiceMock;
        private Mock<INoteShareService> _noteShareServiceMock;
        private SubjectListViewModel _viewModel;
        private CrudOperationHandler _crudHandler;

        [SetUp]
        public void Setup()
        {
            _subjectServiceMock = new Mock<ISubjectService>();
            _noteShareServiceMock = new Mock<INoteShareService>();
            _viewModel = new SubjectListViewModel(_subjectServiceMock.Object, _noteShareServiceMock.Object);
            _crudHandler = new CrudOperationHandler(
                _subjectServiceMock.Object,
                null,
                null,
                null
            );
        }

        [Test]
        public async Task AddSubject_UresNevEseten_HibatDobEsNemMentAlapra()
        {
            _subjectServiceMock
                .Setup(s => s.AddSubjectAsync(""))
                .ThrowsAsync(new ArgumentException("A tantárgy címe nem lehet üres."));

            var hibasSubject = new Subject { Id = 0, Title = "" };

            Assert.CatchAsync<ArgumentException>(async () =>
                await _crudHandler.SaveSubjectAsync(hibasSubject)
            );
        }

        [Test]
        public async Task AddSubject_NevDuplikacioEseten_HibatDobEsNemMentAlapra()
        {
            _subjectServiceMock
                .Setup(s => s.AddSubjectAsync("Matek"))
                .ThrowsAsync(new InvalidOperationException("Már létezik 'Matek' nevű tantárgy!"));

            var masodikMatek = new Subject { Id = 0, Title = "Matek" };

            Assert.CatchAsync<InvalidOperationException>(async () =>
                await _crudHandler.SaveSubjectAsync(masodikMatek)
            );
        }


        [Test]
        [TestCase(1, "Programozás")]
        [TestCase(3, "Teszt Tárgy")]
        public async Task LoadSubjectsAsync_SikeresLekeres_FeltoltiAVmListat(int count, string titlePrefix)
        {
            var mintaList = new List<Subject>();
            for (int i = 0; i < count; i++)
            {
                mintaList.Add(new Subject { Id = i + 1, Title = $"{titlePrefix} {i}" });
            }

            _subjectServiceMock.Setup(s => s.GetAllSubjectsAsync()).ReturnsAsync(mintaList);

            await _viewModel.LoadSubjectsAsync();

            Assert.That(_viewModel.Subjects.Count, Is.EqualTo(count));
            Assert.That(_viewModel.Subjects[0].Title, Is.EqualTo($"{titlePrefix} 0"));
        }
        [Test]
        public async Task LoadSubjectsAsync_UresListaErkezik_AVmListajaIsUresMarad()
        {
            _subjectServiceMock.Setup(s => s.GetAllSubjectsAsync()).ReturnsAsync(new List<Subject>());

            await _viewModel.LoadSubjectsAsync();

            Assert.That(_viewModel.Subjects, Is.Empty);
        }
        [Test]
        public void LoadSubjectsAsync_SzervizHibatDob_A_MetodusTovabbDobjaAHibat()
        {
            _subjectServiceMock
                .Setup(s => s.GetAllSubjectsAsync())
                .ThrowsAsync(new Exception("API szerver nem elérhető"));

            Assert.ThrowsAsync<Exception>(async () => await _viewModel.LoadSubjectsAsync());
        }
        [Test]
        public void RemoveSubject_LetezoIdEseten_TorliAElemetEsNullazzaAKijelölest()
        {
            var subject1 = new Subject { Id = 42, Title = "Matek" };
            var subject2 = new Subject { Id = 99, Title = "Kémia" };

            var vmSubject1 = new SubjectViewModel(subject1);
            var vmSubject2 = new SubjectViewModel(subject2);

            _viewModel.Subjects.Add(vmSubject1);
            _viewModel.Subjects.Add(vmSubject2);

            _viewModel.SelectedSubject = vmSubject2;

            _viewModel.RemoveSubject(99);

            Assert.That(_viewModel.Subjects.Count, Is.EqualTo(1));
            Assert.That(_viewModel.Subjects[0].Id, Is.EqualTo(42));

            Assert.That(_viewModel.SelectedSubject, Is.Null);
        }
    }
}