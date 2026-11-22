import { describe, it, expect, afterEach, vi } from 'vitest'
import { flushPromises, VueWrapper } from '@vue/test-utils'
import ItemDetailView from '@/views/ItemDetailView.vue'
import { mountView, mockFetch, ok, makeUser, textError, sentBody, type FakeResponse } from './helpers'

afterEach(() => {
    vi.unstubAllGlobals() // put the real fetch and confirm back
})

// create a test fake test item 
function makeItem(state: string, changes: object = {}) {
    return {
        id: 4, name: 'Zoom', description: null, provider: null, referenceNumber: 'Z-1', lifecycleState: state,
        categoryName: 'Software', departmentName: 'IT', responsibleUserId: 7, responsibleUserName: 'Ivan Ivanov',
        periodNumber: 2, startDate: '2026-01-01', expirationDate: '2026-12-01', cost: 300,
        ...changes,
    }
}

// function for the answers for everything the page loads
// the item and the lists in the tabs
// extra can add or replace answers
function itemAnswers(item: object, extra: Record<string, FakeResponse> = {}) {
    return {
        '/api/items/4': ok(item),
        '/api/users/employees': ok([{ id: 7, firstName: 'Ivan', lastName: 'Ivanov' }, { id: 8, firstName: 'Mila', lastName: 'Koleva' }]),
        '/api/items/4/requests': ok([]),
        '/api/items/4/periods': ok([
            { periodNumber: 1, startDate: '2025-01-01', expirationDate: '2026-01-01', cost: 250, createdAt: '2025-01-01T00:00:00', createdFromRequestId: null },
        ]),
        '/api/items/4/history': ok([]),
        '/api/items/4/documents': ok([{ id: 9, originalFileName: 'contract.pdf', sizeBytes: 2048, uploadedAt: '2026-01-01T00:00:00', uploadedByUserName: 'Ivan Ivanov' }]),
        ...extra,
    }
}

// find a button by its label
function buttonWithText(wrapper: VueWrapper, text: string) {
    return wrapper.findAll('button').find((button) => button.text() === text)
}

describe('ItemDetailView', () => {
    it('shows the item card', async () => {
        mockFetch(itemAnswers(makeItem('Active')))
        const { wrapper } = await mountView(ItemDetailView, '/items/4', makeUser('Employee', 7))

        expect(wrapper.find('.card-title').text()).toBe('Zoom')
        expect(wrapper.text()).toContain('$300.00')
    })

    it('lets the employee edit and renew an expiring item', async () => {
        mockFetch(itemAnswers(makeItem('ExpiringSoon')))
        const { wrapper } = await mountView(ItemDetailView, '/items/4', makeUser('Employee', 7))

        expect(wrapper.find('a[href="/items/4/edit"]').exists()).toBe(true)
        expect(wrapper.find('a[href="/items/4/renew"]').exists()).toBe(true)
        expect(buttonWithText(wrapper, 'Cancel Item')).toBeUndefined() // only managers can cancel items
    })

    it('hides renew and links to the open request while one is in progress', async () => {
        const openRequest = { id: 31, status: 'PendingManagerApproval', proposedExpirationDate: '2027-12-01', proposedCost: 320, submittedAt: '2026-11-10T10:00:00' }
        mockFetch(itemAnswers(makeItem('ExpiringSoon'), { '/api/items/4/requests': ok([openRequest])}))
        const { wrapper } = await mountView(ItemDetailView, '/items/4', makeUser('Employee', 7))

        expect(wrapper.find('a.open-request').attributes('href')).toBe('/requests/31')
        expect(wrapper.find('a[href="/items/4/renew"]').exists()).toBe(false)
    })

    it('shows no actions for cancelled item', async () => {
        mockFetch(itemAnswers(makeItem('Cancelled')))
        const { wrapper } = await mountView(ItemDetailView, '/items/4', makeUser('Employee', 7))

        expect(wrapper.find('a[href="/items/4/edit"]').exists()).toBe(false)
        expect(wrapper.find('form.action-panel').exists()).toBe(false)
        expect(buttonWithText(wrapper, 'Cancel Item')).toBeUndefined() // only managers can cancel items
    })

    it('lets the manager cancel the item', async () => {
        mockFetch(itemAnswers(makeItem('Active'), { 'PUT /api/items/4/cancel': ok(makeItem('Cancelled'))}))
        vi.stubGlobal('confirm', () => true) // managers confirms cancelling the item
        const { wrapper } = await mountView(ItemDetailView, '/items/4', makeUser('DepartmentManager'))

        await buttonWithText(wrapper, 'Cancel Item')!.trigger('click')
        await flushPromises()

        expect(wrapper.find('.status-tab').text()).toBe('Cancelled')
    })

    it('shows message when item cannot be cancelled', async () => {
        const message = 'You cannot cancel the item because it has a renewal request open.'
        mockFetch(itemAnswers(makeItem('Active'), { 'PUT /api/items/4/cancel': textError(400, message)}))
        vi.stubGlobal('confirm', () => true) // managers confirms cancelling the item

        const { wrapper } = await mountView(ItemDetailView, '/items/4', makeUser('DepartmentManager'))

        await buttonWithText(wrapper, 'Cancel Item')!.trigger('click')
        await flushPromises()

        expect(wrapper.find('.error').text()).toBe(message)
    })

    it('lets manager reassign the item to another employee', async () => {
        const reassigned = makeItem('Active', { responsibleUserId: 8, responsibleUserName: 'Mila Koleva'})
        const fetchMock = mockFetch(itemAnswers(makeItem('Active'), { 'PUT /api/items/4/reassign': ok(reassigned)}))
        const { wrapper } = await mountView(ItemDetailView, '/items/4', makeUser('DepartmentManager'))

        await wrapper.find('form.action-panel select').setValue('8')
        await wrapper.find('form.action-panel').trigger('submit')
        await flushPromises()

        expect(sentBody(fetchMock, 'PUT', '/api/items/4/reassign')).toEqual({responsibleUserId:8})
        expect(wrapper.text()).toContain('Mila Koleva')
    })
})