<script setup lang="ts">
import { ref } from 'vue'

interface User {
  id: number
  firstName: string
  lastName: string
  email: string
  role: string
  departmentName: string | null
}

const email = ref('')
const password = ref('')
const errorMessage = ref('')
const user = ref<User | null>(null)

async function login() {
  errorMessage.value = ''

  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json'},
    body: JSON.stringify({email: email.value, password: password.value}),
  })

  if (response.status == 401) {
    errorMessage.value = 'Invalid email or password.'
    return
  }

  if (!response.ok) {
    errorMessage.value = `Something went wrong (error ${response.status}).`
    return 
  }

  user.value = await response.json()
}

</script>

<template>
  <h1>ExpiryTrack</h1>
  <p v-if="user">Hello, {{ user.firstName }} {{ user.lastName }} {{ user.role }}</p>

  <form v-else @submit.prevent="login">
    <div>
      <label>Email <input v-model="email" type="email" required /></label>
    </div>

    <div>
      <label>Password <input v-model="password" type="password" required /></label>
    </div>

    <button type="submit">Log in</button>
    <p v-if="errorMessage" class="error"> {{ errorMessage }}</p>
  </form>
</template>

<style scoped></style>
