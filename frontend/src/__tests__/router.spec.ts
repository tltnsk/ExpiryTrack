import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia' 
import { useAuthStore  } from '@/stores/auth'
import router from '@/router'
import { mockFetch, makeUser, textError } from './helpers'

// checks where the router sends users based on their session and role
describe('router guards', () => {
    beforeEach(async () => {
        // give each test a new auth store and a known starting route
        setActivePinia(createPinia())
        // start every test from the same page
        mockFetch({})
        useAuthStore().user = makeUser('Administrator')
        await router.push('/dashboard')
    })

    afterEach(() => {
        // restore the original fetch
        vi.unstubAllGlobals()
    })

    // logs in as the given role and opens the path
    async function open(path: string, role: string | null){
        useAuthStore().user = role ? makeUser(role) : null
        await router.push(path)
        return router.currentRoute.value.path
    }

    it('sends a user who is not logged in to login page', async () => {
        mockFetch({ '/api/auth/me': textError(401, '')})
        expect(await open('/items', null)).toBe('/login')
    })

    it('loads the user from the cookie', async () => {
        const fetchMock = mockFetch({ '/api/auth/me': { status: 200, body: makeUser('Employee')}})
        useAuthStore().user = null
        await router.push('/items')

        expect(fetchMock).toHaveBeenCalledWith('/api/auth/me')
        expect(router.currentRoute.value.path).toBe('/items')
    })

    it('does not ask the server again after logout', async () => {
        const fetchMock = mockFetch({})
        const auth = useAuthStore()
        auth.checked = true
        auth.user = null

        await router.push('/items')

        expect(fetchMock).not.toHaveBeenCalledWith('/api/auth/me')
        expect(router.currentRoute.value.path).toBe('/login')
    })

    it('sends a logged in user away from login page', async () => {
        expect(await open('/login', 'Employee')).toBe('/dashboard')
    })

    it('only lets admin edit categories', async () => {
        expect(await open('/categories/1/edit', 'Employee')).toBe('/categories')
        expect(await open('/categories/1/edit', 'Administrator')).toBe('/categories/1/edit')
    })

    it('only lets admin open departments', async () => {
        expect(await open('/departments', 'DepartmentManager')).toBe('/dashboard')
        expect(await open('/departments', 'Administrator')).toBe('/departments')
    })

    it('only lets admin open users', async () => {
        expect(await open('/users', 'DepartmentManager')).toBe('/dashboard')
        expect(await open('/users', 'Administrator')).toBe('/users')
    })

    it('only lets manager import items', async () => {
        expect(await open('/items/import', 'Employee')).toBe('/items')
        expect(await open('/items/import', 'DepartmentManager')).toBe('/items/import')
    })

    it('only lets the department manager open my employees', async () => {
        expect(await open('my-employees', 'Employee')).toBe('/dashboard')
        expect(await open('my-employees', 'Administrator')).toBe('/dashboard')
        expect(await open('my-employees', 'DepartmentManager')).toBe('/my-employees')
    })
})