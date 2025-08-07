/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Components/**/*.{razor,html,cshtml}",
    "./Pages/**/*.{razor,html,cshtml}",
    "./wwwroot/**/*.html",
    "./**/*.razor"
  ],
  theme: {
    extend: {
      colors: {
        'royal-purple': 'rgb(147, 51, 234)',
        'deep-purple': 'rgb(126, 34, 206)',
        'light-purple': 'rgb(196, 181, 253)',
        'royal-gold': 'rgb(245, 158, 11)',
        'light-gold': 'rgb(252, 211, 77)',
        'dark-gold': 'rgb(217, 119, 6)',
        'pink-primary': 'rgb(236, 72, 153)',
        'pink-light': 'rgb(251, 207, 232)',
        'charcoal': 'rgb(15, 23, 42)'
      },
      fontFamily: {
        'inter': ['Inter', 'sans-serif'],
        'poppins': ['Poppins', 'sans-serif'],
        'display': ['Poppins', 'sans-serif']
      },
      animation: {
        'fade-in-up': 'fadeInUp 0.8s cubic-bezier(0.4, 0, 0.2, 1)',
        'gradient-shift': 'gradientShift 4s ease infinite',
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite'
      },
      keyframes: {
        fadeInUp: {
          '0%': {
            opacity: '0',
            transform: 'translateY(40px)'
          },
          '100%': {
            opacity: '1',
            transform: 'translateY(0)'
          }
        },
        gradientShift: {
          '0%': { 'background-position': '0% 50%' },
          '50%': { 'background-position': '100% 50%' },
          '100%': { 'background-position': '0% 50%' }
        }
      },
      boxShadow: {
        'professional': '0 10px 40px rgba(147, 51, 234, 0.1)',
        'professional-hover': '0 20px 60px rgba(147, 51, 234, 0.2)',
        'glow': '0 0 20px rgba(147, 51, 234, 0.3)',
        'glow-pink': '0 0 20px rgba(236, 72, 153, 0.3)'
      },
      backdropBlur: {
        'xs': '2px',
      }
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
} 