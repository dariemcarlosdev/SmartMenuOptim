/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
        "./Components/**/*.razor",      // All .razor files in Components
        "./Pages/**/*.razor",           // All .razor files in Pages
        "./Components/**/*.cshtml",
        "./Components/**/*.html",
        "./Pages/**/*.cshtml",
        "./Pages/**/*.html",
        "./wwwroot/**/*.html"
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}