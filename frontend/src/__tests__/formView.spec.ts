import { describe, it, expect, afterEach, vi } from 'vitest'
import { flushPromises, type VueWrapper } from '@vue/test-utils'
import CategoriesCreateView from '@/views/CategoriesCreateView.vue'
import CategoriesEditView from '@/views/CategoriesEditView.vue'
import DepartmentsEditView from '@/views/DepartmentsEditView.vue'
import UsersCreateView from '@/views/UsersCreateView.vue'
import UsersEditView from '@/views/UsersEditView.vue'
import ChangePasswordView from '@/views/ChangePasswordView.vue'
import ItemCreateView from '@/views/ItemCreateView.vue'
import ItemEditView from '@/views/ItemEditView.vue'
import ItemImportView from '@/views/ItemImportView.vue'
import RenewalSubmitView from '@/views/RenewalSubmitView.vue'
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

describe('UsersEditView', () => {
    it('shows role and department as read only and saves name and email', async() => {
        const user = { id: 5, firstName: 'Ivan', lastName: 'Ivanov', email: 'ivan@test.com', role: 'Employee', departmentName: 'IT', isActive: true }
        const fetchMock = mockFetch({
            '/api/users/5': ok(user),
            'PUT /api/users/5': ok(user)
        })

        const { wrapper, router } = await mountView(UsersEditView, '/users/5/edit', admin)

        expect(wrapper.findAll('input[readonly]')).toHaveLength(2)

        await wrapper.findAll('input')[1]!.setValue('Petrov')
        await submit(wrapper)

        expect(sentBody(fetchMock, 'PUT', '/api/users/5')).toEqual({ firstName: 'Ivan', lastName: 'Petrov', email: 'ivan@test.com' })
        expect(router.currentRoute.value.path).toBe('/users')
    })
})

describe('ChangePasswordView', () => {
    async function fill(wrapper: VueWrapper, current: string, newPassword: string, confirm: string) {
        const inputs = wrapper.findAll('input[type="password"]')
        await inputs[0]!.setValue(current)
        await inputs[1]!.setValue(newPassword)
        await inputs[2]!.setValue(confirm)
    }

    it('does not send anything when the passwords dont match', async () => {
        const fetchMock = mockFetch({})
        const { wrapper } = await mountView(ChangePasswordView, '/change-password', admin)

        await fill(wrapper, 'oldpassword', 'newpassword', 'pass1234')
        await submit(wrapper)

        expect(wrapper.find('.error').text()).toContain('do not match')
        expect(fetchMock).not.toHaveBeenCalled()
    })

    it('shows a success message when the password was changed', async () => {
        mockFetch({ 'POST /api/auth/change-password': { status: 204 } })
        const { wrapper } = await mountView(ChangePasswordView, '/change-password', admin)

        await fill(wrapper, 'oldpass12', 'newpass12', 'newpass12')
        await submit(wrapper)

        expect(wrapper.text()).toContain('Your password has been changed.')
    })

    it('shows the message when the current password is wrong', async () => {
        mockFetch({ 'POST /api/auth/change-password': textError(400, 'Invalid password.') })
        const { wrapper } = await mountView(ChangePasswordView, '/change-password', admin)

        await fill(wrapper, 'wrong', 'newpass12', 'newpass12')
        await submit(wrapper)

        expect(wrapper.find('.error').text()).toBe('Invalid password.')
    })
})

describe('ItemCreateView', () => {
    const categories = [
        { id: 1, name: 'Software', warningPeriodDays: 30, isActive: true },
        { id: 2, name: 'Old', warningPeriodDays: 30, isActive: false  }]

    it('only shows active categories', async () => {
        mockFetch({ '/api/categories': ok(categories) })
        const { wrapper } = await mountView(ItemCreateView, '/items/new', makeUser('Employee'))

        const options = wrapper.find('select').text()
        expect(options).toContain('Software')
        expect(options).not.toContain('Old')
    })

    it('creates an item and opens it ', async () => {
        const fetchMock = mockFetch({ '/api/categories': ok(categories), 'POST /api/items': ok({id: 15 }) })
        const { wrapper, router } = await mountView(ItemCreateView, '/items/new', makeUser('Employee'))

        await wrapper.find('input').setValue('Zoom')
        await wrapper.find('select').setValue('1')
        await wrapper.findAll('input[type="date"]')[0]!.setValue('2026-11-01')
        await wrapper.findAll('input[type="date"]')[1]!.setValue('2027-11-01')
        await submit(wrapper)

        expect(sentBody(fetchMock, 'POST', '/api/items')).toMatchObject({
            name: 'Zoom', categoryId: 1, startDate: '2026-11-01', expirationDate: '2027-11-01',
        })

        expect(router.currentRoute.value.path).toBe('/items/15')
    })

    it('shows message', async () => {
        mockFetch({ '/api/categories': ok(categories), 'POST /api/items': textError(400, 'The expiration date must be a future date.')})
        const { wrapper } = await mountView(ItemCreateView, '/items/new', makeUser('Employee'))

        await submit(wrapper)

        expect(wrapper.find('.error').text()).toBe('The expiration date must be a future date.')
    })

    it('shows message when categories cannot be loaded', async () => {
        mockFetch({ '/api/categories': textError(500, '')})
        const { wrapper } = await mountView(ItemCreateView, '/items/new', makeUser('Employee'))

        expect(wrapper.find('.error').text()).toBe('Could not load the categories.')
    })
})

