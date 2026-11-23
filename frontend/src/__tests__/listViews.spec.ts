import { describe, it, expect, afterEach, vi } from 'vitest'
import CategoriesView from '@/views/CategoriesView.vue'
import RequestsView from '@/views/RequestsView.vue'

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