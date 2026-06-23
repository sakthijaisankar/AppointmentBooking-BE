using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Repository.Implementations;

public class RoleRepository : IRoleRepository
{
    private readonly AppointmentBookingDbContext _context;

    public RoleRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default) =>
        _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName && r.IsActive, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await _context.Roles.Where(r => r.IsActive).OrderBy(r => r.RoleName).ToListAsync(cancellationToken);
}
