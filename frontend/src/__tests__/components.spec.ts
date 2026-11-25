import { describe, it, expect, vi, afterEach } from 'vitest'
import { flushPromises } from '@vue/test-utils'
import App from '@/App.vue'
import { useAuthStore } from '@/stores/auth'
import { mountView, mockFetch, ok, makeUser } from './helpers'

afterEach(() => {
    vi.unstubAllGlobals()
})

describe('App (the sidebar)', () => {
    it('shows the initials and the number of unread notifications', async () => {
        mockFetch({ '/api/notifications/unread-count': ok(4) })
        const { wrapper } = await mountView(App, '/dashboard', makeUser('Employee'))

        expect(wrapper.find('.avatar').text()).toBe('AT') 
        expect(wrapper.find('.badge').text()).toBe('4')
    })

    it('shows the Users link only to the admin', async () => {
        mockFetch({ '/api/notifications/unread-count': ok(0) })

        const admin = await mountView(App, '/dashboard', makeUser('Administrator'))
        expect(admin.wrapper.find('a[href="/users"]').exists()).toBe(true)

        const employee = await mountView(App, '/dashboard', makeUser('Employee'))
        expect(employee.wrapper.find('a[href="/users"]').exists()).toBe(false)
    })

    it('logs out and goes to the login page', async () => {
        mockFetch({ 'POST /api/auth/logout': ok(), '/api/notifications/unread-count': ok(0) })
        const { wrapper, router } = await mountView(App, '/dashboard', makeUser('Employee'))

        await wrapper.find('#logout-btn').trigger('click')
        await flushPromises()

        expect(useAuthStore().user).toBeNull()
        expect(router.currentRoute.value.path).toBe('/login')
    })

    it('shows the breadcrumb and the quick actions for the role', async () => {
        mockFetch({ '/api/notifications/unread-count': ok(0) })
        const { wrapper } = await mountView(App, '/items/5/edit', makeUser('DepartmentManager'))

        expect(wrapper.find('.breadcrumb').text()).toContain('Items')
        expect(wrapper.find('.breadcrumb .current').text()).toBe('Edit')
        expect(wrapper.find('a.quick-action[href="/items/import"]').exists()).toBe(true)
    })
})
