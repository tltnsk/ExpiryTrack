<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { formatMoney, formatDate, formatLabel } from '@/utils/format'
import { useSort } from '@/utils/sort'

interface Employee {
    id: number
    firstName: string
    lastName: string
    email: string
}

interface Item {
    id: number
    name: string
    responsibleUserId: number
    lifecycleState: string
    expirationDate: string
    cost: number | null
}

interface RenewalRequest {
    id: number
    itemId: number
    status: string
}

// one row of the table, an employee and the numbers about their items
interface EmployeeRow {
    id: number
    name: string
    email: string
    items: number
    itemList: Item[]
    expiringSoon: number
    expired: number
    openRequests: number
    totalCost: number
}

const employees = ref<Employee[]>([])
const items = ref<Item[]>([])
const requests = ref<RenewalRequest[]>([])
const loading = ref(true)
const errorMessage = ref('')
const search = ref('')

// requests that are not finished yet
const openStatuses = ['PendingFinancialReview', 'PendingManagerApproval', 'ClarificationRequired', 'Approved']

onMounted(async () => {
    const employeesResponse = await fetch('/api/users/employees')
    const itemsResponse = await fetch('/api/items')
    const requestsResponse = await fetch('/api/requests')

    if (employeesResponse.ok && itemsResponse.ok && requestsResponse.ok) {
        employees.value = await employeesResponse.json()
        items.value = await itemsResponse.json()
        requests.value = await requestsResponse.json()
    } else {
        errorMessage.value = 'Could not load your employees.'
    }
    loading.value = false
})

const rows = computed<EmployeeRow[]>(() => employees.value.map((employee) => {
    // cancelled items are not counted
    const theirItems = items.value.filter((item) => item.responsibleUserId === employee.id && item.lifecycleState !== 'Cancelled')
    const theirItemIds = theirItems.map((item) => item.id)

    return {
        id: employee.id,
        name: `${employee.firstName} ${employee.lastName}`,
        email: employee.email,
        items: theirItems.length,
        itemList: theirItems,
        expiringSoon: theirItems.filter((item) => item.lifecycleState === 'ExpiringSoon').length,
        expired: theirItems.filter((item) => item.lifecycleState === 'Expired').length,
        // a request belongs to whoever is responsible for its item now
        openRequests: requests.value.filter((request) => theirItemIds.includes(request.itemId) && openStatuses.includes(request.status)).length,
        totalCost: theirItems.reduce((sum, item) => sum + (item.cost ?? 0), 0),
    }
}))

// the employee whose items are shown under their row
const openEmployeeId = ref<number | null>(null)

// clicking the same employee again closes it
function toggle(id: number){
    openEmployeeId.value = openEmployeeId.value === id ? null : id
}

// ids of items that already have a request in progress
const itemsWithOpenRequest = computed(() => 
    requests.value.filter((request) => 
openStatuses.includes(request.status)).map((request) => request.itemId))

// expiring soon or expired, and no renewal request
function isAtRisk(item: Item) {
    return (item.lifecycleState === 'ExpiringSoon' || item.lifecycleState === 'Expired') && !itemsWithOpenRequest.value.includes(item.id)
}

// the numbers in the cards at the top
function total(key: 'items' | 'expiringSoon' | 'openRequests') {
    return rows.value.reduce((sum, row) => sum + row[key], 0)
}

// search by name or email
const filteredRows = computed(() => {
    const text = search.value.trim().toLowerCase()
    return rows.value.filter((row) => row.name.toLowerCase().includes(text) || row.email.toLowerCase().includes(text))
})

const { sorted, sortBy, arrow } = useSort(filteredRows, 'name')
</script>

