import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { apiUrl } from '../config/api'

export interface ChatMensajeDto {
  id: string
  slug: string
  remitenteTipo: 'cliente' | 'admin' | string
  remitenteNombre: string
  texto: string
  enviadoEn: string
}

export function createChatHub(options: {
  token?: string | null
  onMensaje?: (msg: ChatMensajeDto) => void
  onHistorial?: (msgs: ChatMensajeDto[]) => void
}) {
  const connection: HubConnection = new HubConnectionBuilder()
    .withUrl(apiUrl('/hubs/chat'), {
      accessTokenFactory: () => options.token ?? '',
      withCredentials: true,
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()

  if (options.onMensaje) {
    connection.on('MensajeNuevo', options.onMensaje)
  }

  if (options.onHistorial) {
    connection.on('Historial', options.onHistorial)
  }

  return connection
}

