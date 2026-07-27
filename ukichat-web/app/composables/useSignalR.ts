import * as signalR from "@microsoft/signalr";
import { getCurrentScope, onScopeDispose, readonly, ref } from "vue";

type ConnectedHandler = () => unknown
type HubHandler = (...args: any[]) => void

// Паузы между попытками переподключения (мс). Последнее значение повторяется бесконечно,
// поэтому клиент никогда не сдаётся: бэкенд можно поднять/перезапустить в любой момент.
const RECONNECT_DELAYS = [0, 1000, 2000, 5000, 10000]
// Паузы для «холодного» подключения, когда бэкенда ещё нет (без нулевой — иначе busy loop).
const COLD_START_DELAYS = [1000, 2000, 5000, 10000]

// Всё состояние живёт на уровне модуля: одно соединение на вкладку,
// переживает переходы между страницами и перемонтирование компонентов.
let connection: signalR.HubConnection | null = null
let connectLoopRunning = false
let wakeListenersInstalled = false
let readyPromise: Promise<signalR.HubConnection> | null = null
let resolveReady: ((conn: signalR.HubConnection) => void) | null = null
// Прерывание текущей паузы между попытками (сеть вернулась / вкладка стала активной).
let wakeUp: (() => void) | null = null

const connectedHandlers = new Set<ConnectedHandler>()
const connected = ref(false)

function delayAt(delays: number[], attempt: number) {
    return delays[Math.min(attempt, delays.length - 1)]!
}

function plainSleep(ms: number) {
    return new Promise<void>(resolve => setTimeout(resolve, ms))
}

// Пауза, которую можно прервать досрочно (wakeUp). Используется только циклом подключения.
function sleep(ms: number) {
    return new Promise<void>((resolve) => {
        if (ms <= 0) {
            resolve()
            return
        }
        const finish = () => {
            clearTimeout(timer)
            if (wakeUp === finish) wakeUp = null
            resolve()
        }
        const timer = setTimeout(finish, ms)
        wakeUp = finish
    })
}

// Досрочно будим цикл переподключения вместо ожидания конца паузы.
function installWakeListeners() {
    if (wakeListenersInstalled || typeof window === 'undefined') return
    wakeListenersInstalled = true

    const kick = (reason: string) => {
        if (connected.value) return
        console.log(`[signalr] wake: ${reason}`)
        wakeUp?.()
        void connectLoop()
    }

    window.addEventListener('online', () => kick('online'))
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') kick('visible')
    })
}

function getConnection(): signalR.HubConnection {
    if (connection) return connection

    // В dev WebSocket через Vite-прокси не работает — используем LongPolling
    const transport = import.meta.env.DEV
        ? signalR.HttpTransportType.LongPolling
        : signalR.HttpTransportType.WebSockets

    connection = new signalR.HubConnectionBuilder()
        .withUrl(window.location.origin + "/apphub", { transport }) // dev: прокси 3000→5000, prod: 5000 напрямую
        .withAutomaticReconnect({
            // Никогда не возвращаем null — встроенный реконнект не сдаётся.
            nextRetryDelayInMilliseconds: ctx => delayAt(RECONNECT_DELAYS, ctx.previousRetryCount),
        })
        .configureLogging(signalR.LogLevel.Information)
        .build()

    connection.onreconnecting((err) => {
        connected.value = false
        console.warn(`[signalr] onreconnecting: ${err?.message ?? '<no error>'}`)
    })

    connection.onreconnected((id) => {
        console.log(`[signalr] onreconnected connId=${id}`)
        markConnected()
    })

    connection.onclose((err) => {
        connected.value = false
        console.warn(`[signalr] onclose: ${err?.message ?? '<no error>'} — поднимаем соединение заново`)
        // Страховка на случай, если встроенный реконнект всё-таки завершился:
        // свой цикл поднимает соединение до победного.
        void connectLoop()
    })

    return connection
}

// Сообщаем подписчикам, что соединение снова живое: они перечитывают состояние с бэкенда.
function markConnected() {
    const conn = getConnection()
    connected.value = true

    resolveReady?.(conn)
    resolveReady = null

    for (const handler of [...connectedHandlers]) {
        try {
            Promise.resolve(handler()).catch(err => console.error('[signalr] onConnected handler failed', err))
        } catch (err) {
            console.error('[signalr] onConnected handler failed', err)
        }
    }
}

