import { vi } from 'vitest'
import type { Component } from 'vue'
import { createRouter, createMemoryHistory } from 'vue-router'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore, type User } from '@/stores/auth'

 // backend answer
 // a status code and a JSON body or a text message
 export interface FakeResponse {
    status: number
    body?: unknown
    text?: string
 }

 // 200 - oK
 export function ok(body: unknown = null): FakeResponse {
    return { status: 200, body }
 }

 // an error with a plain text message in the controllers
 export function textError(status: number, text: string): FakeResponse {
    return { status, text}
 }

 // an error with a plain text message
 export function validationError(errors: Record<string, string[]>): FakeResponse {
    return { status: 400, body: { errors } }
 }
// a test user with the given role
export function makeUser(role: string, id = 1): User {
    return { id, firstName: 'Ana', lastName: 'Test', email: 'ana@test.com', role, departmentName: 'IT' }
}

// this is a function that replaces fetch, so the tests don't call the backend
// for GET, we can use just the url as the key 
// for other requests, include the method, like "POST /api/items"
// if there's no matching answer, return 404 
export function mockFetch(answers: Record<string, FakeResponse>) {
    const fakeFetch = vi.fn(async (url: string, options?: RequestInit) => {
        const method = options?.method ?? 'GET'
        let answer = answers[`${method} ${url}`]
        if (!answer && method === 'GET') answer = answers[url]
        if (!answer) answer = { status: 404, text: 'Not found' }
        return {
            ok: answer.status >= 200 && answer.status < 300,
            status: answer.status,
            // the view checks this header to decide whether to read JSON or text
            headers: new Headers({ 'Content-Type': answer.text !== undefined ? 'text/plain' : 'application/json' }),
            json: async () => answer.body,
            text: async () => answer.text ?? '',
        }
    })
    vi.stubGlobal('fetch', fakeFetch)
    return fakeFetch
}

// this function creates a router with the same paths as the real one, but every page is empty,
// this lets the view read route params like route.params.id 
export function makeRouter() {
    const empty = { render: () => null }
    const paths = [
        '/', '/login', '/dashboard', '/items', '/items/new', '/items/import', '/items/:id', '/items/:id/edit',
        '/items/:id/renew', '/categories', '/categories/new', '/categories/:id/edit', '/departments',
        '/departments/new', '/departments/:id/edit', '/users', '/users/new', '/users/:id/edit',
        '/requests', '/requests/:id', '/notifications', '/change-password', '/reports', '/my-employees'
    ]
    return createRouter({
        history: createMemoryHistory(),
        routes: paths.map((path) => ({ path, component: empty })),
    })
}

// this function opens a page as the given user and waits until its fetch calls are finished
export async function mountView(view: Component, path: string, user: User | null) {
    const pinia = createPinia()
    setActivePinia(pinia)
    useAuthStore().user = user

    const router = makeRouter()
    await router.push(path)
    await router.isReady()

    const wrapper = mount(view, { global: { plugins: [pinia, router] } })
    await flushPromises() // wait for onMounted to finish loading
    return { wrapper, router }
}

// what the page sent in the body of the request
export function sentBody(fetchMock: ReturnType<typeof mockFetch>, method: string, url: string) {
    const call = fetchMock.mock.calls.find(([u, o]) => u === url && o?.method === method)
    return call ? JSON.parse(String(call[1]?.body)) : undefined
}