<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { formatDate, formatLabel} from '@/utils/format'

// ItemResponse 
interface Item {
    id: number
    name: string
    categoryName: string
    responsibleUserName: string
    lifecycleState: string
    expirationDate: string
    cost: number | null
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
</script>

<template>
    <h2>Items</h2>
    <RouterLink v-if="auth.user?.role === 'Employee' || auth.user?.role === 'DepartmentManager'" to="/items/new">New Item</RouterLink>

    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage"> {{ errorMessage }}</p>
    <p v-else-if="items.length===0">No items.</p>

    <table v-else>
        <thead>
            <tr>
              <th>Name</th>
              <th>Category</th>
              <th>Responsible</th>
              <th>Expires</th>
              <th>Cost</th>
              <th>State</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="item in items" :key="item.id">
                <td>
                    <RouterLink :to="`/items/${item.id}`">{{ item.name }}</RouterLink></td>
                <td>{{ item.categoryName }}</td>
                <td>{{ item.responsibleUserName }}</td>
                <td>{{ formatDate(item.expirationDate) }}</td>
                <td>{{ item.cost ?? '-' }}</td>
                <td>
                <span class="state" :class="item.lifecycleState">{{ formatLabel(item.lifecycleState) }}</span>
                </td>
            </tr>
        </tbody>
    </table>
</template>
<style scoped>
.state {
  padding: 2px 8px;
  border-radius: 3px;
  font-size: 0.85em;
}
.Active {
  background: #349a54;
  color: white;
}
.ExpiringSoon {
  background: #d54747;
  color: white;
}
.Expired {
  background: #fbaa29;
  color: white;
}
.Cancelled {
  background: #343131;
  color: white;
}
.error {
  color: #c53030;
}
</style>