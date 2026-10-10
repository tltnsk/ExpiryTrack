<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const errorMessage = ref('')

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

  auth.user = await response.json()

  // redirect user to items 
  router.push('/items')
}

</script>

<template>
  <h1>Log in</h1>
  <form @submit.prevent="login">
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
