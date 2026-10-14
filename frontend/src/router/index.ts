import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import LoginView from '@/views/LoginView.vue'
import ItemsView from '@/views/ItemsView.vue'
import ItemDetailView from '@/views/ItemDetailView.vue'
import ItemCreateView from '@/views/ItemCreateView.vue'
import ItemEditView from '@/views/ItemEditView.vue'
import CategoriesView from '@/views/CategoriesView.vue'
import CategoriesCreateView from '@/views/CategoriesCreateView.vue'
import CategoriesEditView from '@/views/CategoriesEditView.vue'
import DepartmentsViews from '@/views/DepartmentsViews.vue'
import DepartmentsCreateView from '@/views/DepartmentsCreateView.vue'
import DepartmentsEditView from '@/views/DepartmentsEditView.vue'
import UsersView from '@/views/UsersView.vue'
import UsersCreateView from '@/views/UsersCreateView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/items' },
    { path: '/login', component: LoginView },
    { path: '/items', component: ItemsView },
    { path: '/items/:id', component: ItemDetailView },
    { path: '/items/new', component: ItemCreateView },
    { path: '/items/:id/edit', component: ItemEditView },
    { path: '/categories', component: CategoriesView },
    { path: '/categories/new', component: CategoriesCreateView },
    { path: '/categories/:id/edit', component: CategoriesEditView },
    { path: '/departments', component: DepartmentsViews },
    { path: '/departments/new', component: DepartmentsCreateView },
    { path: '/departments/:id/edit', component: DepartmentsEditView },
    { path: '/users', component: UsersView },
    { path: '/users/new', component: UsersCreateView}
  ],
})

// runs before each navigation
router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // if user is not authenticated, redirect them to login page
  if (auth.user === null) {
    await auth.loadMe()
  }

  if (auth.user === null && to.path !== '/login') {
    return '/login'
  }

  // if authenticated and try to access login page, redirect them to items 
  if (auth.user !== null && to.path === '/login') {
    return '/items'
  }

  // if any other user besides admin tries to access departments
  if (auth.user?.role !== "Administrator" && to.path.startsWith('/departments')) {
    return '/items'
  }

  // if any other user tries to edit or create categories
  if (auth.user?.role !== "Administrator" && to.path.startsWith('/categories/')) {
    return '/categories'
  }

  // only admins can access users 
  if (auth.user?.role !== "Administrator" && to.path.startsWith('/users')) {
    return '/items'
  }
})

export default router
