<script setup lang="ts">
import {ref, onMounted} from 'vue'
import {useRoute, useRouter } from 'vue-router'

interface Department {
    id: number
    name: string
    isActive: boolean
    managerId: number | null
    managerName: string | null
}

// UserResponse
interface User {
    id: number
    firstName: string
    lastName: string
    role: string
    departmentId: number | null
    isActive: boolean
}

const route = useRoute()
const router = useRouter()
const id = route.params.id

const department = ref<Department | null>(null)
const loading = ref(true)
const errorMessage = ref('')
const saveError = ref('')
const saving = ref(false)

const name = ref('')
const isActive = ref(true)

// user who can be chosen as manager
const managers = ref<User[]>([])
const managerId = ref<number | null>(null)

onMounted(async() => {
    const response = await fetch(`/api/departments/${id}`)

    if (response.ok) {
        const data: Department = await response.json()
        department.value = data
        name.value = data.name
        managerId.value = data.managerId
        isActive.value = data.isActive
    } else if (response.status === 404 || response.status === 400) {
        errorMessage.value = 'Department not found.'
    } else {
        errorMessage.value = `Could not load department (error ${response.status}).`
    }
    loading.value = false 

    // only active users with the role department manager who belong to the current department can be assigned department managers
    const userResponse = await fetch('/api/users')
    if (userResponse.ok) {
        const users: User[] = await userResponse.json()
        managers.value = users.filter(
            (u) => u.role === 'DepartmentManager' && u.isActive && u.departmentId === Number(id),
        )
    }
})

async function editDepartment() {
    saveError.value = ''
    saving.value = true

    const response = await fetch(`/api/departments/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            name: name.value,
            isActive: isActive.value
        }),
    })

    saving.value = false

    if (response.ok) {
        router.push('/departments')
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
        saveError.value = `Could not edit the department (error ${response.status}).`
    }

    // update manager if name and is active updates succeeded
    if (managerId.value !== department.value?.managerId) {
        const managerResponse = await fetch(`/api/departments/${id}/manager`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({userId: managerId.value})
        })

        if (!managerResponse.ok) {
            saving.value = false
            if (managerResponse.status === 400 || managerResponse.status === 409) {
                // AssignManager in the controller sends plain text messages
              saveError.value = await managerResponse.text()
            } else {
                saveError.value = `Could not save the manager (error ${managerResponse.status}).`
            }
            return
        }
    }

    saving.value = false
    router.push('/departments')
}

</script>

<template>
  <RouterLink :to="`/departments`">Back to departments</RouterLink>
  <h2>Edit department</h2>
  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>
  <div v-else-if="department">
    <form @submit.prevent="editDepartment">
        <div>
        <label>
            Name
            <input v-model="name" required maxlength="100" />
        </label>
        </div>
        <div>
        <label>
            Active
            <input v-model="isActive" type="checkbox" />
        </label>
        </div>
              <div>
        <label>
          Manager
          <select v-model="managerId">
            <option :value="null">No manager</option>
            <option v-for="user in managers" :key="user.id" :value="user.id">
              {{ user.firstName }} {{ user.lastName }}
            </option>
          </select>
        </label>
        <p v-if="managers.length === 0">
          No active department managers belong to this department yet.
        </p>
      </div>

        <button type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Save department' }}</button>
        <p v-if="saveError" class="error">{{ saveError }}</p>
    </form>
  </div>

</template>