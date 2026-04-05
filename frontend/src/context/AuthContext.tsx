import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react'
import { authService } from '../services/authService'

interface AuthContextType {
  isAuthenticated: boolean
  isLoading: boolean
  username: string | null
  fullName: string | null
  login: () => void
  logout: () => void
  getToken: () => string | undefined
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [username, setUsername] = useState<string | null>(null)
  const [fullName, setFullName] = useState<string | null>(null)

  useEffect(() => {
    authService.init(() => {
      const loggedIn = authService.isLoggedIn()
      setIsAuthenticated(loggedIn)
      
      if (loggedIn) {
        setUsername(authService.getUsername())
        setFullName(authService.getUserFullName())
      }
      
      setIsLoading(false)
    })
  }, [])

  const value = {
    isAuthenticated,
    isLoading,
    username,
    fullName,
    login: authService.login,
    logout: authService.logout,
    getToken: authService.getToken,
  }

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen bg-slate-950 gap-4">
        <div className="w-12 h-12 border-4 border-blue-500/20 border-t-blue-500 rounded-full animate-spin"></div>
        <p className="text-slate-500 font-black tracking-widest text-[10px] uppercase animate-pulse">Initializing Identity Session...</p>
      </div>
    )
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
