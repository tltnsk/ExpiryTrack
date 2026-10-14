<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

// CategoryResponse
interface Category {
    id: number,
    name: string,
    warningPeriodDays: number,
    isActive: boolean
}

interface User {
  id: number
  firstName: string
  lastName: string
}

const router = useRouter()

const categories = ref<Category[]>([])
const name = ref('')
const categoryId = ref<number | null>(null)
const description = ref('')
const provider = ref('')
const referenceNumber = ref('')
const startDate = ref('')
const expirationDate = ref('')
const cost = ref('')
const responsibleUserId = ref<number | null>(null)

const errorMessage = ref('')
const saving = ref(false)

const auth = useAuthStore()
const employees = ref<User[]>([])

onMounted(async () => {
    const response = await fetch('/api/categories')
    if (response.ok) {
        const all: Category[] = await response.json()
        categories.value = all.filter((c) => c.isActive)
    }
    if (auth.user?.role === 'DepartmentManager') {
      const employeesResponse = await fetch('/api/users/employees')
      if (employeesResponse.ok) {
        employees.value = await employeesResponse.json()
      }
    }
})

async function createItem() {
  errorMessage.value = ''
  saving.value = true

  const response = await fetch('/api/items', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      name: name.value,
      categoryId: categoryId.value,
      description: description.value || null,
      provider: provider.value || null,
      referenceNumber: referenceNumber.value || null,
      startDate: startDate.value,
      expirationDate: expirationDate.value,
      cost: cost.value === '' ? null : Number(cost.value),
      responsibleUserId: responsibleUserId.value
    }),
  })

  saving.value = false

  if (response.status === 400) {
    const contentType = response.headers.get('Content-Type') ?? ''
    if (contentType.includes('json')) {
      const problem = await response.json()
      errorMessage.value = Object.values(problem.errors).flat().join(' ')
    } else {
      errorMessage.value = await response.text()
    }
    return
  }
  if (!response.ok) {
    errorMessage.value = `Could not create the item (error ${response.status}).`
    return
  }

  const created = await response.json()
  router.push(`/items/${created.id}`)
}
</script>

<template>
    <RouterLink to="/items">Back to items</RouterLink>
    <h2>New item</h2>

    <form @submit.prevent="createItem">
      <div>
      <label>
        Name
        <input v-model="name" required maxlength="150" />
      </label>
       </div>
        <div>
            <label>
                Category
                <select v-model="categoryId" required>
                    <option :value="null" disabled>Select a category</option>
                    <option v-for="category in categories" :key="category.id" :value="category.id">
                        {{ category.name }}
                    </option>
                </select>
            </label>
        </div>
        <div>
      <label>
        Description
        <textarea v-model="description" rows="3"></textarea>
      </label>
    </div>
    <div>
      <label>
        Provider
        <input v-model="provider" maxlength="150" />
      </label>
    </div>
    <div>
      <label>
        Reference number
        <input v-model="referenceNumber" maxlength="100" />
      </label>
    </div>
    <div>
      <label>
        Start date
        <input v-model="startDate" type="date" required />
      </label>
    </div>
    <div>
      <label>
        Expiration date
        <input v-model="expirationDate" type="date" required />
      </label>
    </div>
    <div>
      <label>
        Cost
        <input v-model="cost" type="number" min="0.01" step="0.01" placeholder="Leave empty if free" />
      </label>
    </div>
    <div v-if="auth.user?.role === 'DepartmentManager'">
      <label>
        Responsible Employee
        <select v-model="responsibleUserId" required>
          <option :value="null" disabled>Select a responsible user</option>
          <option v-if="employees.length===0" :value="null" disabled>No active employees in your department.</option>
          <option v-for="employee in employees" :key="employee.id" :value="employee.id">
            {{ employee.firstName }} {{  employee.lastName }}
          </option>
    
        </select>
      </label>
    </div>

    <button type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Create item' }}</button>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    </form>
</template>