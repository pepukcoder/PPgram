/** @type {import('tailwindcss').Config} */
module.exports = {
    content: {
      files: ["*.html", "./src/**/*.rs", "./input.css"],
    },
    theme: {
      extend: {
        fontFamily: {
          'inter': ['Inter var', 'ui-sans-serif', 'system-ui', 'sans-serif', 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol', 'Noto Color Emoji'],
          'opensans': ['Open Sans', 'sans-serif'],
          'robotomono': ['Roboto Mono', 'monospace'],
        }
      },
    },
    plugins: [],
    darkMode: 'class',
  }