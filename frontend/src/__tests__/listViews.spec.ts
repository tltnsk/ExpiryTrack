import { describe, it, expect, afterEach, vi } from 'vitest'
import { flushPromises } from '@vue/test-utils'
import ItemsView from '@/views/ItemsView.vue'
import CategoriesView from '@/views/CategoriesView.vue'
import RequestsView from '@/views/RequestsView.vue'
import UsersView from '@/views/UsersView.vue'
import NotificationsView from '@/views/NotificationsView.vue'
import ReportsView from '@/views/ReportsView.vue'

import { mountView, mockFetch, ok, textError, makeUser, sentBody } from './helpers'

afterEach(() => {
    vi.unstubAllGlobals() // put the real fetch back
})

describe('RequestsView', () => {
    it('shows one row per request', async () => {
        mockFetch({
            '/api/requests': ok([
                { id: 7, itemName: 'Microsoft 365', status: 'Approved', proposedExpirationDate: '2027-01-01', proposedCost: 120, submittedAt: '2026-11-01T10:00:00'},
                { id: 8, itemName: 'Car lease', status: 'Rejected', proposedExpirationDate: '2027-02-01', proposedCost: null, submittedAt: '2026-11-02T10:00:00'},
            ]), 
        })

        const { wrapper } = await mountView(RequestsView, '/requests', makeUser('DepartmentManager'))

        const rows = wrapper.findAll('tbody tr')
        expect(rows).toHaveLength(2)
        expect(rows[0]!.text()).toContain('Microsoft 365')
        expect(rows[0]!.text()).toContain('$120.00')
    })

    it('shows an error when the request cannot be loaded', async () => {
        mockFetch({ '/api/requests': textError(500, '')})
        const { wrapper } = await mountView(RequestsView, '/requests', makeUser('Employee'))

        expect(wrapper.find('.error').text()).toContain('error 500')
    })
})

describe('CategoriesView', () => {
    const categories = [
        { id: 1, name: 'Software', description: null, warningPeriodDays: 30, requiresFinancialReview: true, financeReviewThreshold: 500, isActive: true },
    ]
    it('shows new and edit buttons to admin', async () => {
        mockFetch({
            '/api/categories': ok(categories)
        })

        const { wrapper } = await mountView(CategoriesView, '/categories', makeUser('Administrator'))

        expect(wrapper.text()).toContain('New category')
        expect(wrapper.find('a[href="/categories/1/edit"').exists()).toBe(true)
    })

    it('hides the buttons from the other roles', async () => {
        mockFetch({
            '/api/categories': ok(categories)
        })

        const { wrapper } = await mountView(CategoriesView, '/categories', makeUser('Employee'))

        expect(wrapper.text()).not.toContain('New category')
        expect(wrapper.find('a[href="/categories/1/edit"').exists()).toBe(false)
    })
})

describe('UsersView', () => {
    const users = [
        { id: 5, firstName: 'Ivan', lastName: 'Ivanov', email: 'ivan@test.com', role: 'Employee', departmentId: 1, departmentName: 'IT', isActive: true },
    ]
    it('deactivates a user', async () => {
        const fetchMock = mockFetch({
            '/api/users': ok(users), 'PUT /api/users/5/active': ok()
        })
        vi.stubGlobal('confirm', () => true) // the admin confirms the deactivation (clicks OK in the confirm box)

        const { wrapper } = await mountView(UsersView, '/users', makeUser('Administrator'))

        await wrapper.find('tbody button').trigger('click')
        await flushPromises()

        expect(sentBody(fetchMock, 'PUT', '/api/users/5/active')).toEqual({ isActive: false })
        expect(wrapper.find('tbody button').text()).toBe('Activate')
    })
    it('shows a message when a user cannot be deactivated', async () => {
        const fetchMock = mockFetch({
            '/api/users': ok(users), 'PUT /api/users/5/active': textError(409, 'This user manages a department.')
        })
        vi.stubGlobal('confirm', () => true) // the admin confirms the deactivation (clicks OK in the confirm box)

        const { wrapper } = await mountView(UsersView, '/users', makeUser('Administrator'))

        await wrapper.find('tbody button').trigger('click')
        await flushPromises()

        expect(wrapper.find('.error').text()).toBe('This user manages a department.')
    })
})