describe('ItemEditView', () => {
    const item = {
        id: 4, name: 'Zoom', description: null, provider: 'Zoom Inc', referenceNumber: null, lifecycleState: 'Active',
        categoryName: 'Software', departmentName: 'IT', responsibleUserName: 'Ivan Ivanov',
        periodNumber: 1, startDate: '2026-01-01', expirationDate: '2027-01-01', cost: null,
    }

    it('shows the dates and cost as read only', async () => {
        mockFetch({ '/api/items/4': ok(item)})
        const { wrapper } = await mountView(ItemEditView, '/items/4/edit', admin)

        const readonlyInputs = wrapper.findAll('input[readonly]')
        expect(readonlyInputs).toHaveLength(3)
    })

    it('saves changes and goes back to the item', async() => {
        const fetchMock = mockFetch({
            '/api/items/4': ok(item),
            'PUT /api/items/4': ok(item)
        })

        const { wrapper, router } = await mountView(ItemEditView, '/items/4/edit', makeUser('Employee'))

        await wrapper.find('input').setValue('Zoom Pro')
        await submit(wrapper)

        expect(sentBody(fetchMock, 'PUT', '/api/items/4')).toMatchObject({ name: 'Zoom Pro' })
        expect(router.currentRoute.value.path).toBe('/items/4')
    })
})

describe('RenewalSubmitView', () => {
    const item = { name: 'Zoom', expirationDate: '2026-12-01', lifecycleState: 'ExpiringSoon' }

    it('does not allow a date before the current expiration date', async () => {
        mockFetch({ '/api/items/4': ok(item) })
        const { wrapper } = await mountView(RenewalSubmitView, '/items/4/renew', makeUser('Employee'))

        expect(wrapper.find('input[type="date"]').attributes('min')).toBe('2026-12-01')
    })

    it('sends the request and opens it', async () => {
        const fetchMock = mockFetch({ '/api/items/4': ok(item), 'POST /api/items/4/requests': ok({ id: 30 }) })
        const { wrapper, router } = await mountView(RenewalSubmitView, '/items/4/renew', makeUser('Employee'))

        await wrapper.find('input[type="date"]').setValue('2027-12-01')
        await wrapper.find('textarea').setValue('We still use it every day.')
        await submit(wrapper)

        expect(sentBody(fetchMock, 'POST', '/api/items/4/requests')).toMatchObject({
            proposedExpirationDate: '2027-12-01', justification: 'We still use it every day.',
        })
        expect(router.currentRoute.value.path).toBe('/requests/30')
    })

    it('shows the message when there is already an open request', async () => {
        mockFetch({ '/api/items/4': ok(item), 'POST /api/items/4/requests': textError(400, 'Zoom already has an open renewal request.') })
        const { wrapper } = await mountView(RenewalSubmitView, '/items/4/renew', makeUser('Employee'))

        await submit(wrapper)

        expect(wrapper.find('.error').text()).toBe('Zoom already has an open renewal request.')
    })
})

describe('ItemImportView', () => {
    const manager = makeUser('DepartmentManager')
    const excelFile = new File(['test'], 'items.xlsx')

    // function that drops the file on the upload box
    async function dropFile(wrapper: VueWrapper) {
        await wrapper.find('.dropzone').trigger('drop', { dataTransfer: { files: [excelFile] } })
    }

    it('shows how many items were imported', async () => {
        mockFetch({ 'POST /api/items/import': ok({ createdCount: 12 }) })
        const { wrapper } = await mountView(ItemImportView, '/items/import', manager)

        await dropFile(wrapper)
        await wrapper.find('button.import-button').trigger('click')
        await flushPromises()

        expect(wrapper.text()).toContain('12 items imported')
    })

    it('lists the mistakes in the file', async () => {
        mockFetch({ 'POST /api/items/import': { status: 400, body: { errors: ['Row 2: Name is required.', 'Row 3: Start date is invalid.'] } } })
        const { wrapper } = await mountView(ItemImportView, '/items/import', manager)

        await dropFile(wrapper)
        await wrapper.find('button.import-button').trigger('click')
        await flushPromises()

        const errors = wrapper.findAll('li.error').map((li) => li.text())
        expect(errors).toEqual(['Row 2: Name is required.', 'Row 3: Start date is invalid.'])
    })
})