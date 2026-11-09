using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Services;

public static class AccessRules
{
    public static bool CanSeeItem(Item item, User currentUser)
    {
        // employees can only see the items they're responsible for
        if (currentUser.Role.Name == "Employee")
            return item.ResponsibleUserId == currentUser.Id;

        // department managers can see the items in their department
        if (currentUser.Role.Name == "DepartmentManager")
            return item.DepartmentId == currentUser.DepartmentId;

        // finance officers can only see items that have cost
        if (currentUser.Role.Name == "FinanceOfficer")
            return item.CurrentPeriod!.Cost != null;

        // admins can see all the items 
        return true;
    }

    public static bool CanSeeRequest(RenewalRequest request, User currentUser)
    {
        // employees can only see the requests for items they're responsible for
        if (currentUser.Role.Name == "Employee")
            return request.Period.Item.ResponsibleUserId == currentUser.Id;

        // department managers can see the requests for the items in their department
        if (currentUser.Role.Name == "DepartmentManager")
            return request.Period.Item.DepartmentId == currentUser.DepartmentId;

        // finance officers can only see the requests for items that need financial review
        if (currentUser.Role.Name == "FinanceOfficer")
            return request.NeedsFinancialReview;

        // admins can see all the requests 
        return true;
    }
}