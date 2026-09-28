import { HealthStatus } from './features/health/HealthStatus'

// For now the whole app is the health page. React Router with the Public /
// Account / Admin layouts replaces this in the next frontend step.
function App() {
  return (
    <main>
      <HealthStatus />
    </main>
  )
}

export default App
