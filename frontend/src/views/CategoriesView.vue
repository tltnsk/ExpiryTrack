<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'

// CategoryResponse in C# 
interface Category {
  id: number
  name: string
  description: string | null
  warningPeriodDays: number
  requiresFinancialReview: boolean
  financeReviewThreshold: number | null
  isActive: boolean
}

const categories = ref<Category[]>([])
const auth = useAuthStore()

const loading = ref(true)
const errorMessage = ref('')

// fetch categories from the backend 
onMounted(async () => {
  const response = await fetch('/api/categories')
  if (response.ok) {
    categories.value = await response.json()
  } else {
    errorMessage.value = `Could not load categories (error ${response.status}).`
  }
  loading.value = false
})
</script>

<template>
  <h2>Categories</h2>

  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>
  <p v-else-if="categories.length === 0">No categories yet.</p>

  <table v-else>
    <thead>
      <tr>
        <th>Name</th>
        <th>Description</th>
        <th>Warning period (days)</th>
        <th>Financial review</th>
        <th>Threshold</th>
        <th>Active</th>
        <th v-if="auth.user?.role === 'Administrator'"></th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="category in categories" :key="category.id">
        <td>{{ category.name }}</td>
        <td>{{ category.description ?? '-' }}</td>
        <td>{{ category.warningPeriodDays }}</td>
        <td>{{ category.requiresFinancialReview ? 'Yes' : 'No' }}</td>
        <td>{{ category.financeReviewThreshold ?? '-' }}</td>
        <td>{{ category.isActive ? 'Yes' : 'No' }}</td>
        <td v-if="auth.user?.role === 'Administrator'">
          <RouterLink :to="`/categories/${category.id}/edit`">Edit</RouterLink>
        </td>
      </tr>
    </tbody>
  </table>
</template>