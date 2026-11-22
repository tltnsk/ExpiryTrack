import { describe, it, expect, afterEach, vi } from 'vitest'
import { flushPromises, type VueWrapper } from '@vue/test-utils'
import RequestDetailView from '@/views/RequestDetailView.vue'
import { mountView, mockFetch, ok, makeUser, textError, sentBody} from './helpers'

afterEach(() => {
    vi.unstubAllGlobals() // put the real fetch and confirm back
})

// one step in the request history
function action(type: string, fromStatus: string | null, toStatus: string, comment: string | null = null) {
    return { type, fromStatus, toStatus, recommendation: null, comment, createdAt: '2026-11-10T10:00:00:00', actorUserId: 1, actorUserName: 'Ana Test'}
}

// create a test request
function makeRequest(status: string, changes: object = {}) {
    return {
        id: 31, itemId: 4, itemName: 'Zoom', status, needsFinancialReview: false,
        proposedExpirationDate: '2027-12-01', proposedCost: 320, justification: 'We use it every day.',
        submittedAt: '2026-11-10T10:00:00', decidedAt: null, completedAt: null, periodId: 2, requestedByUserId: 7,
        actions: [action('Submitted', null, 'PendingManagerApproval')],
        ...changes,
    }
}

// opens request 31 as the given role
async function openRequest(role: string) {
    return mountView(RequestDetailView, '/requests/31', makeUser(role))
}

// the class of each step in the progress bar
function stepClasses(wrapper: VueWrapper) {
    return wrapper.findAll('.stepper li').map((li) => li.classes()[0])
}

describe('RequestDetailView progress bar', () => {
    it('has 3 steps when no financial review is needed', async () => {
        mockFetch({ '/api/requests/31': ok(makeRequest('PendingManagerApproval'))})
        const { wrapper } = await openRequest('Employee')

        expect(stepClasses(wrapper)).toEqual(['done', 'current', 'pending'])
    })

    it('has 4 steps when no financial review is needed', async () => {
        mockFetch({ '/api/requests/31': ok(makeRequest('PendingFinancialReview', { needsFinancialReview: true}))})
        const { wrapper } = await openRequest('Employee')

        expect(stepClasses(wrapper)).toEqual(['done', 'current', 'pending', 'pending'])
    })
    
    it('marks the last step as success when the renewal is completed', async () => {
        mockFetch({ '/api/requests/31': ok(makeRequest('Completed'))})
        const { wrapper } = await openRequest('Employee')

        expect(stepClasses(wrapper)).toEqual(['done', 'done', 'success'])
    })
})

describe('RequestDetailView card', () => {
    it('shows the request details and history', async () => {
        mockFetch({ '/api/requests/31': ok(makeRequest('PendingManagerApproval'))})
        const { wrapper } = await openRequest('Employee')

        expect(wrapper.find('.card-title').text()).toBe('Zoom')
        expect(wrapper.text()).toContain('$320.00')
        expect(wrapper.find('a.item-button').attributes('href')).toBe('/items/4')
        expect(wrapper.find('tbody').text()).toContain('Submitted')
    })

    it('says when the request does not exist', async () => {
        mockFetch({ '/api/requests/31': textError(404, '') })
        const { wrapper } = await openRequest('Employee')
        expect(wrapper.find('.error').text()).toBe('Request not found.')
    })
})