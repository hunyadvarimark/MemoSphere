using Core.Entities;
using MemoSphere.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoSphere.Core.Services
{


    public class SubjectService : ISubjectService
    {

        private readonly IUnitOfWork _unitOfWork;

        public SubjectService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }

        // Add a new subject
        public async Task AddSubjectAsync(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A tantárgy címe nem lehet üres.", nameof(title));
            }

            if (title.Length > 100)
            {
                throw new ArgumentException("A tantárgy címe maximum 100 karakter hosszú lehet.", nameof(title));
            }

            var subject = new Subject
            {
                Name = title
            };

            await _unitOfWork.Subjects.AddAsync(subject);
            await _unitOfWork.SaveChangesAsync();
        }

        // Delete a subject by id
        public async Task DeleteSubjectAsync(int id)
        {
            var subjectToDelete = await _unitOfWork.Subjects.GetByIdAsync(id);

            if (subjectToDelete == null)
            {
                throw new ArgumentException("A megadott azonosítóval nem található tantárgy.", nameof(id));
            }

            _unitOfWork.Subjects.Remove(subjectToDelete);
            await _unitOfWork.SaveChangesAsync();
        }

        // Get a subject by id
        public Task<Subject> GetSubjectByIdAsync(int id)
        {
            return _unitOfWork.Subjects.GetByIdAsync(id);
        }

        // Get all subjects
        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            return await _unitOfWork.Subjects.GetAllAsync();
        }
        
        // Get all subjects with topics
        public async Task<IEnumerable<Subject>> GetAllSubjectsWithTopicsAsync()
        {
            return await _unitOfWork.Subjects.GetAllSubjectsWithTopicsAsync();
        }
    }
}
