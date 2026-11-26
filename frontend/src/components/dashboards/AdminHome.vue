<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { formatLabel} from '@/utils/format'
import { getBarChart, getStateCounts} from '@/utils/dashboard'
import type {  AdminDashboard } from '@/utils/dashboard'
import BarChart from '@/components/BarChart.vue'
import StateChart from '@/components/StateChart.vue'

interface User {
    id: number
    role: string
    departmentName: string | null
    isActive: boolean
}

interface Department {
    id: number
    name: string
    isActive: boolean
    managerName: string | null
}

interface Category {
    id: number
    isActive: boolean
    requiresFinancialReview: boolean
}

interface Check {
    ok: boolean
    text: string
    link: string
    linkText: string
}

const dashboard = ref<AdminDashboard | null>(null)
const users = ref<User[]>([])
const departments = ref<Department[]>([])
const categories = ref<Category[]>([])

const loading = ref(true)
const errorMessage = ref('')

const roles = ['Employee', 'DepartmentManager', 'FinanceOfficer', 'Administrator']

onMounted(async () => {
    const response = await fetch('/api/dashboard/admin')
    if (response.ok) {
        dashboard.value = await response.json()
    } else {
        errorMessage.value = 'Could not load the dashboard.'
    }

    const usersResponse = await fetch('/api/users')
    if (usersResponse.ok) users.value = await usersResponse.json()

    const departmentsResponse = await fetch('/api/departments')
    if (departmentsResponse.ok) departments.value = await departmentsResponse.json()

    const categoriesResponse = await fetch('/api/categories')
    if (categoriesResponse.ok) categories.value = await categoriesResponse.json()

    loading.value = false
})

const activeUsers = computed(() => users.value.filter((user) => user.isActive))
const activeDepartments = computed(() => departments.value.filter((department) => department.isActive))
const activeCategories = computed(() => categories.value.filter((category) => category.isActive))
const departmentsWithoutManager = computed(() => activeDepartments.value.filter((department) => department.managerName === null))


// how many active users have this role
function countRole(role: string) {
    return activeUsers.value.filter((user) => user.role === role).length
}

// department, its manager, employees and items
const departmentRows = computed(() => activeDepartments.value.map((department) => ({
    id: department.id,
    name: department.name,
    managerName: department.managerName,
    employees: activeUsers.value.filter((user) => user.role === 'Employee' && user.departmentName === department.name).length,
    items: dashboard.value?.itemsByDepartment.find((row) => row.departmentName === department.name)?.itemCount ?? 0,
})))

// things the admin should check because the renewal process cant work without them
const checks = computed(() => {
    const list: Check[] = []

    // every department needs a manager
    if (departmentsWithoutManager.value.length === 0) {
        list.push({ok: true, text: 'Every department has a manager', link: '', linkText: ''})
    }

    for (const department of departmentsWithoutManager.value) {
        list.push({ok: false, text: `${department.name} has no manager, so its renewal requests cannot be decided`,
            link: `/departments/${department.id}/edit`, linkText: 'Assign ->'
        })
    }

    // every department should have at least one employee
    const emptyDepartments = departmentRows.value.filter((row) => row.employees === 0)
    if (emptyDepartments.length === 0) {
        list.push({ok: true, text: 'Every department has at least one employee', link: '', linkText: ''})
    }
    for (const row of emptyDepartments) {
        list.push({ok: false, text: `${row.name} has no active employees`, link: '/users/new', linkText:'Add'})
    }

    //  requests that need a financial review need a finance officer
    if (countRole('FinanceOfficer') > 0) {
        list.push({ ok: true, text: 'There is an active finance officer', link: '', linkText: '' })
    } else {
        list.push({ ok: false, text: 'There is no active finance officer, so requests that need a financial review cannot continue',
            link: '/users/new', linkText: 'Add →' })
    }

    // items cannot be created without an active category
    if (activeCategories.value.length > 0) {
        list.push({ ok: true, text: `${activeCategories.value.length} active categories`, link: '', linkText: '' })
    } else {
        list.push({ ok: false, text: 'There are no active categories, so no items can be created',
            link: '/categories/new', linkText: 'Add →' })
    }

    return list
})

