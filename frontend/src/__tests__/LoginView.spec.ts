import { describe, it, expect, afterEach, vi } from 'vitest'
import { flushPromises } from '@vue/test-utils'
import { useAuthStore  } from '@/stores/auth'
import LoginView from '@/views/LoginView.vue'
import { mountView, mockFetch, ok, makeUser, textError, sentBody } from './helpers'

describe('LoginView', () => {
    afterEach(() => {
        vi.unstubAllGlobals()
    })

    // fills in the form and submits it
    async function login(wrapper: Awaited<ReturnType<typeof mountView>>['wrapper']) {
        await wrapper.find('input[type="email"]').setValue('ana@test.com')
        await wrapper.find('input[type="password"]').setValue('secret123')
        await wrapper.find('form').trigger('submit')
        await flushPromises()
    }

    it('saves the user and goes to the dashboard when login works', async () => {
        const user = makeUser('Employee')
        const fetchMock = mockFetch({ 'POST /api/auth/login': ok(user)})
        const { wrapper, router } = await mountView(LoginView, '/login', null)

        await login(wrapper)

        expect(sentBody(fetchMock, 'POST', '/api/auth/login')).toEqual({email: 'ana@test.com', password: 'secret123'})
        expect(useAuthStore().user).toEqual(user)
        expect(router.currentRoute.value.path).toBe('/dashboard')
    })

    it('shows a message when email or password is wrong', async () => {
        mockFetch({ 'POST /api/auth/login': textError(401, 'Invalid email or password.')})
        const { wrapper, router } = await mountView(LoginView, '/login', null)

        await login(wrapper)

        expect(wrapper.find('.error').text()).toBe('Invalid email or password.')
        expect(useAuthStore().user).toBeNull()
        expect(router.currentRoute.value.path).toBe('/login')
    })

    it('shows the error code for other errors', async () => {
        mockFetch({ 'POST /api/auth/login': textError(500, 'Something went wrong.')})
        const { wrapper } = await mountView(LoginView, '/login', null)

        await login(wrapper)

        expect(wrapper.find('.error').text()).toContain('error 500')
    })
})