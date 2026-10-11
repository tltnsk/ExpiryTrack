<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

interface Item {
    id: number
    name: string
    description: string | null
    provider: string | null
    referenceNumber: string | null
    lifecycleState: string
    categoryName: string
    departmentName: string
    responsibleUserName: string
    periodNumber: string
    startDate: string 
    expirationDate: string
    cost: number
}

const route = useRoute()
const auth = useAuthStore()
const id = route.params.id

const item = ref<Item | null>(null)
const loading = ref(true)
const errorMessage = ref('')
const cancelError = ref('')

onMounted(async () => {
    const response = await fetch(`/api/items/${id}`)
    if (response.ok) {
        item.value = await response.json()
    } else if (response.status === 404 || response.status === 400) {
    errorMessage.value = 'Item not found.'
    } else {
        errorMessage.value = `Could not load item (error ${response.status}).`
    }
    loading.value = false
})

async function cancelItem() {
    cancelError.value = ''
    const result = confirm('Are you sure you want to cancel this item?')
    if (result) {
        const response = await fetch(`/api/items/${id}/cancel`, {
            method: 'PUT'
        })

        if (response.status === 200) {
            item.value = await response.json()
        } else if (response.status === 400) {
            cancelError.value = await response.text()
        } else {
            cancelError.value = `Could not cancel the item (error ${response.status})`
        }
    }
}
</script>

<template>
    <RouterLink to="/items">Back to Items</RouterLink>

    <p v-if="loading">Loading...</p>

    <p v-else-if="errorMessage"> {{  errorMessage }}</p>

    <div v-else-if="item">
        <h2>{{ item.name }}</h2>
        <span> {{ item.lifecycleState }}</span>

        <h3>Details</h3>
        <dl>
            <dt>Description</dt>
            <dd>{{ item.description ?? '-' }}</dd>
            <dt>Category</dt>
            <dd>{{ item.categoryName }}</dd>
            <dt>Department</dt>
            <dd>{{ item.departmentName }}</dd>
            <dt>Responsible</dt>
            <dd>{{ item.responsibleUserName }}</dd>
            <dt>Provider</dt>
            <dd>{{ item.provider ?? '-' }}</dd>
            <dt>Reference Number</dt>
            <dd>{{ item.referenceNumber ?? '-' }}</dd>
        </dl>

        <h3>Current Period</h3>
        <dl>
            <dt>Period</dt>
            <dd>{{ item.periodNumber }}</dd>
            <dt>Start date</dt>
            <dd>{{ item.startDate }}</dd>
            <dt>Expiration Date</dt>
            <dd>{{ item.expirationDate }}</dd>
            <dt>Cost</dt>
            <dd>{{ item.cost ?? '-' }}</dd>
        </dl>

        <button v-if="auth.user?.role === 'DepartmentManager' && (item.lifecycleState === 'Active' || item.lifecycleState === 'ExpiringSoon')"  @click="cancelItem">Cancel Item</button>
        <p v-if="cancelError">{{ cancelError }}</p>
    </div>
</template>

<style scoped>
</style>