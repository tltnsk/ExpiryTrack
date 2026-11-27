<script setup lang="ts">
import { ref, onMounted} from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const firstName = ref('')
const lastName = ref('')
const email = ref('')
const role = ref('')
const password = ref('')
const departmentId = ref<number | ''>('')

const errorMessage = ref('')
const saving = ref(false)

const errors = ref<Record<string, string>>({})

function onInvalid(event: Event) {
    const input = event.target as HTMLInputElement
    event.preventDefault() 
    errors.value[input.name] = input.validity.valueMissing
        ? 'This field is required.'
        : input.validationMessage 
}

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
  } else {
        errorMessage.value = 'Could not load the departments.'
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
            departmentId: departmentId.value === '' ? null : departmentId.value, 
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
  <h2>New user</h2>

  <form @submit.prevent="createUser" @invalid.capture="onInvalid" @input="errors = {}">
    <div>
      <label>
        First Name <span class="required">*</span>
        <input v-model="firstName" name="firstName" required maxlength="100" />
      </label>
      <p v-if="errors.firstName" class="field-error">{{ errors.firstName }}</p>
    </div>
    <div>
      <label>
        Last Name <span class="required">*</span>
        <input v-model="lastName" name="lastName" required maxlength="100" />
      </label>
      <p v-if="errors.lastName" class="field-error">{{ errors.lastName }}</p>
    </div>
    <div>
      <label>
        Email <span class="required">*</span>
        <input v-model="email" name="email" type="email" required/>
      </label>
      <p v-if="errors.email" class="field-error">{{ errors.email }}</p>
    </div>
    <div>
      <label>
        Password <span class="required">*</span>
        <input v-model="password" name="password" type="password" minlength="8" required/>
      </label>
      <p v-if="errors.password" class="field-error">{{ errors.password }}</p>
    </div>
    <div>
        <label>
          Role <span class="required">*</span>
          <select v-model="role" name="role" required>
            <option :value="''" >Choose a role</option>
            <option v-for="role in roles" :key="role" :value="role">
              {{  role }}
            </option>
          </select>
        </label>
        <p v-if="errors.role" class="field-error">{{ errors.role }}</p>
    </div>
    <div>
        <label>
          Department <span v-if="role === 'Employee' || role === 'DepartmentManager'" class="required">*</span>
          <select v-model="departmentId" name="departmentId" :required="role === 'Employee' || role === 'DepartmentManager'">
            <option value="">No department</option>
            <option v-for="department in departments" :key="department.id" :value="department.id">
              {{ department.name}}
            </option>
          </select>
        </label>
        <p v-if="errors.departmentId" class="field-error">{{ errors.departmentId }}</p>
        <p v-if="departments.length === 0">
          No active departments yet.
        </p>
      </div>

    <button type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Create user' }}</button>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
  </form>
</template>