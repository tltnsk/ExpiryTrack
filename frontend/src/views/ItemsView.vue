<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { formatDate, formatLabel, formatMoney} from '@/utils/format'
import { useRoute } from 'vue-router'
import { useSort } from '@/utils/sort'

// ItemResponse
interface Item {
    id: number
    name: string
    categoryName: string
    responsibleUserName: string
    lifecycleState: string
    expirationDate: string
    cost: number | null
    provider: string | null
}

const items = ref<Item[]>([])
const loading = ref(true)
const errorMessage = ref('')
const auth = useAuthStore()
const route = useRoute()

const stateFilter = ref(String(route.query.state ?? ''))
const categoryFilter = ref('')
const responsibleFilter = ref('')
const expiresFrom = ref('')
const expiresTo = ref('')

const categories = computed(() => [...new Set(items.value.map((item) => item.categoryName))].sort())
const employees = computed(() => [...new Set(items.value.map((item) => item.responsibleUserName))].sort())
const states = ['Active', 'ExpiringSoon', 'Expired', 'Cancelled']

onMounted(async() => {
    const response = await fetch('/api/items')
    if(response.ok) {
        items.value = await response.json()
    } else {
        errorMessage.value = `Could not load items (error ${response.status}).`
    }
    loading.value = false
})

const search = ref('')

const filteredItems = computed(() => {
  const text = search.value.trim().toLowerCase()

  return items.value.filter((item) => {
    const matchesSearch = item.name.toLowerCase().includes(text) ||
    (item.provider ?? '').toLowerCase().includes(text)

    if (!matchesSearch) return false

    if(stateFilter.value && item.lifecycleState !== stateFilter.value)
    return false

    if(categoryFilter.value && item.categoryName !== categoryFilter.value)
    return false

    if(responsibleFilter.value && item.responsibleUserName !== responsibleFilter.value)
    return false

    if (expiresFrom.value && item.expirationDate < expiresFrom.value) return false
    if (expiresTo.value && item.expirationDate > expiresTo.value) return false

    return true
  })
})

// sort the filtered items; at the start the soonest expiration is first
const { sorted, sortBy, arrow, sortKey, ascending } = useSort(filteredItems, 'expirationDate')

function resetFilters() {
  search.value = ''
  stateFilter.value = ''
  categoryFilter.value = ''
  responsibleFilter.value = ''
  expiresFrom.value = ''
  expiresTo.value = ''
}

// show 15 items per page
const pageSize = 15
const page = ref(1)

const totalPages = computed(() => Math.max(1, Math.ceil(filteredItems.value.length / pageSize)))

// show only the itemsof the current page
const pageItems = computed(() => {
  const start = (page.value - 1) * pageSize
  return sorted.value.slice(start, start + pageSize)
})

// go back to the first page when a filter changes
watch([search, stateFilter, categoryFilter, responsibleFilter, expiresFrom, expiresTo, sortKey, ascending], () => {
  page.value = 1
})
</script>

<template>
    <h2>Items</h2>
    <div class="page-top">
       <RouterLink v-if="auth.user?.role === 'Employee' || auth.user?.role === 'DepartmentManager'" to="/items/new" class="add-link">+ New Item</RouterLink>
       <RouterLink v-if="auth.user?.role === 'DepartmentManager'" to="/items/import" class="add-link">Import Items from Excel</RouterLink>
    </div>

    <div class="card filters">
      <input v-model="search" type="search" placeholder="Search by name or provider" />
       <select v-model="stateFilter">
        <option value="">All states</option>
        <option v-for="state in states" :key="state" :value="state">{{ formatLabel(state) }}</option>
       </select>

      <select v-model="categoryFilter">
        <option value="">All categories</option>
        <option v-for="category in categories" :key="category" :value="category">{{ category }}</option>
       </select>

      <select v-if="auth.user?.role !== 'Employee'" v-model="responsibleFilter">
        <option value="">All employees</option>
        <option v-for="employee in employees" :key="employee" :value="employee">{{ employee }}</option>
       </select>

       <label>Expires from <input v-model="expiresFrom" type="date"/></label>
       <label>Expires to <input v-model="expiresTo" type="date"/></label>

       <button type="button" @click="resetFilters">Reset</button>
    </div>

    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error"> {{ errorMessage }}</p>
    <p v-else-if="items.length===0">No items.</p>
    <p v-else-if="filteredItems.length===0">No items match your search.</p>

    <template v-else>
      <div class="table-box">
          <table>
            <thead>
                <tr>
                    <th class="sortable" @click="sortBy('name')">Name {{ arrow('name') }}</th>
                    <th class="sortable" @click="sortBy('categoryName')">Category {{ arrow('categoryName') }}</th>
                    <th class="sortable" @click="sortBy('responsibleUserName')">Responsible Employee {{ arrow('responsibleUserName') }}</th>
                    <th class="sortable" @click="sortBy('expirationDate')">Expires {{ arrow('expirationDate') }}</th>
                    <th class="sortable" @click="sortBy('provider')">Provider {{ arrow('provider') }}</th>
                    <th class="sortable" @click="sortBy('cost')">Cost {{ arrow('cost') }}</th>
                    <th class="sortable" @click="sortBy('lifecycleState')">State {{ arrow('lifecycleState') }}</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="item in pageItems" :key="item.id">
                    <td>
                        <RouterLink :to="`/items/${item.id}`">{{ item.name }}</RouterLink></td>
                    <td>{{ item.categoryName }}</td>
                    <td>{{ item.responsibleUserName }}</td>
                    <td>{{ formatDate(item.expirationDate) }}</td>
                    <td>{{ item.provider ?? '-'}}</td>
                    <td>{{ formatMoney(item.cost) }}</td>
                    <td>
                    <span class="state" :class="item.lifecycleState">{{ formatLabel(item.lifecycleState) }}</span>
                    </td>
                </tr>
            </tbody>
        </table>
      </div>
       <div v-if="totalPages > 1" class="pager">
        <button type="button" :disabled="page === 1" @click="page--" aria-label="Previous page">‹</button>
        <span>Page {{ page }} of {{ totalPages }}</span>
        <button type="button" :disabled="page === totalPages" @click="page++" aria-label="Next page">›</button>
      </div>
    </template>
</template>
<style scoped>
.table-box {
  background: white;
  border: 1px solid #eceef1;
  border-radius: 12px;
  overflow: hidden;
}

table {
  margin: 0;
  table-layout: fixed;
}

th {
  background: #f9fafb;
}

th:nth-child(1) {width: 22%}
th:nth-child(2) {width: 14%}
th:nth-child(3) {width: 16%}
th:nth-child(4) {width: 12%}
th:nth-child(5) {width: 14%}
th:nth-child(6) {width: 10%}
th:nth-child(7) {width: 12%}


.pager {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 12px;
  margin-top: 16px;
  color: #6b7280;
}

.pager button {
  margin: 0;
  padding: 8px 16px;
  color: var(--primary);
  background: rgba(255, 255, 255, 0.856);
  border: 1px solid rgba(235, 228, 228, 0.7);
  border-radius: 999px;
}

.add-link {
  padding: 8px 14px;
  border-radius: 8px;
  background: var(--primary);
  color: white;
  text-decoration: none;
  font-weight: 500;
}

.page-top {
  display: flex;
  gap: 10px;
  align-items: center;
  margin-bottom: 20px;
}
</style>
