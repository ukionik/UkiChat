import * as signalR from "@microsoft/signalr";

export function useSignalR() {
    let connection: signalR.HubConnection | null = null

    const startSignalR = async () => {
        const t0 = performance.now()
        console.log(`[signalr] building connection to /apphub ...`)
        connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5000/apphub") // сервер WPF
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build()

        connection.onreconnecting((err) => {
            console.warn(`[signalr] onreconnecting: ${err?.message ?? '<no error>'}`)
        })
        connection.onreconnected((id) => {
            console.log(`[signalr] onreconnected connId=${id}`)
        })
        connection.onclose((err) => {
            console.warn(`[signalr] onclose: ${err?.message ?? '<no error>'}`)
        })

        try {
            await connection.start()
            const took = (performance.now() - t0).toFixed(0)
            console.log(`[signalr] connected: state=${connection.state} took=${took}ms`)
        } catch (err) {
            const took = (performance.now() - t0).toFixed(0)
            console.error(`[signalr] connect FAILED after ${took}ms`, err)
            throw err
        }
        return connection;
    }

    const invokeGet = async (method: string) => {
        if (!connection) {
            console.warn(`[signalr] invokeGet(${method}): no connection`)
            return
        }
        const t0 = performance.now()
        console.log(`[signalr] -> ${method}`)
        try {
            const result = await connection.invoke(method)
            const took = (performance.now() - t0).toFixed(0)
            console.log(`[signalr] <- ${method} took=${took}ms`)
            return result
        } catch (err) {
            const took = (performance.now() - t0).toFixed(0)
            console.error(`[signalr] !! ${method} after ${took}ms`, err)
            throw err
        }
    }

    const invokeUpdate = async (method: string, ...args: any[]) => {
        if (!connection) {
            console.warn(`[signalr] invokeUpdate(${method}): no connection`)
            return
        }
        const t0 = performance.now()
        console.log(`[signalr] -> ${method}(${args.length} args)`)
        try {
            await connection.invoke(method, ...args)
            const took = (performance.now() - t0).toFixed(0)
            console.log(`[signalr] <- ${method} took=${took}ms`)
        } catch (err) {
            const took = (performance.now() - t0).toFixed(0)
            console.error(`[signalr] !! ${method} after ${took}ms`, err)
            throw err
        }
    }

    return { startSignalR, invokeGet, invokeUpdate }
}