// Бесконечный цикл подключения. Запускается при старте страницы и после каждого обрыва.
async function connectLoop() {
    if (connectLoopRunning) return
    connectLoopRunning = true

    const conn = getConnection()
    let attempt = 0

    try {
        for (;;) {
            if (conn.state === signalR.HubConnectionState.Connected) {
                if (!connected.value) markConnected()
                return
            }

            // Идёт start()/встроенный реконнект — не мешаем, просто ждём.
            if (conn.state !== signalR.HubConnectionState.Disconnected) {
                await plainSleep(500)
                continue
            }

            const t0 = performance.now()
            try {
                await conn.start()
                console.log(`[signalr] connected: state=${conn.state} took=${(performance.now() - t0).toFixed(0)}ms`)
                markConnected()
                return
            } catch (err) {
                const delay = delayAt(COLD_START_DELAYS, attempt++)
                console.warn(`[signalr] попытка #${attempt} не удалась (${(performance.now() - t0).toFixed(0)}ms), повтор через ${delay}ms`, err)
                await sleep(delay)
            }
        }
    } finally {
        connectLoopRunning = false
    }
}

// Ждём живое соединение перед вызовом метода хаба.
async function waitForConnection(timeoutMs: number) {
    const conn = getConnection()
    // Если соединение не поднято — цикл подключения уже работает либо стартует здесь
    // (повторный вызов при живом соединении ничего не делает).
    void connectLoop()

    const deadline = Date.now() + timeoutMs
    for (;;) {
        // Состояние читаем заново на каждой итерации: оно меняется асинхронно,
        // и сужение типа по предыдущей проверке здесь неприменимо.
        const state = conn.state
        if (state === signalR.HubConnectionState.Connected) return conn
        if (Date.now() >= deadline) {
            throw new Error(`[signalr] нет соединения с бэкендом (state=${state})`)
        }
        await plainSleep(100)
    }
}

export function useSignalR() {
    // Поднимает соединение и возвращает его, как только оно установлено.
    // Попытки не прекращаются никогда, поэтому вызов можно и не ждать:
    // обработчики регистрируются через on(), а загрузка данных — через onConnected().
    const startSignalR = async () => {
        const conn = getConnection()
        installWakeListeners()

        if (conn.state === signalR.HubConnectionState.Connected) return conn

        if (!readyPromise) {
            readyPromise = new Promise<signalR.HubConnection>((resolve) => {
                resolveReady = resolve
            })
        }

        void connectLoop()
        return readyPromise
    }

    // Подписка на событие хаба. Обработчики переживают переподключения (объект соединения один),
    // а при размонтировании компонента снимаются сами — без дублей при возврате на страницу.
    const on = (event: string, handler: HubHandler) => {
        const conn = getConnection()
        conn.on(event, handler)
        const off = () => conn.off(event, handler)
        if (getCurrentScope()) onScopeDispose(off)
        return off
    }

    // Колбэк вызывается при каждом (пере)подключении — сюда кладём загрузку состояния с бэкенда,
    // чтобы после перезапуска приложения страница подхватила актуальные настройки.
    const onConnected = (handler: ConnectedHandler, immediate = true) => {
        connectedHandlers.add(handler)
        const off = () => connectedHandlers.delete(handler)
        if (getCurrentScope()) onScopeDispose(off)

        if (immediate && connected.value) {
            try {
                Promise.resolve(handler()).catch(err => console.error('[signalr] onConnected handler failed', err))
            } catch (err) {
                console.error('[signalr] onConnected handler failed', err)
            }
        }
        return off
    }

    const invoke = async (method: string, args: any[], timeoutMs: number) => {
        const conn = await waitForConnection(timeoutMs)
        const t0 = performance.now()
        console.log(`[signalr] -> ${method}${args.length ? `(${args.length} args)` : ''}`)
        try {
            const result = await conn.invoke(method, ...args)
            console.log(`[signalr] <- ${method} took=${(performance.now() - t0).toFixed(0)}ms`)
            return result
        } catch (err) {
            console.error(`[signalr] !! ${method} after ${(performance.now() - t0).toFixed(0)}ms`, err)
            throw err
        }
    }

    const invokeGet = async (method: string, ...args: any[]) => invoke(method, args, 30000)

    const invokeUpdate = async (method: string, ...args: any[]) => {
        await invoke(method, args, 15000)
    }

    return {
        startSignalR,
        invokeGet,
        invokeUpdate,
        on,
        onConnected,
        isConnected: readonly(connected),
    }
}
