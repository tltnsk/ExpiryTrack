<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'

// DepartmentResponse in C#
interface Department {
  id: number
  name: string
  isActive: boolean
  managerId: number | null
  managerName: string | null
}

const departments = ref<Department[]>([])
const auth = useAuthStore()

const loading = ref(true)
const errorMessage = ref('')

// fetch departments from the backend
onMounted(async () => {
  const response = await fetch('/api/departments')
  if (response.ok) {
    departments.value = await response.json()
  } else {
    errorMessage.value = `Could not load departments (error ${response.status}).`
  }
  loading.value = false
})
</script>

<template>
<h2>Departments</h2>

  <RouterLink v-if="auth.user?.role === 'Administrator'" to="/departments/new">New Department</RouterLink>

  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>
  <p v-else-if="departments.length === 0">No departments yet.</p>

  <table v-else>
    <thead>
      <tr>
        <th>Name</th>
        <th>Active</th>
        <th>Manager</th>
        <th v-if="auth.user?.role === 'Administrator'"></th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="department in departments" :key="department.id">
        <td>{{ department.name }}</td>
        <td>{{ department.isActive ? 'Yes' : 'No' }}</td>
        <td>{{ department.managerName ?? 'No manager' }}</td>
        <td v-if="auth.user?.role === 'Administrator'">
          <RouterLink :to="`/departments/${department.id}/edit`">Edit</RouterLink>
        </td>
      </tr>
    </tbody>
  </table>
</template>

<style scoped>
</style>