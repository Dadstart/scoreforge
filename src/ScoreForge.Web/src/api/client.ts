const jsonHeaders = {
  Accept: 'application/json',
  'Content-Type': 'application/json',
  'X-Requested-With': 'XMLHttpRequest',
}

async function parseJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let message = response.statusText
    try {
      const body = await response.json()
      message = body.message ?? message
    } catch {
      // ignore
    }
    const error = new Error(message) as Error & { status?: number; body?: unknown }
    error.status = response.status
    throw error
  }

  if (response.status === 204)
    return undefined as T

  return response.json() as Promise<T>
}

export async function apiGet<T>(path: string): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    headers: { Accept: 'application/json' },
  })
  return parseJson<T>(response)
}

export async function apiPost<T>(path: string, body?: unknown): Promise<T> {
  const response = await fetch(path, {
    method: 'POST',
    credentials: 'include',
    headers: jsonHeaders,
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  return parseJson<T>(response)
}

export function newClientEventId(): string {
  return crypto.randomUUID()
}
