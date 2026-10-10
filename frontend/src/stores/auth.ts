import { ref } from 'vue'
import { defineStore } from 'pinia'

// UserResponse 
// describes structure of data that the backend returns 
export interface User {
    id: number
    firstName: string
    lastName: string
    email: string
    role: string
    departmentName: string | null
}

// give components access to the shared auth store 
export const useAuthStore = defineStore('auth', () => {
    const user = ref<User | null>(null)

    async function loadMe() {
        const response = await fetch('/api/auth/me')
        if (response.ok) {
            user.value = await response.json()
        } else {
            user.value = null
        }
    }

    async function logout() {
        await fetch('/api/auth/logout', { method: 'POST' })
        user.value = null
    }

    return { user, loadMe, logout }
})