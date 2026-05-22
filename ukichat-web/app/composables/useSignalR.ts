import * as signalR from "@microsoft/signalr";

export function useSignalR() {
    let connection: signalR.HubConnection | null = null
    const startSignalR = async () => {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5000/apphub") // сервер WPF
            .withAutomaticReconnect()
            .build()

        await connection.start()
        return connection;
    }

    const invokeGet = async (method: string) => {
        if (connection) {
            return await connection.invoke(method)
        }
    }

    const invokeUpdate = async (method: string, ...args: any[]) => {
        if (connection) {
            await connection.invoke(method, ...args)
        }
    }

    return { startSignalR, invokeGet, invokeUpdate }
}