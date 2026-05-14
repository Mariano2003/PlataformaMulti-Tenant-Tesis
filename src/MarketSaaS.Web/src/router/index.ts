import { createRouter, createWebHistory } from 'vue-router'
import type { RouteLocationRaw } from 'vue-router'
import TiendaView from '../views/TiendaView.vue'
import CheckoutView from '../views/CheckoutView.vue'
import PortalLoginView from '../views/PortalLoginView.vue'
import RecuperarClaveView from '../views/RecuperarClaveView.vue'
import RestablecerClaveView from '../views/RestablecerClaveView.vue'
import SeleccionTiendasView from '../views/SeleccionTiendasView.vue'
import SuperAdminPlataformaView from '../views/SuperAdminPlataformaView.vue'
import AdminLoginView from '../views/admin/AdminLoginView.vue'
import AdminPedidosView from '../views/admin/AdminPedidosView.vue'
import AdminProductosView from '../views/admin/AdminProductosView.vue'
import AdminAnalyticsView from '../views/admin/AdminAnalyticsView.vue'
import AdminChatView from '../views/admin/AdminChatView.vue'
import AdminMercadoPagoView from '../views/admin/AdminMercadoPagoView.vue'
import { useAuthStore } from '../stores/auth'

function resolverInicio(): RouteLocationRaw {
  const auth = useAuthStore()
  if (!auth.token || !auth.usuario) return { name: 'portal-login' }
  const u = auth.usuario
  if (u.rol === 'SuperAdmin') return { name: 'superadmin-plataforma' }
  if (u.rol === 'AdminTienda') {
    const slug = u.negocioSlug?.trim()
    if (slug) return { name: 'admin-pedidos', params: { slug } }
    return { name: 'portal-login' }
  }
  if (u.rol === 'Cliente') return { name: 'tiendas' }
  return { name: 'portal-login' }
}

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      redirect: resolverInicio,
    },
    {
      path: '/acceder',
      name: 'portal-login',
      component: PortalLoginView,
    },
    {
      path: '/recuperar-clave',
      name: 'recuperar-clave',
      component: RecuperarClaveView,
    },
    {
      path: '/restablecer-clave',
      name: 'restablecer-clave',
      component: RestablecerClaveView,
    },
    {
      path: '/plataforma',
      name: 'superadmin-plataforma',
      component: SuperAdminPlataformaView,
      meta: { requiresSesion: true, rolesPermitidos: ['SuperAdmin'] },
    },
    {
      path: '/tiendas',
      name: 'tiendas',
      component: SeleccionTiendasView,
      meta: { requiresSesion: true, rolesPermitidos: ['Cliente', 'SuperAdmin'] },
    },
    {
      path: '/admin/:slug/login',
      name: 'admin-login',
      component: AdminLoginView,
    },
    {
      path: '/admin/:slug/pedidos',
      name: 'admin-pedidos',
      component: AdminPedidosView,
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/:slug/productos',
      name: 'admin-productos',
      component: AdminProductosView,
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/:slug/analytics',
      name: 'admin-analytics',
      component: AdminAnalyticsView,
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/:slug/chat',
      name: 'admin-chat',
      component: AdminChatView,
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/:slug/mercadopago',
      name: 'admin-mercadopago',
      component: AdminMercadoPagoView,
      meta: { requiresAuth: true },
    },
    {
      path: '/tienda/:slug/checkout',
      name: 'checkout',
      component: CheckoutView,
    },
    {
      path: '/tienda/:slug',
      name: 'tienda',
      component: TiendaView,
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.meta.requiresSesion === true && !auth.token) {
    return {
      name: 'portal-login',
      query: { redirect: to.fullPath },
    }
  }

  const rolesPermitidos = to.meta.rolesPermitidos as string[] | undefined
  if (rolesPermitidos?.length && auth.usuario) {
    if (!rolesPermitidos.includes(auth.usuario.rol)) {
      return resolverInicio()
    }
  }

  if (to.meta.requiresAuth === true) {
    if (!auth.token) {
      return {
        name: 'admin-login',
        params: { slug: to.params.slug as string },
        query: { redirect: to.fullPath },
      }
    }
  }
})
