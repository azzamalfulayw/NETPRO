import Keycloak from 'keycloak-js'

const keycloakConfig = {
  url: 'http://localhost:8080',
  realm: 'netpro',
  clientId: 'netpro-frontend',
}

const keycloak = new Keycloak(keycloakConfig)

/**
 * Singleton service to manage Keycloak authentication state.
 */
export const authService = {
  init: (onAuthenticatedCallback: () => void) => {
    keycloak
      .init({
        onLoad: 'check-sso',
        silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
        pkceMethod: 'S256',
      })
      .then((authenticated) => {
        if (authenticated) {
          console.log('User is authenticated')
          onAuthenticatedCallback()
        } else {
          console.log('User is not authenticated')
          onAuthenticatedCallback()
        }
      })
      .catch((err) => {
        console.error('Keycloak initialization failed', err)
      })
  },

  login: () => keycloak.login(),
  
  logout: () => keycloak.logout({
    redirectUri: window.location.origin + '/login'
  }),

  getToken: () => keycloak.token,

  isLoggedIn: () => !!keycloak.token,

  updateToken: (minValidity = 5) => 
    keycloak.updateToken(minValidity)
      .then((refreshed) => {
        if (refreshed) {
          console.log('Token was successfully refreshed')
        }
        return refreshed
      })
      .catch(() => {
        console.error('Failed to refresh token, logging out...')
        keycloak.logout()
      }),

  getUsername: () => keycloak.tokenParsed?.preferred_username as string,

  getUserFullName: () => {
    const firstName = (keycloak.tokenParsed as any)?.given_name || ''
    const lastName = (keycloak.tokenParsed as any)?.family_name || ''
    return `${firstName} ${lastName}`.trim() || authService.getUsername()
  }
}

export default keycloak
