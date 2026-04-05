import { useState, useEffect } from 'react'
import { Save, ShieldCheck, Globe, User } from 'lucide-react'
import { toast } from 'react-hot-toast'
import { useAuth } from '../context/AuthContext'

export default function SettingsPage() {
  const [apiUrl, setApiUrl] = useState('')
  const { fullName, username, isAuthenticated } = useAuth()

  useEffect(() => {
    setApiUrl(localStorage.getItem('netpro_api_url') || 'http://localhost:5247/api')
  }, [])

  const handleSave = () => {
    try {
      localStorage.setItem('netpro_api_url', apiUrl)
      toast.success('Settings saved successfully')
      // Force a reload to update axios base URL
      setTimeout(() => window.location.reload(), 1000)
    } catch (error) {
      toast.error('Failed to save settings')
    }
  }

  return (
    <div className="max-w-2xl mx-auto space-y-8">
      <div className="card">
        <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
          <Globe className="w-5 h-5 text-blue-500" />
          API Configuration
        </h2>
        
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-400 mb-1.5">
              Backend Base URL
            </label>
            <div className="flex gap-2">
               <input 
                 type="text" 
                 value={apiUrl}
                 onChange={(e) => setApiUrl(e.target.value)}
                 className="input flex-1"
                 placeholder="http://localhost:5247/api"
               />
               <button 
                 onClick={handleSave}
                 className="btn btn-primary px-6 flex items-center gap-2"
               >
                 <Save className="w-4 h-4" />
                 Save
               </button>
            </div>
            <p className="mt-2 text-xs text-slate-500 font-medium">
               The root URL of your .NET Web API project. Restart for changes to take effect globally.
            </p>
          </div>
        </div>
      </div>

      <div className="card border-blue-900/30">
        <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
          <ShieldCheck className="w-5 h-5 text-blue-500" />
          Identity & Security
        </h2>
        
        <div className="space-y-6">
          <div className="flex items-center gap-4 p-4 bg-slate-900/50 rounded-xl border border-slate-800">
             <div className="w-12 h-12 rounded-full bg-blue-500/10 flex items-center justify-center border border-blue-500/20">
                <User className="w-6 h-6 text-blue-500" />
             </div>
             <div>
                <p className="text-xs font-black uppercase text-slate-500 tracking-widest mb-0.5">Logged in as</p>
                <p className="font-bold text-slate-100">{fullName}</p>
                <p className="text-[10px] text-blue-400 font-black uppercase tracking-tighter">{username}</p>
             </div>
             <div className="ml-auto">
                <div className="flex items-center gap-1.5 bg-green-500/10 text-green-500 px-2 py-1 rounded-md border border-green-500/20">
                   <div className="w-1.5 h-1.5 rounded-full bg-green-500"></div>
                   <span className="text-[10px] font-black uppercase tracking-widest">Authenticated</span>
                </div>
             </div>
          </div>

          <div className="p-4 bg-blue-500/5 rounded-xl border border-blue-400/10 text-xs text-blue-400 leading-relaxed italic">
             <p>Authentication is managed automatically via Keycloak OIDC. Manual token entry is disabled to ensure security protocol integrity.</p>
          </div>
        </div>
      </div>

      <div className="p-6 bg-slate-900/50 rounded-xl border border-slate-800 text-sm text-slate-400">
        <h3 className="font-bold text-slate-200 mb-2">Technical Summary:</h3>
        <ul className="space-y-2 list-disc list-inside">
           <li>Flow: Authorization Code with PKCE</li>
           <li>Provider: Keycloak (Realm: netpro)</li>
           <li>Session: Active {isAuthenticated ? '✓' : '✗'}</li>
        </ul>
      </div>
    </div>
  )
}
