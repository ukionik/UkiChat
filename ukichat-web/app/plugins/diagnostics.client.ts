// Diagnostics plugin: forwards console.* into the WPF host via WebView2.postMessage,
// so we can see frontend logs in the same file as backend logs.
// Also captures window errors and unhandled rejections.

declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage: (msg: string) => void
      }
    }
    __diagBootTime?: number
    diag?: (level: string, ...args: unknown[]) => void
  }
}

export default defineNuxtPlugin(() => {
  if (typeof window === 'undefined') return

  const bootTime = performance.now()
  window.__diagBootTime = bootTime

  const post = (msg: string) => {
    try {
      window.chrome?.webview?.postMessage(msg)
    } catch {
      // ignored
    }
  }

  const stringify = (a: unknown): string => {
    if (a instanceof Error) return `${a.name}: ${a.message}\n${a.stack ?? ''}`
    if (typeof a === 'string') return a
    try {
      return JSON.stringify(a)
    } catch {
      return String(a)
    }
  }

  const sendLog = (level: string, args: unknown[]) => {
    const ts = (performance.now() - bootTime).toFixed(1)
    const text = args.map(stringify).join(' ')
    post(`[front +${ts}ms] [${level}] ${text}`)
  }

  // Override console methods (keep originals so devtools still works).
  const origLog = console.log.bind(console)
  const origWarn = console.warn.bind(console)
  const origError = console.error.bind(console)
  const origInfo = console.info.bind(console)

  console.log = (...args: unknown[]) => { sendLog('log', args); origLog(...args) }
  console.info = (...args: unknown[]) => { sendLog('info', args); origInfo(...args) }
  console.warn = (...args: unknown[]) => { sendLog('warn', args); origWarn(...args) }
  console.error = (...args: unknown[]) => { sendLog('error', args); origError(...args) }

  // Convenience global
  window.diag = (level: string, ...args: unknown[]) => sendLog(level, args)

  // Capture uncaught errors & promise rejections — these often explain the white screen
  window.addEventListener('error', (e) => {
    sendLog('window.error', [
      `${e.message} @ ${e.filename}:${e.lineno}:${e.colno}`,
      e.error?.stack ?? '',
    ])
  })

  window.addEventListener('unhandledrejection', (e) => {
    sendLog('unhandledrejection', [e.reason instanceof Error ? e.reason : stringify(e.reason)])
  })

  // First markers — useful to see if JS even starts executing
  sendLog('boot', [
    `userAgent="${navigator.userAgent}"`,
    `url="${window.location.href}"`,
    `readyState=${document.readyState}`,
  ])

  document.addEventListener('DOMContentLoaded', () => sendLog('boot', ['DOMContentLoaded']))
  window.addEventListener('load', () => {
    const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined
    if (nav) {
      sendLog('boot', [
        `window.load: ttfb=${(nav.responseStart - nav.startTime).toFixed(0)}ms ` +
        `domContentLoaded=${(nav.domContentLoadedEventEnd - nav.startTime).toFixed(0)}ms ` +
        `loadEvent=${(nav.loadEventEnd - nav.startTime).toFixed(0)}ms`,
      ])
    } else {
      sendLog('boot', ['window.load'])
    }
  })
})
