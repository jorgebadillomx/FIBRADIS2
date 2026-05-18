import { spawn } from 'node:child_process'

const baseUrl = 'http://127.0.0.1:5173'
const workspaceDir = new URL('..', import.meta.url)

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms))
}

async function waitForServer(url, timeoutMs) {
  const startedAt = Date.now()

  while (Date.now() - startedAt < timeoutMs) {
    try {
      const response = await fetch(url)
      if (response.ok) {
        return
      }
    } catch {
      // Server not ready yet.
    }

    await delay(500)
  }

  throw new Error(`Timed out waiting for ${url}`)
}

function runCommand(command, args, options = {}) {
  return spawn(command, args, {
    cwd: workspaceDir,
    stdio: 'inherit',
    shell: true,
    ...options,
  })
}

async function killServer(server) {
  if (server.exitCode !== null) {
    return
  }

  if (process.platform === 'win32') {
    await new Promise((resolve) => {
      const killer = spawn('taskkill', ['/pid', String(server.pid), '/t', '/f'], {
        stdio: 'ignore',
      })

      killer.on('exit', () => resolve())
      killer.on('error', () => resolve())
    })

    return
  }

  server.kill('SIGTERM')
}

const viteServer = runCommand('npx', ['vite', '--host', '127.0.0.1', '--strictPort'])

try {
  await waitForServer(baseUrl, 120000)

  const playwright = runCommand('npx', ['playwright', 'test'])

  const exitCode = await new Promise((resolve, reject) => {
    playwright.on('exit', resolve)
    playwright.on('error', reject)
  })

  await killServer(viteServer)
  process.exit(exitCode ?? 1)
} catch (error) {
  await killServer(viteServer)
  console.error(error instanceof Error ? error.message : error)
  process.exit(1)
}
