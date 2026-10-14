<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'

// UserResposnse 
interface User {
  id: number
  firstName: string
  lastName: string
  email: string
  role: string
  departmentId: number | null
  departmentName: string | null
  isActive: boolean
}

const users = ref<User[]>([])
const loading = ref(true)
const errorMessage = ref('')
const auth = useAuthStore()

onMounted(async () => {
  const response = await fetch('/api/users')
  if (response.ok) {
    users.value = await response.json()
  } else {
    errorMessage.value = `Could not load users (error ${response.status}).`
  }
  loading.value = false
})
</script> 

<template>
  <h2>Users</h2>

  <RouterLink v-if="auth.user?.role === 'Administrator'" to="/users/new">New user</RouterLink>

  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>
  <p v-else-if="users.length === 0">No users yet.</p>

  <table v-else>
    <thead>
      <tr>
        <th>Name</th>
        <th>Email</th>
        <th>Role</th>
        <th>Department</th>
        <th>Active</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="user in users" :key="user.id">
        <td>
            <RouterLink :to="`/users/${user.id}`">{{ user.firstName }} {{ user.lastName }}</RouterLink>
        </td>
        <td> {{ user.email}}</td>
        <td> {{ user.role }}</td>
        <td> {{ user.departmentName  ?? '-'}}</td>
        <td> {{ user.isActive ? "Yes" : "No"}}</td>
      </tr>
    </tbody>
  </table>
</template>