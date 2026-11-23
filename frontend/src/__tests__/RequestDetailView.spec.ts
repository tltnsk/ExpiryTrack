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
        periodExpirationDate: '2026-12-01', periodCost: 300,
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

// find a button by its label
function buttonWithText(wrapper: VueWrapper, text: string) {
    return wrapper.findAll('button').find((button) => button.text() === text)
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
        // the comparison table comes first, the history table second
        expect(wrapper.findAll('tbody')[1]?.text()).toContain('Submitted')
        expect(wrapper.find('.compare').text()).toContain('+7%')
    })

    it('says when the request does not exist', async () => {
        mockFetch({ '/api/requests/31': textError(404, '') })
        const { wrapper } = await openRequest('Employee')
        expect(wrapper.find('.error').text()).toBe('Request not found.')
    })
})

describe('RequestDetailView actions per role', () => {
    it('lets finance officer send a recommendation', async () => {
        const fetchMock = mockFetch({
            '/api/requests/31': ok(makeRequest('PendingFinancialReview', { needsFinancialReview: true})),
            'POST /api/requests/31/finance-review': ok(),
        })

        const { wrapper } = await openRequest('FinanceOfficer')

        await wrapper.find('select').setValue('Approve')
        await wrapper.find('textarea').setValue('Within budget.')
        await wrapper.find('form').trigger('submit')
        await flushPromises()

        expect(sentBody(fetchMock, 'POST', '/api/requests/31/finance-review')).toEqual({ recommendation: 'Approve', comment: 'Within budget.'})
    })

    it('lets manager approve with a comment', async () => {
        const fetchMock = mockFetch({
            '/api/requests/31': ok(makeRequest('PendingManagerApproval')),
            'POST /api/requests/31/approve': ok(),
        })

        const { wrapper } = await openRequest('DepartmentManager')

        await wrapper.find('textarea').setValue('Looks good.')
        await buttonWithText(wrapper, 'Approve')!.trigger('click')
        await flushPromises()

        expect(sentBody(fetchMock, 'POST', '/api/requests/31/approve')).toEqual({ comment: 'Looks good.'})
    })

    it('shows the message when someone else changed the request at the same time', async () => {
        const message = 'Someone else changed this request at the same time. Reload the page and try again.'
        mockFetch({
            '/api/requests/31': ok(makeRequest('PendingManagerApproval')),
            'POST /api/requests/31/reject': textError(400, message),
        })

        const { wrapper } = await openRequest('DepartmentManager')

        await buttonWithText(wrapper, 'Reject')!.trigger('click')
        await flushPromises()

        expect(wrapper.find('.action-panel .error').text()).toBe(message)
    })

    it('shows the employee the question and sends the answer', async () => {
        const waitingForAnswer = makeRequest('ClarificationRequired', {
            actions: [action('ClarificationRequested', 'PendingManagerApproval', 'ClarificationRequired', 'Why is it more expensive?')],
        })
        const fetchMock = mockFetch({
            '/api/requests/31': ok(waitingForAnswer),
            'POST /api/requests/31/provide-clarification': ok(),
        })

        const { wrapper } = await openRequest('Employee')

        expect(wrapper.find('.question').text()).toContain('Why is it more expensive?')

        await wrapper.find('textarea').setValue('Prices went up.')
        await wrapper.find('form').trigger('submit')
        await flushPromises()

        expect(sentBody(fetchMock, 'POST', '/api/requests/31/provide-clarification')).toEqual({ answer: 'Prices went up.'})
    })

    it('lets the employee complete an approved renewal', async () => {
        const fetchMock = mockFetch({
            '/api/requests/31': ok(makeRequest('Approved')),
            'POST /api/requests/31/complete-renewal': ok(),
        })

        const { wrapper } = await openRequest('Employee')

        await wrapper.find('form').trigger('submit')
        await flushPromises()

        expect(fetchMock).toHaveBeenCalledWith('/api/requests/31/complete-renewal', { method: 'POST' })
    })
})