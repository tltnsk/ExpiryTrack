import { describe, it, expect, afterEach, vi } from 'vitest'
import { flushPromises, type VueWrapper } from '@vue/test-utils'
import CategoriesCreateView from '@/views/CategoriesCreateView.vue'
import CategoriesEditView from '@/views/CategoriesEditView.vue'
import { mountView, mockFetch, ok, textError, validationError, makeUser, sentBody } from './helpers'

afterEach(() => {
    vi.unstubAllGlobals() // put the real fetch back
})

// submits the form and waits until the fetch calls are finished
async function submit(wrapper: VueWrapper) {
    await wrapper.find('form').trigger('submit')
    await flushPromises()
}

const admin = makeUser('Administrator')

describe('CategoriesCreateView', () => {
    it('sends the new category and goes back to the list', async () => {
        const fetchMock = mockFetch({ 'POST /api/categories': ok({ id: 3})})
        const { wrapper, router } = await mountView(CategoriesCreateView, '/categories/new', admin)

        await wrapper.find('input').setValue('Licenses')
        await wrapper.find('input[type="number"').setValue('45')
        await submit(wrapper)

        expect(sentBody(fetchMock, 'POST', '/api/categories')).toMatchObject({ name: 'Licenses', warningPeriodDays: 45 })
        expect(router.currentRoute.value.path).toBe('/categories')
    })

    it('shows the message when the name already exists', async () => {
        mockFetch({ 'POST /api/categories': textError(409, 'A category with that name already exists.')})
        const { wrapper, router } = await mountView(CategoriesCreateView, '/categories/new', admin)

        await submit(wrapper)

        expect(wrapper.find('.error').text()).toBe('A category with that name already exists.')
        expect(router.currentRoute.value.path).toBe('/categories/new') // stays on the form
    })

    it('shows validation messages', async () => {
        mockFetch({ 'POST /api/categories': validationError({Name: ['The Name field is required.']})})
        const { wrapper } = await mountView(CategoriesCreateView, '/categories/new', admin)

        await submit(wrapper)

        expect(wrapper.find('.error').text()).toBe('The Name field is required.')
    })
})

describe('CategoriesEditView', () => {
    const category = { id: 2, name: 'Software', description: 'Apps', warningPeriodDays: 30, requiresFinancialReview: false, financeReviewThreshold: null, isActive: true }

    it('fills the form with the category and saves the changes', async () => {
        const fetchMock = mockFetch({ '/api/categories/2': ok(category), 'PUT /api/categories/2': ok(category) })
        const { wrapper, router } = await mountView(CategoriesEditView, '/categories/2/edit', admin)

        const nameInput = wrapper.find('input')
        expect((nameInput.element as HTMLInputElement).value).toBe('Software')

        await nameInput.setValue('Software licenses')
        await submit(wrapper)

        expect(sentBody(fetchMock, 'PUT', '/api/categories/2')).toMatchObject({ name: 'Software licenses' })
        expect(router.currentRoute.value.path).toBe('/categories')
    })

    it('says when the category does not exist', async () => {
        mockFetch({ '/api/categories/2': textError(404, '')})
        const { wrapper } = await mountView(CategoriesEditView, '/categories/2/edit', admin)

        expect(wrapper.find('.error').text()).toBe('Category not found.')
        expect(wrapper.find('form').exists()).toBe(false)
    })
})