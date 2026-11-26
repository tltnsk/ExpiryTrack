import { describe, it, expect, afterEach, vi } from 'vitest'
import DashboardView from '@/views/DashboardView.vue'
import {mountView, mockFetch, ok, textError, makeUser} from './helpers' 

// chart.js is replaced with empty function
vi.mock('chart.js/auto', () => ({ default: vi.fn() })) 

afterEach(() => {
    vi.unstubAllGlobals() // put the real fetch back
})

// the numbers every dashboard has
const counts = { totalItems: 10, activeItems: 5, expiringSoonItems: 3, expiredItems: 1, cancelledItems: 1 }
const months = [{ year: 2026, month: 11, count: 2 }]
const upcoming = [{ id: 4, name: 'Zoom', departmentName: 'IT', expirationDate: '2026-12-01' }]
const request = {
    id: 31, itemName: 'Zoom', status: 'PendingManagerApproval', needsFinancialReview: false,
    proposedExpirationDate: '2027-12-01', submittedAt: '2026-11-10T10:00:00', proposedCost: 320, requestedByUserName: 'Ivan Ivanov',
}

describe('DashboardView', () => {
    it('shows the admin the setup checklist', async () => {
        mockFetch({
            '/api/dashboard/admin': ok({
                ...counts,
                itemsByDepartment: [{ departmentName: 'IT', itemCount: 6 }],
                upcomingExpirations: upcoming,
                expirationsByMonth: months,
            }),
            '/api/users': ok ([
                {id: 1, role: 'Administrator', departmentName: null, isActive: true},
                {id: 2, role: 'Employee', departmentName: 'IT', isActive: true}
            ]),
            '/api/departments': ok([{id: 3, name: 'IT', isActive: true, managerName: null}]),
            '/api/categories': ok([{id: 1, isActive: true, requiresFinancialReview: false}])
        })
        const { wrapper } = await mountView(DashboardView, '/dashboard', makeUser('Administrator'))
        expect(wrapper.text()).toContain('IT has no manager')
        expect(wrapper.find('.checklist a[href="/departments/3/edit"]').exists()).toBe(true)
        expect(wrapper.text()).toContain('There is no active finance officer')
        expect(wrapper.text()).toContain('Every department has at least one employee')
    })

    it('shows the manager the requests to review', async () => {
        mockFetch({
            '/api/dashboard/manager': ok({
                ...counts,
                requestsToReview: [request],
                requestsAwaitingCompletion: [],
                upcomingExpirations: [],
                expirationsByMonth: months,
                requestsByStatus: [],
            }),
        })
        const { wrapper } = await mountView(DashboardView, '/dashboard', makeUser('DepartmentManager'))

        expect(wrapper.text()).toContain('No requests awaiting completion')
        expect(wrapper.find('a[href="/requests/31"]').text()).toContain('Review')
    })

    it('shows the employee a renew link for items that need renewal', async () => {
        mockFetch({
            '/api/dashboard/employee': ok({
                ...counts,
                itemsNeedingRenewal: upcoming,
                requestsNeedingClarification: [],
                myOpenRequests: [],
                requestsAwaitingCompletion: [],
                upcomingExpirations: [],
                expirationsByMonth: months,
                itemsByTimeLeft: [],
            }),
        })
        const { wrapper } = await mountView(DashboardView, '/dashboard', makeUser('Employee'))

        expect(wrapper.find('a[href="/items/4/renew"]').exists()).toBe(true)
    })

    it('shows the finance officer the money cards, waiting days and total', async () => {
        mockFetch({
            '/api/dashboard/finance': ok({
                activeItems: 5, expiringSoonItems: 3, expiredItems: 1, cancelledItems: 1,
                requestsToReviewCount: 1, totalCostWaitingForReview: 320, expectedUpcomingCosts: 900, approvedCost: 100,
                requestsNeedingFinancialReview: [request],
                expirationsByMonth: months,
                costsByDepartment: [{label: 'IT', value: 300}, {label: 'HR', value: 100}],
            }),
        })
        const { wrapper } = await mountView(DashboardView, '/dashboard', makeUser('FinanceOfficer'))

        expect(wrapper.findAll('.stat-card')).toHaveLength(4)
        expect(wrapper.find('.total-row').text()).toContain('$320.00')
        expect(wrapper.find('.late').exists()).toBe(true)
    })

    it('shows an error when the dashboard cannot be loaded', async () => {
        mockFetch({
            '/api/dashboard/employee': textError(500, '')
        })
        const { wrapper } = await mountView(DashboardView, '/dashboard', makeUser('Employee'))

        expect(wrapper.find('.error').text()).toBe('Could not load the dashboard.')
    })
})