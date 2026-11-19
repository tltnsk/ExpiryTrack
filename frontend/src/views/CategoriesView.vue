<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { formatMoney } from '@/utils/format'

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

const isAdmin = computed(() => auth.user?.role === 'Administrator')

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
  <div class="page-top">
    <h2>Categories</h2>
    <RouterLink v-if="isAdmin" to="/categories/new" class="new-button">New category</RouterLink>
   </div>

  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>
  <p v-else-if="categories.length === 0">No categories yet.</p>

  <div v-else class="table-box">
      <table>
    <thead>
      <tr>
        <th>Name</th>
        <th>Description</th>
        <th>Warning period (days)</th>
        <th>Financial review</th>
        <th>Threshold</th>
        <th>Active</th>
        <th v-if="isAdmin"></th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="category in categories" :key="category.id">
        <td class="name">{{ category.name }}</td>
        <td class="description">{{ category.description ?? '-' }}</td>
        <td>{{ category.warningPeriodDays }} days</td>
        <td>{{ category.requiresFinancialReview ? 'Required' : 'Not Required' }}</td>
        <td>{{ formatMoney(category.financeReviewThreshold) }}</td>
        <td>
          <span v-if="category.isActive" class="active-cat">Active</span>
          <span v-else class="inactive-cat">Inactive</span>
        </td>
        <td v-if="isAdmin" class="edit-cell">
          <RouterLink :to="`/categories/${category.id}/edit`" class="edit-link" :title="`Edit ${category.name}`" :aria-label="`Edit ${category.name}`">✎</RouterLink>
        </td>
      </tr>
    </tbody>
  </table>
  </div>
</template>
<style scoped>
.page-top{
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-top h2 {
  margin: 0;
}

.new-button {
  padding: 8px 14px;
  border-radius: 8px;
  background: var(--primary);
  color: white;
  text-decoration: none;
  font-weight: 500;
}

.table-box {
  background: white;
  border: 1px solid #eceef1;
  border-radius: 12px;
  overflow: hidden;
}

table {
  margin: 0;
}

th, td {
  padding: 18px 20px;
  border: none;
  border-bottom: 1px solid #eceef1;
}

th {
  background: #f9fafb;
}

th + th {
  border-left: 1px solid #eceef1;
}

tbody tr:last-child td {
  border-bottom: none;
}

.name {
  font-weight: 500;
}

.description {
  max-width: 280px;
}

.active-cat {
  background-color: rgb(132, 219, 132);
  padding: 5px;
}

.inactive-cat {
  background-color: rgb(221, 113, 113);
  padding: 5px;
}

.edit-cell {
  text-align: right;
}

.edit-link {
  color: #6b7280;
  font-size: 1.23em;
  text-decoration: none;
}
</style>