describe('NotificationsView', () => {
    const notifications = [
        { id: 1, type: 'ItemExpiringSoon', message: 'Microsoft 365 is expiring soon.', isRead: false, createdAt: '2026-11-10T09:00:00', itemId: 4, requestId: null },
        { id: 2, type: 'RequestApproved', message: 'Your request for Car lease was approved.', isRead: true, createdAt: '2026-11-09T09:00:00', itemId: null, requestId: 9 },
    ]

    it('marks a notification as read', async () => {
        const fetchMock = mockFetch({
            '/api/notifications': ok(notifications), 
            'PUT /api/notificatios/1/read': ok(),
            '/api/notifications/unread-count': ok(0),
        })

        const { wrapper } = await mountView(NotificationsView, '/notifications', makeUser('Employee'))

        await wrapper.find('tbody button').trigger('click')
        await flushPromises()

        expect(fetchMock).toHaveBeenCalledWith('/api/notifications/1/read', { method: 'PUT' })
    })
})

describe('ReportsView', () => {
    const report = {
        expiring: { within30Days: 2, within60Days: 3, within90Days: 5 },
        categories: [
            { categoryName: 'Software', itemCount: 4, totalCost: 100 },
            { categoryName: 'Vehicles', itemCount: 2, totalCost: 9000 },
        ],
        topProviders: [],
        topEmployees: [],
    }

    function categoryNames(wrapper: Awaited<ReturnType<typeof mountView>>['wrapper']) {
        return wrapper.findAll('.report-table tbody tr').map((row) => row.find('td').text())
    }

    it('sorts categories by items first', async () => {
        mockFetch({
            '/api/reports': ok(report), 
        })

        const { wrapper } = await mountView(ReportsView, '/reports', makeUser('Administrator'))

        expect(categoryNames(wrapper)).toEqual(['Software', 'Vehicles'])

        const costButton = wrapper.findAll('.sort button').find((button) => button.text() === 'Highest cost')!
        await costButton.trigger('click')

        expect(categoryNames(wrapper)).toEqual(['Vehicles', 'Software'])
    })

    it('shows an error when report cannot be loaded', async () => {
        mockFetch({
            '/api/reports': textError(500, ''), 
        })

        const { wrapper } = await mountView(ReportsView, '/reports', makeUser('Administrator'))

        expect(wrapper.find('.error').text()).toContain('Could not load the report (error 500).')
    })
})

describe('ItemsView', () => {
    const items: object[] = []
    for (let i = 1; i <= 20; i++) {
        items.push({
            id: i,
            name: `Item ${i}`,
            categoryNames: 'Software',
            responsibleUserName: 'Ana Test',
            lifecycleState: i % 4 === 1 ? 'Expired' : 'Active',
            expirationDate: '2027-01-01',
            cost: 100,
            provider: i === 2 ? 'Microsoft' : null,
        })
    }

    it('shows 15 items per page', async () => {
        mockFetch({
            '/api/items': ok(items)
        })

        const { wrapper } = await mountView(ItemsView, '/items', makeUser('DepartmentManager'))

        expect(wrapper.findAll('tbody tr')).toHaveLength(15)
        await wrapper.find('button[aria-label="Next page"]').trigger('click')
        expect(wrapper.findAll('tbody tr')).toHaveLength(5)
    })

    it('searches by name and provider', async () => {
        mockFetch({
            '/api/items': ok(items)
        })

        const { wrapper } = await mountView(ItemsView, '/items', makeUser('DepartmentManager'))

        await wrapper.find('input[type="search"]').setValue('microsoft')

        expect(wrapper.findAll('tbody tr')).toHaveLength(1)
        expect(wrapper.find('tbody').text()).toContain('Item 2')
    })
})