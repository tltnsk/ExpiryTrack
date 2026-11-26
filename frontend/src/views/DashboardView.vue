<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import EmployeeHome from '@/components/dashboards/EmployeeHome.vue'
import ManagerHome from '@/components/dashboards/ManagerHome.vue'
import AdminHome from '@/components/dashboards/AdminHome.vue'
import FinanceHome from '@/components/dashboards/FinanceHome.vue'

const auth = useAuthStore()

</script>
<template>
    <EmployeeHome v-if="auth.user?.role === 'Employee'" />
    <ManagerHome v-else-if="auth.user?.role === 'DepartmentManager'" />
    <AdminHome v-else-if="auth.user?.role === 'Administrator'" />
    <FinanceHome v-else-if="auth.user?.role === 'FinanceOfficer'" />
</template>