<template>
    <h2>My employees</h2>

    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>
    <p v-else-if="employees.length === 0">There are no employees in your department.</p>

    <template v-else>
        <div class="panel">
            <p>You currently have:</p>
            <div class="summary">
                <div class="summary-card"><strong>{{ employees.length }}</strong><span>Employees</span></div>
                <div class="summary-card"><strong>{{ total('items') }}</strong><span>Items</span></div>
                <div class="summary-card"><strong>{{ total('expiringSoon') }}</strong><span>Expiring soon</span></div>
                <div class="summary-card"><strong>{{ total('openRequests') }}</strong><span>Open requests</span></div>
            </div>
            <input v-model="search" type="search" placeholder="Search employees by name or email" />
        </div>

        <div class="table-box">
            <table class="employees">
                <thead>
                    <tr>
                        <th class="sortable" @click="sortBy('name')">Employee {{ arrow('name') }}</th>
                        <th class="sortable" @click="sortBy('items')">Items {{ arrow('items') }}</th>
                        <th class="sortable" @click="sortBy('expiringSoon')">Expiring soon {{ arrow('expiringSoon') }}</th>
                        <th class="sortable" @click="sortBy('expired')">Expired {{ arrow('expired') }}</th>
                        <th class="sortable" @click="sortBy('openRequests')">Open requests {{ arrow('openRequests') }}</th>
                        <th class="sortable" @click="sortBy('totalCost')">Total cost {{ arrow('totalCost') }}</th>
                    </tr>
                </thead>
                <tbody>
                    <template v-for="row in sorted" :key="row.id">
                        <tr @click="toggle(row.id)">
                            <td>
                                <RouterLink :to="{ path: '/items', query: { responsible: row.name } }" class="name">{{ row.name }}</RouterLink>
                                <div>{{ row.email }}</div>
                            </td>
                            <td>{{ row.items }}</td>
                            <td><span v-if="row.expiringSoon > 0">{{ row.expiringSoon }}</span><template v-else>0</template></td>
                            <td><span v-if="row.expired > 0" >{{ row.expired }}</span><template v-else>0</template></td>
                            <td>{{ row.openRequests }}</td>
                            <td>{{ formatMoney(row.totalCost) }}</td>
                        </tr>
                        <!-- their items, shown when the row is clicked -->
                        <tr v-if="openEmployeeId === row.id">
                            <td colspan="6">
                                <p v-if="row.itemList.length === 0">No items.</p>
                                <table v-else>
                                    <tbody>
                                        <tr v-for="item in row.itemList" :key="item.id">
                                            <td><RouterLink :to="`/items/${item.id}`">{{ item.name }}</RouterLink></td>
                                            <td><span class="state" :class="item.lifecycleState">{{ formatLabel(item.lifecycleState) }}</span></td>
                                            <td>{{ formatDate(item.expirationDate) }}</td>
                                            <td>{{ formatMoney(item.cost) }}</td>
                                            <td><span v-if="isAtRisk(item)">No renewal request</span></td>
                                        </tr>
                                    </tbody>
                                </table>
                                <RouterLink :to="{ path: '/items', query: { responsible: row.name } }" class="all-items">Open in Items →</RouterLink>
                            </td>
                        </tr>
                    </template>
                </tbody>
            </table>
        </div>
    </template>
</template>

<style scoped>
.panel {
    margin-bottom: 16px;
    padding: 16px 20px;
    border: 1px solid var(--border);
    border-radius: 12px;
}

.summary {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 12px;
    margin: 16px 0 24px;
}

.summary-card {
    display: flex;
    gap: 6px;
    padding: 14px 16px;
    border: 1px solid var(--border);
    background-color: #eceef1;
}

.table-box {
    background: white;
    border: 1px solid #eceef1;
    border-radius: 12px;
    overflow: hidden;
}

.employees {
    margin: 0;
}

.employees th,
.employees td {
    padding: 14px 16px;
    border: none;
    border-bottom: 1px solid #eceef1;
}

.employees th {
    background: #f9fafb;
}

.employees tbody tr:last-child td {
    border-bottom: none;
}

.name {
    font-weight: 600;
    color: inherit;
    text-decoration: none;
}

.name:hover {
    text-decoration: underline;
}

@media (max-width: 700px) {
    .summary {
        grid-template-columns: 1fr 1fr;
    }
}
</style>