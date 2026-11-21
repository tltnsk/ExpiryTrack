import { describe, it, expect } from 'vitest'
import { getTiles, getBarChart, getStateCounts, getMonthChart, percent,
    type AdminDashboard, type ManagerDashboard, type EmployeeDashboard, type FinanceDashboard
} from '@/utils/dashboard'

const months = [
    { year: 2026, month: 11, count: 2},
    { year: 2026, month: 12, count: 0},
    { year: 2027, month: 1, count: 5}
]

const admin: AdminDashboard = {
    totalItems: 20, activeItems: 10, expiringSoonItems: 5, expiredItems: 3, cancelledItems: 2,
    itemsByDepartment: [
        { departmentName: 'IT', itemCount: 12 },
        { departmentName: 'HR', itemCount: 8 },
    ],
    upcomingExpirations: [],
    expirationsByMonth: months,
}

const manager: ManagerDashboard = {
    totalItems: 4, activeItems: 1, expiringSoonItems: 1, expiredItems: 1, cancelledItems: 1,
    requestsToReview: [], requestsAwaitingCompletion: [], upcomingExpirations: [],
    expirationsByMonth: months,
    requestsByStatus: [
        { label: 'PendingManagerApproval', value: 3 },
        { label: 'Approved', value: 1 },
    ],
}

const employee: EmployeeDashboard = {
    totalItems: 0, activeItems: 0, expiringSoonItems: 0, expiredItems: 0, cancelledItems: 0,
    itemsNeedingRenewal: [], requestsNeedingClarification: [], requestsAwaitingCompletion: [],
    myOpenRequests: [], upcomingExpirations: [],
    expirationsByMonth: months,
    itemsByTimeLeft: [
        { label: 'Expired', value: 1 },
        { label: 'Something', value: 2 },
    ],
}

const finance: FinanceDashboard = {
    activeItems: 7, expiringSoonItems: 2, expiredItems: 1, cancelledItems: 0,
    requestsToReviewCount: 3, totalCostWaitingForReview: 1500, expectedUpcomingCosts: 2500.5, approvedCost: 0,
    requestsNeedingFinancialReview: [],
    expirationsByMonth: months,
    costsByDepartment: [{ label: 'IT', value: 900 }],
}

describe('percent', () => {
    it('rounds to a whole number', () => {
        expect(percent(1, 3)).toBe(33)
        expect(percent(2, 3)).toBe(67)
    })
    it('returns 0 when total is 0', () => {
        expect(percent(5, 0)).toBe(0)
    })
})

describe('getTiles', () => {
    it('shows item counts for the admin', () => {
        const tiles = getTiles(admin, null, null, null)

        expect(tiles).toHaveLength(5)
        expect(tiles[0]).toMatchObject({ label: 'Total items', value: 20, link: '/items' })
        expect(tiles[1]).toMatchObject({ label: 'Active', value: 10, link: '/items?state=Active' })
        expect(tiles[4]).toMatchObject({ label: 'Cancelled', value: 2 })
    })
    it('uses the manager data when there is no admin data', () => {
        const tiles = getTiles(null, manager, null, null)
        expect(tiles[0]?.value).toBe(4)
    })
    it('shows money tiles for the finance officer', () => {
        const tiles = getTiles(null, null, null, finance)

        expect(tiles).toHaveLength(4)
        expect(tiles[0]).toMatchObject({ label: 'Requests to review', value: 3 })
        expect(tiles[1]).toMatchObject({ value: '$1,500.00', note: '3 requests' })
        expect(tiles[2]?.value).toBe('$2,500.50')
        expect(tiles[3]?.value).toBe('$0.00')
    })
    it('returns no tiles when nothing is loaded', () => {
        expect(getTiles(null, null, null, null)).toEqual([])
    })
})

describe('getBarChart', () => {
    it('shows items per department for the admin', () => {
        expect(getBarChart(admin, null, null, null)).toEqual({
            title: 'Items per department',
            labels: ['IT', 'HR'],
            values: [12, 8],
        })
    })
    it('shows requests by status for the manager', () => {
        expect(getBarChart(null, manager, null, null)).toEqual({
            title: 'Requests by status',
            labels: ['Waiting for manager', 'Approved'],
            values: [3, 1],
        })
    })
    it('shows items by time left for the employee', () => {
        const chart = getBarChart(null, null, employee, null)
        expect(chart.title).toBe('Items by time left')
        expect(chart.labels).toEqual(['Expired', 'Something'])
        expect(chart.values).toEqual([1, 2])
    })
    it('shows costs by department for the finance officer', () => {
        expect(getBarChart(null, null, null, finance)).toEqual({
            title: 'Costs by department',
            labels: ['IT'],
            values: [900],
        })
    })
})

describe('getStateCounts', () => {
    it('returns active, expiring soon, expired and cancelled', () => {
        expect(getStateCounts(admin, null, null, null)).toEqual([10, 5, 3, 2])
    })
})

describe('getMonthChart', () => {
    it('turns month numbers into short names with the year', () => {
        expect(getMonthChart(null, manager, null, null)).toEqual({
            labels: ['Nov 2026', 'Dec 2026', 'Jan 2027'],
            values: [2, 0, 5],
        })
    })
})
