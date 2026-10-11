<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'

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
    <RouterLink v-if="auth.user?.role === 'Employee'" to="/items/new">New Item</RouterLink>

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
                <td>{{ item.expirationDate }}</td>
                <td>{{ item.cost ?? '-' }}</td>
                <td>
                <span>{{ item.lifecycleState }}</span>
                </td>
            </tr>
        </tbody>
    </table>
</template>