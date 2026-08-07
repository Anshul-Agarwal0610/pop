const http = require("http")
const next = require("next")

const app = next({ dev: false, dir: __dirname })
const handle = app.getRequestHandler()

app.prepare().then(() => {
  http.createServer((request, response) => handle(request, response)).listen(process.env.PORT)
}).catch((error) => {
  console.error(error)
  process.exit(1)
})
