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

  return items.value.filter((item) => 
    item.name.toLowerCase().includes(text) ||
    (item.provider ?? '').toLowerCase().includes(text) 
  )
})

</script>

<template>
    <h2>Items</h2>
    <RouterLink v-if="auth.user?.role === 'Employee' || auth.user?.role === 'DepartmentManager'" to="/items/new">New Item</RouterLink>
    <RouterLink v-if="auth.user?.role === 'DepartmentManager'" to="/items/import">Import Items from Excel</RouterLink>

    <input v-model="search" type="search" placeholder="Search by name or provider" />
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