const stateCounts = computed(() => getStateCounts(dashboard.value, null, null, null))
const departmentChart = computed(() => getBarChart(dashboard.value, null, null, null))
</script>
<template>
    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error"> {{ errorMessage }}</p>
        <div v-else-if="dashboard" class="admin-home">
            <section class="box checklist">
                <h3>Setup checklist</h3>
                <div v-for="check in checks" :key="check.text" class="check" :class="{problem: !check.ok}">
                    <span class="check-icon">{{ check.ok ? '✓' : '!' }}</span>
                    <span class="check-text">{{ check.text }}</span>
                    <RouterLink v-if="!check.ok" :to="check.link" class="check-link">{{ check.linkText }}</RouterLink>
                </div>
            </section>
        <div class="top">
            <section class="box">
                <h3>Departments</h3>
                <p v-if="departmentRows.length === 0" class="empty">No departments.</p>
                <table v-else class="departments">
                    <thead>
                        <tr><th>Department</th><th>Manager</th><th>Employees</th><th>Items</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="row in departmentRows" :key="row.id">
                            <td><RouterLink :to="`/departments/${row.id}/edit`">{{ row.name }}</RouterLink></td>
                            <td>
                                <template v-if="row.managerName">{{ row.managerName }}</template>
                                <span v-else class="missing">No manager</span>
                            </td>
                            <td>{{ row.employees }}</td>
                            <td>{{ row.items }}</td>
                        </tr>
                    </tbody>
                </table>
            </section>

            <section class="box">
                <h3>Users by role</h3>
                <div v-for="role in roles" :key="role" class="role-row">
                    <span>{{ formatLabel(role) }}</span>
                    <strong>{{ countRole(role) }}</strong>
                </div>
            </section>
        </div>

        <div class="charts">
            <div class="chart-card">
                <h3>Items by state</h3>
                <StateChart :values="stateCounts" />
            </div>
            <div class="chart-card">
                <h3>{{ departmentChart.title }}</h3>
                <BarChart :labels="departmentChart.labels" :values="departmentChart.values" />
            </div>
        </div>
    </div>
</template>
<style scoped>
h3 {
    margin: 0 0 12px;
}

.charts {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
    margin-top: 24px;
}

.chart-card {
    min-width: 0;
    padding: 20px;
    background-color: #f1f5fe;
}

.top {
    display: grid;
    grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
    gap: 16px;
    align-items: start;
}

.box {
    padding: 16px 20px;
    border: 1px solid var(--border);
}

.departments {
    margin: 0;
    border: none;
}

.departments th,
.departments td {
    padding: 10px 8px;
    border: none;
    border-bottom: 1px solid #eceef1;
}

.departments tbody tr:last-child td {
    border-bottom: none;
}

.missing {
    color: var(--danger);
    font-weight: 600;
}

.role-row {
    display: flex;
    justify-content: space-between;
    padding: 10px 0;
    border-bottom: 1px solid #eceef1;
}

.role-row:last-child {
    border-bottom: none;
}

.empty {
    color: #6b6b6b;
}

.checklist {
    margin-bottom: 16px;
}

.check {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 8px 0;
    border-bottom: 1px solid #eceef1;
}

.check:last-child {
    border-bottom: none;
}

.check-text{
    flex:1;
}

.check-icon {
    width: 20px;
    font-weight: 700;
}

.check.problem .check-icon,
.check.problem .check-text {
    color: var(--danger);
}

.check-link {
    font-size: 14px;
    white-space: nowrap;
}

@media (max-width: 1000px) {
    .charts,
    .top {
        grid-template-columns: 1fr;
    }
}
</style>