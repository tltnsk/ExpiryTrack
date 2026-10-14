<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

interface User {
    id: number
    firstName: string
    lastName: string
    email: string
    role: string
    departmentName: string | null
    isActive: boolean
}

const route = useRoute()
const router = useRouter()
const id = route.params.id

const user = ref<User | null>(null)
const loading = ref(true)
const errorMessage = ref('')
const saveError = ref('')
const saving = ref(false)

const firstName = ref('')
const lastName = ref('')
const email = ref('')


onMounted(async () => {
  const response = await fetch(`/api/users/${id}`)
  if (response.ok) {
    const data: User = await response.json()
    user.value = data
    firstName.value = data.firstName
    lastName.value = data.lastName
    email.value = data.email
  } else if (response.status === 404 || response.status === 400) {
    errorMessage.value = 'User not found.'
  } else {
    errorMessage.value = `Could not load user (error ${response.status}).`
  }
  loading.value = false
})

async function editUser() {
  saveError.value = ''
  saving.value = true

  const response = await fetch(`/api/users/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        firstName: firstName.value,
        lastName: lastName.value,
        email: email.value,
    }),
  })

  saving.value = false

  if (response.ok) {
    router.push('/users')
  } else if (response.status === 400) {
    const contentType = response.headers.get('Content-Type') ?? ''
    if (contentType.includes('json')) {
      const problem = await response.json()
      saveError.value = Object.values(problem.errors).flat().join(' ')
    } else {
      saveError.value = await response.text()
    }
  } else if (response.status === 409) {
    saveError.value = "A user with this email already exists."
  }
  
  else {
    saveError.value = `Could not edit the user (error ${response.status}).`
  }
}
</script>

<template>
  <RouterLink :to="'/users'">Back to users</RouterLink>

  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>

  <div v-else-if="user">
    <h2>Edit user</h2>

    <form @submit.prevent="editUser">
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
          Role
          <input :value="user.role" readonly>
        </label>
    </div>
    <div>
        <label>
          Department
          <input :value="user.departmentName != null ? user.departmentName : '-'" readonly>
        </label>
      </div>

      <button type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Save' }}</button>
      <p v-if="saveError" class="error">{{ saveError }}</p>
    </form>
  </div>
</template>
