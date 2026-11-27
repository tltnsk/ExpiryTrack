using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Services;

public class UserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserResult> CreateAsync(CreateUserRequest request)
    {
        var email = request.Email.Trim();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return new UserResult { Conflict = "A user with this email exists already." };

        var role = await _db.Roles.SingleOrDefaultAsync(r => r.Name == request.Role);
        if (role == null) return new UserResult { Error = "Unknown role." };

        var departmentError = await CheckDepartment(role.Name, request.DepartmentId);
        if (departmentError != null) return new UserResult { Error = departmentError };

        if (role.Name == "DepartmentManager" && await DepartmentHasManager(request.DepartmentId!.Value))
            return new UserResult { Conflict = "This department already has a department manager." };

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            RoleId = role.Id,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var created = await LoadUser(user.Id);
        return new UserResult { User = created };
    }

    public async Task<UserResult> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await LoadUser(id);

        if (user == null)
            return new UserResult { NotFound = true };

        var email = request.Email.Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id))
            return new UserResult { Conflict = "A user with this email already exists." };

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        var updated = await LoadUser(id);
        return new UserResult { User = updated };
    }

    public async Task<UserResult> SetActiveAsync(int id, int currentUserId, SetUserActiveRequest request)
    {
        var user = await LoadUser(id);
        if (user == null)
            return new UserResult { NotFound = true };

        // reactivating an old manager must not give the department a second one
        if (request.IsActive && !user.IsActive && user.Role.Name == "DepartmentManager" && await DepartmentHasManager(user.DepartmentId!.Value))
            return new UserResult { Conflict = "This department already has an active department manager." };

        if (!request.IsActive)
        {
            if (id == currentUserId)
                return new UserResult { Conflict = "You cannot deactivate your own account." };

            // A user who still owns items or open requests can't be deactivated (U1)
            bool ownsItems = await _db.Items.AnyAsync(i => i.ResponsibleUserId == id
                && (i.LifecycleState == LifecycleState.Active || i.LifecycleState == LifecycleState.ExpiringSoon));
            if (ownsItems)
                return new UserResult { Conflict = "This user is still responsible for items. Reassign the items first." };

            bool hasOpenRequests = await _db.RenewalRequests.AnyAsync(r => r.Period.Item.ResponsibleUserId == id
                && r.Status != RequestStatus.Rejected && r.Status != RequestStatus.Completed);

            if (hasOpenRequests)
                return new UserResult { Conflict = "This user has open renewal requests." };

            if (await _db.Departments.AnyAsync(d => d.ManagerId == id))
                return new UserResult { Conflict = "This user manages a department. Assign another manager first." };

            if (user.Role.Name == "FinanceOfficer")
            {
                bool otherFinanceOfficer = await _db.Users.AnyAsync(u =>
                    u.Id != id && u.Role.Name == "FinanceOfficer" && u.IsActive);

                bool waitingForFinance = await _db.RenewalRequests
                    .AnyAsync(r => r.Status == RequestStatus.PendingFinancialReview);

                if (!otherFinanceOfficer && waitingForFinance)
                {
                    return new UserResult
                    {
                        Conflict =
                            "This is the only active finance officer and some requests are waiting for financial review. "
                    };
                }
            }
        }

        user.IsActive = request.IsActive;
        await _db.SaveChangesAsync();

        return new UserResult { User = user };
    }

    // helper method for loading user by id
    private async Task<User?> LoadUser(int id)
    {
        return await _db.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .SingleOrDefaultAsync(u => u.Id == id);
    }

    // validator for departments
    // employees and managers need an active department
    // administrators and finance officers don't belong to a department
    // the method returns null if everything is fine
    private async Task<string?> CheckDepartment(string roleName, int? departmentId)
    {
        bool needsDepartment = roleName == "Employee" || roleName == "DepartmentManager";

        if (needsDepartment && departmentId == null)
            return "Employees and department managers must belong to a department.";

        if (!needsDepartment && departmentId != null)
            return "Administrators and finance officers do not belong to a department.";

        if (departmentId != null && !await _db.Departments.AnyAsync(d => d.Id == departmentId && d.isActive))
            return "The department does not exist or is inactive.";

        return null;
    }

    // a department can have only one active user
    private async Task<bool> DepartmentHasManager(int departmentId)
    {
        return await _db.Users.AnyAsync(u => u.DepartmentId == departmentId && u.Role.Name == "DepartmentManager" && u.IsActive);
    }
}