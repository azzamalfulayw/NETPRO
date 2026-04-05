import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { Shield, LayoutDashboard, ArrowRight, Loader2 } from 'lucide-react'

export default function LoginPage() {
  const { isAuthenticated, isLoading, login } = useAuth()
  const location = useLocation()
  
  // Get the redirect path from location state or default to dashboard
  const from = (location.state as any)?.from?.pathname || '/dashboard'

  if (isLoading) {
    return (
      <div className="flex h-screen w-full items-center justify-center bg-slate-950">
        <Loader2 className="w-10 h-10 animate-spin text-blue-500" />
      </div>
    )
  }

  if (isAuthenticated) {
    return <Navigate to={from} replace />
  }

  return (
    <div className="min-h-screen bg-slate-950 flex flex-col items-center justify-center p-6 text-white relative overflow-hidden">
      {/* Decorative Gradients */}
      <div className="absolute top-[-20%] left-[-10%] w-[50%] h-[50%] bg-blue-500/10 rounded-full blur-[120px]" />
      <div className="absolute bottom-[-20%] right-[-10%] w-[50%] h-[50%] bg-blue-600/10 rounded-full blur-[120px]" />

      <div className="w-full max-w-md space-y-8 relative z-10 text-center">
        <div className="flex flex-col items-center space-y-4">
           <div className="w-20 h-20 bg-blue-500/10 rounded-3xl flex items-center justify-center border border-blue-500/20 shadow-2xl shadow-blue-500/5">
              <Shield className="w-10 h-10 text-blue-500" />
           </div>
           <div>
              <h1 className="text-4xl font-black italic tracking-tighter uppercase mb-1">
                 NETPRO <span className="text-blue-500">AUTH</span>
              </h1>
              <p className="text-slate-400 text-sm font-medium uppercase tracking-widest">
                 Secure Institutional Access
              </p>
           </div>
        </div>

        <div className="card glass p-8 border-slate-800/50 space-y-8 shadow-2xl">
           <div className="space-y-2">
              <h2 className="text-xl font-bold">Welcome Back</h2>
              <p className="text-sm text-slate-400">Please sign in via Keycloak to access your portfolio and transaction ledger.</p>
           </div>

           <button 
             onClick={() => login()}
             className="btn btn-primary w-full py-4 text-sm font-black uppercase tracking-widest flex items-center justify-center gap-2 group"
           >
             Sign in with Keycloak
             <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" />
           </button>

           <div className="grid grid-cols-2 gap-4 text-[10px] uppercase font-black tracking-widest text-slate-600">
              <div className="flex items-center gap-2 justify-center py-2 bg-slate-900/50 rounded-lg border border-slate-800">
                 <Shield className="w-3 h-3" />
                 Encrypted
              </div>
              <div className="flex items-center gap-2 justify-center py-2 bg-slate-900/50 rounded-lg border border-slate-800">
                 <LayoutDashboard className="w-3 h-3" />
                 Managed
              </div>
           </div>
        </div>

        <p className="text-slate-600 text-[10px] uppercase font-bold tracking-[0.2em] pt-8">
           Standard OAuth2 / OIDC Protocol Implementation
        </p>
      </div>
    </div>
  )
}
