<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { formatDate, formatLabel, formatMoney} from '@/utils/format'

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

const stateFilter = ref('')
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

function resetFilters() {
  search.value = ''
  stateFilter.value = ''
  categoryFilter.value = ''
  responsibleFilter.value = ''
  expiresFrom.value = ''
  expiresTo.value = ''
}

</script>

<template>
    <h2>Items</h2>
    <button v-if="auth.user?.role === 'Employee' || auth.user?.role === 'DepartmentManager'" class="add-btn"><RouterLink  to="/items/new" class="add-link">+ New Item</RouterLink></button>
    <button v-if="auth.user?.role === 'DepartmentManager'" class="add-btn"><RouterLink to="/items/import" class="add-link">Import Items from Excel</RouterLink></button>

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


    <table v-else>
        <thead>
            <tr>
              <th>Name</th>
              <th>Category</th>
              <th>Responsible Employee</th>
              <th>Expires</th>
              <th>Provider</th>
              <th>Cost</th>
              <th>State</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="item in filteredItems" :key="item.id">
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
</template>
<style scoped>

</style>