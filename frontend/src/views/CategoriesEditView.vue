<script setup lang="ts">
import {ref, onMounted} from 'vue'
import {useRoute, useRouter } from 'vue-router'

interface Category {
    id: number
    name: string
    description: string | null
    warningPeriodDays: number
    requiresFinancialReview: boolean
    financeReviewThreshold: number | null
    isActive: boolean 
}

const route = useRoute()
const router = useRouter()
const id = route.params.id

const category = ref<Category | null>(null)
const loading = ref(true)
const errorMessage = ref('')
const saveError = ref('')
const saving = ref(false)

const name = ref('')
const description = ref('')
const warningPeriodDays = ref('30')
const requiresFinancialReview = ref(false)
const financeReviewThreshold = ref('')
const isActive = ref(true)

onMounted(async() => {
    const response = await fetch(`/api/categories/${id}`)

    if (response.ok) {
        const data: Category = await response.json()
        category.value = data
        name.value = data.name
        description.value = data.description ?? ''
        warningPeriodDays.value = data.warningPeriodDays.toString()
        requiresFinancialReview.value = data.requiresFinancialReview 
        financeReviewThreshold.value = data.financeReviewThreshold?.toString() ?? ''
        isActive.value = data.isActive
    } else if (response.status === 404 || response.status === 400) {
        errorMessage.value = 'Category not found.'
    } else {
        errorMessage.value = `Could not load category (error ${response.status}).`
    }
    loading.value = false 
})

async function editCategory() {
    saveError.value = ''
    saving.value = true

    const response = await fetch(`/api/categories/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            name: name.value,
            description: description.value || null,
            warningPeriodDays: warningPeriodDays.value,
            requiresFinancialReview: requiresFinancialReview.value,
            financeReviewThreshold: financeReviewThreshold.value || null,
            isActive: isActive.value
        }),
    })

      saving.value = false

    if (response.ok) {
        router.push('/categories')
    } else if (response.status === 400) {
        const contentType = response.headers.get('Content-Type') ?? ''
        if (contentType.includes('json')) {
            const problem = await response.json()
            saveError.value = Object.values(problem.errors).flat().join(' ')
        } else {
            saveError.value = await response.text()
        }
    } else if (response.status === 409) {
        saveError.value = await response.text()
    } else {
        saveError.value = `Could not edit the category (error ${response.status}).`
    }
}

</script>

<template>
  <RouterLink :to="`/categories`">Back to categories</RouterLink>
  <h2>Edit Category</h2>
  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>
  <div v-else-if="category">
        <form @submit.prevent="editCategory">
    <div>
      <label>
        Name
        <input v-model="name" required maxlength="100" />
      </label>
    </div>
    <div>
      <label>
        Description
        <textarea v-model="description" rows="3" maxlength="255"></textarea>
      </label>
    </div>
    <div>
      <label>
        Warning Period (days)
        <input v-model="warningPeriodDays" type="number" min="1" max="365" required/>
      </label>
    </div>
    <div>
      <label>
        Requires Financial Review
        <input type="checkbox" v-model="requiresFinancialReview" />
      </label>
    </div>
    <div v-if="requiresFinancialReview">
      <label>
        Finance Review Threshold
        <input v-model="financeReviewThreshold" type="number" min="0" step="0.01"/>
      </label>
    </div>
    <div>
      <label>
        Active
        <input v-model="isActive" type="checkbox" />
      </label>
    </div>

    <button type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Save category' }}</button>
    <p v-if="saveError" class="error">{{ saveError }}</p>
    </form>
  </div>

</template>