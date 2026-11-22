import { describe, it, expect, afterEach, vi } from 'vitest'
import { flushPromises, type VueWrapper } from '@vue/test-utils'
import CategoriesCreateView from '@/views/CategoriesCreateView.vue'
import CategoriesEditView from '@/views/CategoriesEditView.vue'
import DepartmentsEditView from '@/views/DepartmentsEditView.vue'
import UsersCreateView from '@/views/UsersCreateView.vue'
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
        await wrapper.find('input[type="number"]').setValue('45')
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

describe('DepartmentsEditView', () => {
    const department = { id: 1, name: 'IT', isActive: true, managerId: null, managerName: null }
    const users = [
        { id: 3, firstName: 'Maria', lastName: 'Petrova', role: 'DepartmentManager', departmentId: 1, isActive: true },
        { id: 4, firstName: 'Ivan', lastName: 'Ivanov', role: 'Employee', departmentId: 1, isActive: true },
    ]

    it('only shows managers of current department', async () => {
        mockFetch({ '/api/departments/1': ok(department), '/api/users': ok(users) })
        const { wrapper } = await mountView(DepartmentsEditView, '/departments/1/edit', admin)

        const options = wrapper.find('select').text()
        expect(options).toContain('Maria Petrova')
        expect(options).not.toContain('Ivan Ivanov') // an employee, not a manager
    })

    it('saves the name and the manager', async() => {
        const fetchMock = mockFetch({
            '/api/departments/1': ok(department),
            '/api/users': ok(users),
            'PUT /api/departments/1': ok(department),
            'PUT /api/departments/1/manager': ok(department),
        })

        const { wrapper, router } = await mountView(DepartmentsEditView, '/departments/1/edit', admin)

        await wrapper.find('select').setValue('3')
        await submit(wrapper)

        expect(sentBody(fetchMock, 'PUT', '/api/departments/1/manager')).toEqual({ userId: 3 })
        expect(router.currentRoute.value.path).toBe('/departments')
    })

    it('does not save manager when name is refused', async() => {
        const fetchMock = mockFetch({
            '/api/departments/1': ok(department),
            '/api/users': ok(users),
            'PUT /api/departments/1': textError(409, 'A department with this name already exists.'),
        })

        const { wrapper, router } = await mountView(DepartmentsEditView, '/departments/1/edit', admin)

        await wrapper.find('select').setValue('3')
        await submit(wrapper)

        expect(wrapper.find('.error').text()).toBe('A department with this name already exists.')
        expect(sentBody(fetchMock, 'PUT', '/api/departments/1/manager')).toBeUndefined()
        expect(router.currentRoute.value.path).toBe('/departments/1/edit')
    })
})

describe('UsersCreateView', () => {
    const departments = [{ id: 1, name: 'IT', isActive: true, managerId: null, managerName: null }]

    it('sends the new user and goes back to the list', async () => {
        const fetchMock = mockFetch({ '/api/departments': ok(departments), 'POST /api/users': ok({id: 9})})
        const { wrapper, router } = await mountView(UsersCreateView, '/users/new', admin)
        const inputs = wrapper.findAll('input')
        const selects = wrapper.findAll('select')

        await inputs[0]!.setValue('Ivan')
        await inputs[1]!.setValue('Ivanov')
        await inputs[2]!.setValue('ivan@test.com')
        await inputs[3]!.setValue('password1')
        await selects[0]!.setValue('Employee')
        await selects[1]!.setValue('1')
        await submit(wrapper)

        expect(sentBody(fetchMock, 'POST', '/api/users')).toEqual({
            firstName: 'Ivan', lastName: 'Ivanov', email: 'ivan@test.com',
            role: 'Employee', departmentId: 1, password: 'password1',
        })
        expect(router.currentRoute.value.path).toBe('/users')
    })

    it('marks the department as required only for employees and managers', async () => {
        mockFetch({ '/api/departments': ok(departments)})
        const { wrapper } = await mountView(UsersCreateView, '/users/new', admin)
        const roleSelect = wrapper.findAll('select')[0]!

        expect(wrapper.findAll('.required')).toHaveLength(5)

        await roleSelect.setValue('Employee')
        expect(wrapper.findAll('.required')).toHaveLength(6)
    })

    it('shows a message when an email exists', async () => {
        mockFetch({ '/api/departments': ok(departments), 'POST /api/users': textError(409, 'A user with this email already exists.') })
        const { wrapper } = await mountView(UsersCreateView, '/users/new', admin)

        await submit(wrapper)

        expect(wrapper.find('.error').text()).toBe('A user with this email already exists.')
    })
})