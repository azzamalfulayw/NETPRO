import { Link, Outlet, useLocation } from 'react-router-dom'
import { 
  LayoutDashboard, 
  TrendingUp, 
  Star, 
  Briefcase, 
  History, 
  Settings,
  LogOut,
  Wallet,
  Activity
} from 'lucide-react'
import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'
import { useQuery } from '@tanstack/react-query'
import { stocksApi } from '../services/api'
import { useAuth } from '../context/AuthContext'

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

const navItems = [
  { name: 'Dashboard', path: '/dashboard', icon: LayoutDashboard },
  { name: 'Stocks', path: '/stocks', icon: TrendingUp },
  { name: 'Live Prices', path: '/live-prices', icon: Activity },
  { name: 'Watchlist', path: '/watchlist', icon: Star },
  { name: 'Portfolio', path: '/portfolio', icon: Briefcase },
  { name: 'Transactions', path: '/transactions', icon: History },
  { name: 'Settings', path: '/settings', icon: Settings },
]

export default function Layout() {
  const location = useLocation()
  const { fullName, logout } = useAuth()

  // Real-time API Connectivity Check
  const { isSuccess: isApiConnected, isLoading: isApiLoading } = useQuery({
    queryKey: ['api_health'],
    queryFn: () => stocksApi.getAll({ PageSize: 1 }),
    refetchInterval: 15000, 
    retry: 1,
  })

  return (
    <div className="flex h-screen bg-slate-950 text-slate-50">
      {/* Sidebar */}
      <aside className="w-64 border-r border-slate-800 flex flex-col glass">
        <div className="p-6 flex items-center gap-3">
          <div className="bg-blue-600 p-2 rounded-xl">
            <Wallet className="w-6 h-6 text-white" />
          </div>
          <span className="font-bold text-xl tracking-tight">NETPRO</span>
        </div>

        <nav className="flex-1 px-4 py-4 space-y-1">
          {navItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className={cn(
                "flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 group",
                location.pathname.startsWith(item.path)
                  ? "bg-blue-600 text-white shadow-lg shadow-blue-900/20"
                  : "text-slate-400 hover:bg-slate-800/50 hover:text-slate-100"
              )}
            >
              <item.icon className={cn(
                "w-5 h-5",
                location.pathname.startsWith(item.path) ? "text-white" : "group-hover:text-blue-400"
              )} />
              <span className="font-medium">{item.name}</span>
            </Link>
          ))}
        </nav>

        <div className="p-4 border-t border-slate-800">
          <button 
            onClick={() => logout()}
            className="flex items-center gap-3 w-full px-4 py-3 rounded-xl text-slate-400 hover:bg-red-500/10 hover:text-red-400 transition-all"
          >
            <LogOut className="w-5 h-5" />
            <span className="font-medium">Logout</span>
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto bg-gradient-to-br from-slate-950 to-slate-900">
        <header className="h-16 border-b border-slate-800 flex items-center justify-between px-8 glass sticky top-0 z-10">
          <div className="flex items-center gap-4">
             <h1 className="text-lg font-semibold text-slate-200">
               {navItems.find(i => location.pathname.startsWith(i.path))?.name || 'Home'}
             </h1>
          </div>
          <div className="flex items-center gap-4">
             <div className="flex flex-col items-end mr-2">
                <span className="text-xs font-black uppercase tracking-widest text-slate-200">{fullName}</span>
                <span className="text-[10px] text-blue-500 font-bold uppercase tracking-tighter">Verified Session</span>
             </div>
             
             <div className={cn(
                "flex items-center gap-2 px-3 py-1.5 rounded-full border transition-all duration-500",
                isApiConnected ? "bg-green-500/10 border-green-500/20" : "bg-red-500/10 border-red-500/20"
             )}>
                <div className={cn(
                   "w-2 h-2 rounded-full",
                   isApiConnected ? "bg-green-500 animate-pulse" : "bg-red-500",
                   isApiLoading && "bg-blue-500 animate-spin"
                )}></div>
                <span className={cn(
                   "text-xs font-medium uppercase tracking-tight",
                   isApiConnected ? "text-slate-400" : "text-red-400"
                )}>
                   {isApiConnected ? 'API Connected' : 'API Offline'}
                </span>
             </div>
          </div>
        </header>

        <div className="p-8">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
