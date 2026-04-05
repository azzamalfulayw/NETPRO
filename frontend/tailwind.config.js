/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        background: '#020617', // slate-950
        foreground: '#f8fafc', // slate-50
        card: '#0f172a', // slate-900
        primary: '#3b82f6', // blue-500
        secondary: '#64748b', // slate-500
        accent: '#f59e0b', // amber-500
        success: '#22c55e', // green-500
        danger: '#ef4444', // red-500
      }
    },
  },
  plugins: [],
}
