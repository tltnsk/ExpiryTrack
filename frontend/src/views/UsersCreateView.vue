<script setup lang="ts">
import { ref, onMounted} from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const firstName = ref('')
const lastName = ref('')
const email = ref('')
const role = ref('')
const password = ref('')
const departmentId = ref<number | null>(null)

const errorMessage = ref('')
const saving = ref(false)

const roles = ['Administrator', 'Employee', 'DepartmentManager', 'FinanceOfficer']

// so that a department can be added  
interface Department {
  id: number
  name: string
  isActive: boolean
  managerId: number | null
  managerName: string | null
}

const departments = ref<Department[]>([])

onMounted(async () => {
  const departmentResponse = await fetch('/api/departments')
  if (departmentResponse.ok) {
    const possibleDepartments: Department[] = await departmentResponse.json()
    departments.value = possibleDepartments.filter((d) => d.isActive)
  }
})

async function createUser() {
    errorMessage.value = ''
    saving.value = true

    const response = await fetch('/api/users', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            firstName: firstName.value,
            lastName: lastName.value,
            email: email.value,
            role: role.value,
            departmentId: departmentId.value,
            password: password.value,
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
    if (response.status === 409) {
        errorMessage.value = await response.text()
        return
    }
    if (!response.ok) {
        errorMessage.value = `Could not create the user (error ${response.status}).`
        return
    }
    router.push('/users')
}

</script>
<template>
<RouterLink to="/users">Back to users</RouterLink>
  <h2>New user</h2>

  <form @submit.prevent="createUser">
    <div>
      <label>
        First Name
        <input v-model="firstName" required maxlength="100" />
      </label>
    </div>
    <div>
      <label>
        Last Name
        <input v-model="lastName" required maxlength="100" />
      </label>
    </div>
    <div>
      <label>
        Email
        <input v-model="email" type="email" required/>
      </label>
    </div>
    <div>
      <label>
        Password
        <input v-model="password" type="password" minlength="8" required/>
      </label>
    </div>
    <div>
        <label>
          Role
          <select v-model="role">
            <option :value="''">No role</option>
            <option v-for="role in roles" :key="role" :value="role">
              {{  role }}
            </option>
          </select>
        </label>
    </div>
    <div>
        <label>
          Department
          <select v-model="departmentId">
            <option :value="null">No department</option>
            <option v-for="department in departments" :key="department.id" :value="department.id">
              {{ department.name}}
            </option>
          </select>
        </label>
        <p v-if="departments.length === 0">
          No active departments yet.
        </p>
      </div>

    <button type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Create user' }}</button>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
  </form>
</template>