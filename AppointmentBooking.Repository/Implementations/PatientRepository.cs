using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Repository.Implementations;

public class PatientRepository : IPatientRepository
{
    private readonly AppointmentBookingDbContext _context;

    public PatientRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public Task<Patient?> GetByIdAsync(int patientId, CancellationToken cancellationToken = default) =>
        _context.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId && p.IsActive, cancellationToken);

    public Task<Patient?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, cancellationToken);

    public Task<Patient?> GetDetailByIdAsync(int patientId, CancellationToken cancellationToken = default) =>
        _context.Patients
            .Include(p => p.EmergencyContacts.Where(c => c.IsActive))
            .Include(p => p.MedicalHistory.Where(h => h.IsActive))
            .Include(p => p.Documents.Where(d => d.IsActive))
            .FirstOrDefaultAsync(p => p.PatientId == patientId && p.IsActive, cancellationToken);

    public Task<Patient?> GetDetailByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Patients
            .Include(p => p.EmergencyContacts.Where(c => c.IsActive))
            .Include(p => p.MedicalHistory.Where(h => h.IsActive))
            .Include(p => p.Documents.Where(d => d.IsActive))
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, cancellationToken);

    public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(
        string? search, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Patients.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.FirstName.Contains(term) ||
                p.LastName.Contains(term) ||
                p.PatientCode.Contains(term) ||
                (p.Email != null && p.Email.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Patients.AnyAsync(p => p.UserId == userId, cancellationToken);

    public async Task<string> GeneratePatientCodeAsync(CancellationToken cancellationToken = default)
    {
        var count = await _context.Patients.CountAsync(cancellationToken);
        return $"PAT-{(count + 1):D5}";
    }

    public async Task<Patient> CreateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await _context.Patients.AddAsync(patient, cancellationToken);
        return patient;
    }

    public Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        patient.UpdatedAt = DateTime.UtcNow;
        _context.Patients.Update(patient);
        return Task.CompletedTask;
    }

    public Task<EmergencyContact?> GetEmergencyContactByIdAsync(int contactId, CancellationToken cancellationToken = default) =>
        _context.EmergencyContacts.FirstOrDefaultAsync(c => c.EmergencyContactId == contactId && c.IsActive, cancellationToken);

    public async Task<IReadOnlyList<EmergencyContact>> GetEmergencyContactsAsync(int patientId, CancellationToken cancellationToken = default) =>
        await _context.EmergencyContacts.Where(c => c.PatientId == patientId && c.IsActive).OrderByDescending(c => c.IsPrimary).ToListAsync(cancellationToken);

    public async Task<EmergencyContact> AddEmergencyContactAsync(EmergencyContact contact, CancellationToken cancellationToken = default)
    {
        await _context.EmergencyContacts.AddAsync(contact, cancellationToken);
        return contact;
    }

    public Task UpdateEmergencyContactAsync(EmergencyContact contact, CancellationToken cancellationToken = default)
    {
        contact.UpdatedAt = DateTime.UtcNow;
        _context.EmergencyContacts.Update(contact);
        return Task.CompletedTask;
    }

    public Task DeleteEmergencyContactAsync(EmergencyContact contact, CancellationToken cancellationToken = default)
    {
        contact.IsActive = false;
        contact.UpdatedAt = DateTime.UtcNow;
        _context.EmergencyContacts.Update(contact);
        return Task.CompletedTask;
    }

    public Task<PatientMedicalHistory?> GetMedicalHistoryByIdAsync(int historyId, CancellationToken cancellationToken = default) =>
        _context.PatientMedicalHistories.FirstOrDefaultAsync(h => h.PatientMedicalHistoryId == historyId && h.IsActive, cancellationToken);

    public async Task<IReadOnlyList<PatientMedicalHistory>> GetMedicalHistoryAsync(int patientId, CancellationToken cancellationToken = default) =>
        await _context.PatientMedicalHistories.Where(h => h.PatientId == patientId && h.IsActive).OrderByDescending(h => h.DiagnosisDate).ToListAsync(cancellationToken);

    public async Task<PatientMedicalHistory> AddMedicalHistoryAsync(PatientMedicalHistory history, CancellationToken cancellationToken = default)
    {
        await _context.PatientMedicalHistories.AddAsync(history, cancellationToken);
        return history;
    }

    public Task UpdateMedicalHistoryAsync(PatientMedicalHistory history, CancellationToken cancellationToken = default)
    {
        history.UpdatedAt = DateTime.UtcNow;
        _context.PatientMedicalHistories.Update(history);
        return Task.CompletedTask;
    }

    public Task DeleteMedicalHistoryAsync(PatientMedicalHistory history, CancellationToken cancellationToken = default)
    {
        history.IsActive = false;
        history.UpdatedAt = DateTime.UtcNow;
        _context.PatientMedicalHistories.Update(history);
        return Task.CompletedTask;
    }

    public Task<PatientDocument?> GetDocumentByIdAsync(int documentId, CancellationToken cancellationToken = default) =>
        _context.PatientDocuments.FirstOrDefaultAsync(d => d.PatientDocumentId == documentId && d.IsActive, cancellationToken);

    public async Task<IReadOnlyList<PatientDocument>> GetDocumentsAsync(int patientId, CancellationToken cancellationToken = default) =>
        await _context.PatientDocuments.Where(d => d.PatientId == patientId && d.IsActive).OrderByDescending(d => d.UploadedAt).ToListAsync(cancellationToken);

    public async Task<PatientDocument> AddDocumentAsync(PatientDocument document, CancellationToken cancellationToken = default)
    {
        await _context.PatientDocuments.AddAsync(document, cancellationToken);
        return document;
    }

    public Task DeleteDocumentAsync(PatientDocument document, CancellationToken cancellationToken = default)
    {
        document.IsActive = false;
        _context.PatientDocuments.Update(document);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
