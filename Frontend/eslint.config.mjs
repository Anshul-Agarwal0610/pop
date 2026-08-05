import { defineConfig, globalIgnores } from "eslint/config"
import nextVitals from "eslint-config-next/core-web-vitals"
export default defineConfig([...nextVitals, {
  // These React 19 compiler rules require a wider legacy-component migration.
  rules: { "react-hooks/set-state-in-effect": "off", "react-hooks/purity": "off" }
}, globalIgnores([".next/**","next-env.d.ts"])])
