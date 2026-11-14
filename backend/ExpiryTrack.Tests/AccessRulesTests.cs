using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Services;

namespace ExpiryTrack.Tests;

public class AccessRulesTests
{
    // helpers
    private static User MakeUser(int id, string roleName, int? departmentId = null)
    {
        return new User {Id = id, DepartmentId = departmentId, Role = new Role {Name = roleName}};
    }

    private static Item MakeItem(int responsibleUserId, int departmentId, decimal? cost = null)
    {
        return new Item
        {
            ResponsibleUserId = responsibleUserId,
            DepartmentId = departmentId,
            CurrentPeriod = new LifecyclePeriod { Cost = cost }
        };
    }

    private static RenewalRequest MakeRequest(Item item, bool needsFinancialReview = false)
    {
        return new RenewalRequest { NeedsFinancialReview = needsFinancialReview, Period = new LifecyclePeriod {Item = item }};
    }
    
    // an employee can only see an item they're responsible for
    [Fact]
    public void CanSeeItem_EmployeeResponsible_True()
    {
        User employee = MakeUser(1, "Employee", departmentId: 1);
        Item item = MakeItem(responsibleUserId: 1, departmentId: 1);
        bool result = AccessRules.CanSeeItem(item, employee);
        
        Assert.True(result);
    }

    [Fact]
    public void CanSeeItem_EmployeeSameDepartmentNotResponsible_False()
    {
        User employee = MakeUser(1, "Employee", departmentId: 1);
        User responsibleUser = MakeUser(2, "Employee", departmentId: 1);
        Item item = MakeItem(responsibleUserId: 2, departmentId: 1);
        
        bool result = AccessRules.CanSeeItem(item, employee);
        Assert.False(result);
    }
    
    [Fact]
    public void CanSeeItem_ManagerSameDepartment_True()
    {
        User manager = MakeUser(1, "DepartmentManager", departmentId: 1);
        Item item = MakeItem(responsibleUserId: 2, departmentId: 1);
        
        bool result = AccessRules.CanSeeItem(item, manager);
        Assert.True(result);
    }
    
    [Fact]
    public void CanSeeItem_ManagerOtherDepartment_False()
    {
        User manager = MakeUser(1, "DepartmentManager", departmentId: 1);
        User otherManager = MakeUser(2, "DepartmentManager", departmentId: 2);
        Item item = MakeItem(responsibleUserId: 3, departmentId: 2);
        
        bool result = AccessRules.CanSeeItem(item, manager);
        Assert.False(result);
    }
    
    [Fact]
    public void CanSeeItem_FinanceItemWithCost_True()
    {
        User finance = MakeUser(1, "FinanceOfficer");
        Item item = MakeItem(responsibleUserId: 3, departmentId: 2, cost: 10000);
        
        bool result = AccessRules.CanSeeItem(item, finance);
        Assert.True(result);
    }
    
    [Fact]
    public void CanSeeItem_FinanceItemWithoutCost_False()
    {
        User finance = MakeUser(1, "FinanceOfficer");
        Item item = MakeItem(responsibleUserId: 3, departmentId: 2);
        
        bool result = AccessRules.CanSeeItem(item, finance);
        Assert.False(result);
    }
    
    [Fact]
    public void CanSeeItem_AdminAll_True()
    {
        User admin = MakeUser(1, "Administrator");
        Item item = MakeItem(responsibleUserId: 3, departmentId: 2);
        
        bool result = AccessRules.CanSeeItem(item, admin);
        Assert.True(result);
    }

    [Fact]
    public void CanSeeItem_UnknownRole_False()
    {
        User auditor = MakeUser(2, "Auditor");
        Item item = MakeItem(responsibleUserId: 2, departmentId: 1);
        
        bool result = AccessRules.CanSeeItem(item, auditor);
        Assert.False(result);
    }
    
    // CanSeeRequest 
    [Fact]
    public void CanSeeRequest_EmployeeResponsibleForItem_True()
    {
        User employee = MakeUser(1, "Employee", departmentId: 1);
        Item item = MakeItem(responsibleUserId: 1, departmentId: 1);
        RenewalRequest request = MakeRequest(item);
        
        bool result = AccessRules.CanSeeRequest(request, employee);
        Assert.True(result);
    }
    
    [Fact]
    public void CanSeeRequest_EmployeeNotResponsible_False()
    {
        User employee = MakeUser(2, "Employee", departmentId: 1);
        Item item = MakeItem(responsibleUserId: 1, departmentId: 1);
        RenewalRequest request = MakeRequest(item);
        
        bool result = AccessRules.CanSeeRequest(request, employee);
        Assert.False(result);
    }
    
    [Fact]
    public void CanSeeRequest_OtherDepartment_False()
    {
        User manager = MakeUser(1, "DepartmentManager", departmentId: 1);
        Item item = MakeItem(responsibleUserId: 1, departmentId: 2);
        RenewalRequest request = MakeRequest(item);
        
        bool result = AccessRules.CanSeeRequest(request, manager);
        Assert.False(result);
    }
    
    [Fact]
    public void CanSeeRequest_Finance_NeedsReview_True()
    {
        User finance = MakeUser(1, "FinanceOfficer");
        Item item = MakeItem(responsibleUserId: 2, departmentId: 2);
        RenewalRequest request = MakeRequest(item, needsFinancialReview:true);
        
        bool result = AccessRules.CanSeeRequest(request, finance);
        Assert.True(result);
    }
    
    [Fact]
    public void CanSeeRequest_Finance_NoReview_False()
    {
        User finance = MakeUser(1, "FinanceOfficer");
        Item item = MakeItem(responsibleUserId: 2, departmentId: 2);
        RenewalRequest request = MakeRequest(item, needsFinancialReview:false);
        
        bool result = AccessRules.CanSeeRequest(request, finance);
        Assert.False(result);
    }
    
    [Fact]
    public void CanSeeRequest_Admin()
    {
        User admin = MakeUser(1, "Administrator");
        Item item = MakeItem(responsibleUserId: 2, departmentId: 2);
        RenewalRequest request = MakeRequest(item, needsFinancialReview:true);
        
        bool result = AccessRules.CanSeeRequest(request, admin);
        Assert.True(result);
    }
    
    
    [Fact]
    public void CanSeeRequest_UnknownRole_False()
    {
        User auditor = MakeUser(2, "Auditor");
        Item item = MakeItem(responsibleUserId: 2, departmentId: 1);
        RenewalRequest request = MakeRequest(item, needsFinancialReview:true);

        bool result = AccessRules.CanSeeRequest(request, auditor);
        Assert.False(result);
    }
}