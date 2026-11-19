import { formatLabel, formatMoney } from './format'

export interface DepartmentItemCount {
    departmentName: string
    itemCount: number
}

export interface DashboardItem {
    id: number
    name: string
    departmentName: string
    expirationDate: string
}

export interface MonthCount {
    year: number
    month: number
    count: number
}

export interface AdminDashboard {
    totalItems: number
    activeItems: number
    expiringSoonItems: number
    expiredItems: number
    cancelledItems: number
    itemsByDepartment: DepartmentItemCount[]
    upcomingExpirations: DashboardItem[]
    expirationsByMonth: MonthCount[]
}

export interface DashboardRequest {
    id: number
    itemName: string
    status: string
    needsFinancialReview: boolean
    proposedExpirationDate: string
    submittedAt: string
    proposedCost: number | null
    requestedByUserName: string
}

export interface ManagerDashboard {
    totalItems: number
    activeItems: number
    expiringSoonItems: number
    expiredItems: number
    cancelledItems: number
    requestsToReview: DashboardRequest[]
    requestsAwaitingCompletion: DashboardRequest[]
    upcomingExpirations: DashboardItem[]
    expirationsByMonth: MonthCount[]
    requestsByStatus: ChartEntry[]
}

export interface EmployeeDashboard {
    totalItems: number
    activeItems: number
    expiringSoonItems: number
    expiredItems: number
    cancelledItems: number
    itemsNeedingRenewal: DashboardItem[]
    requestsNeedingClarification: DashboardRequest[]
    requestsAwaitingCompletion: DashboardRequest[]
    myOpenRequests: DashboardRequest[]
    upcomingExpirations: DashboardItem[]
    expirationsByMonth: MonthCount[]
    itemsByTimeLeft: ChartEntry[]
}

export interface FinanceDashboard {
    activeItems: number
    expiringSoonItems: number
    expiredItems: number
    cancelledItems: number
    requestsToReviewCount: number
    totalCostWaitingForReview: number
    expectedUpcomingCosts: number
    approvedCost: number
    requestsNeedingFinancialReview: DashboardRequest[]
    expirationsByMonth: MonthCount[]
    costsByDepartment: ChartEntry[]
}

export interface ChartEntry {
    label: string
    value: number
}

export function getTiles(admin: AdminDashboard | null, manager: ManagerDashboard | null,
    employee: EmployeeDashboard | null, finance: FinanceDashboard | null
) {
    if (finance) {
        return [
            { label: 'Requests to review', value: finance.requestsToReviewCount, note: 'waiting for you', color: 'shade-5', link: '/requests' },
            { label: 'Cost waiting for review', value: formatMoney(finance.totalCostWaitingForReview), note: `${finance.requestsToReviewCount} requests`, color: 'shade-4', link: '/requests' },
            { label: 'Expected upcoming costs', value: formatMoney(finance.expectedUpcomingCosts), note: 'items expiring soon', color: 'shade-3', link: '/items' },
            { label: 'Approved cost', value: formatMoney(finance.approvedCost), note: 'not renewed yet', color: 'shade-2', link: '/requests' },
        ]
    }
    const data = admin ?? manager ?? employee
    if (!data) return []
        return [
        { label: 'Total items', value: data.totalItems, color: 'shade-5', link: '/items' },
        { label: 'Active', value: data.activeItems, color: 'shade-4', link: '/items?state=Active' },
        { label: 'Expiring soon', value: data.expiringSoonItems, color: 'shade-3', link: '/items?state=ExpiringSoon' },
        { label: 'Expired', value: data.expiredItems, color: 'shade-2', link: '/items?state=Expired' },
        { label: 'Cancelled', value: data.cancelledItems, color: 'shade-1', link: '/items?state=Cancelled' },
    ]
}

export function getBarChart(admin: AdminDashboard | null, manager: ManagerDashboard | null,
    employee: EmployeeDashboard | null, finance: FinanceDashboard | null
)
{
    if (admin) {
        return {
            title: 'Items per department',
            labels: admin.itemsByDepartment.map((d) => d.departmentName),
            values: admin.itemsByDepartment.map((d) => d.itemCount) 
        }
    }

    if (manager) {
        return {
            title: 'Requests by status',
            labels: manager.requestsByStatus.map((e) => formatLabel(e.label)),
            values: manager.requestsByStatus.map((e) => e.value) 
        }
    }

    if (employee) {
        return {
            title: 'Items by time left',
            labels: employee.itemsByTimeLeft.map((e) => formatLabel(e.label)),
            values: employee.itemsByTimeLeft.map((e) => e.value) 
        }
    }

    if (finance) {
        return {
            title: 'Costs by department',
            labels: finance.costsByDepartment.map((e) => e.label),
            values: finance.costsByDepartment.map((e) => e.value) 
        }
    }
    return { title: '', labels: [], values: []}
}

export function getStateCounts(admin: AdminDashboard | null, manager: ManagerDashboard | null,
    employee: EmployeeDashboard | null, finance: FinanceDashboard | null
) {
    const data = admin ?? manager ?? employee ?? finance
    if (!data) return []
    return [data.activeItems, data.expiringSoonItems, data.expiredItems, data.cancelledItems]
}

export function percent(part: number, total: number): number {
    if (total === 0) return 0
    return Math.round((part / total) * 100)
}

export function daysLeftText(date: string): string {
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    const days = Math.round((new Date(date + 'T00:00:00').getTime() - today.getTime()) / 86400000)
    if (days < 0) return 'Expired'
    if (days === 0) return 'Today'
    if (days === 1) return '1 day left'
    return `${days} days left`
}

const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export function getMonthChart(admin: AdminDashboard | null, manager: ManagerDashboard | null,
    employee: EmployeeDashboard | null, finance: FinanceDashboard | null) {
    const data = admin ?? manager ?? employee ?? finance
    if (!data) return { labels: [], values: [] }
    return {
        labels: data.expirationsByMonth.map((m) => `${monthNames[m.month - 1]} ${m.year}`),
        values: data.expirationsByMonth.map((m) => m.count),
    }
}