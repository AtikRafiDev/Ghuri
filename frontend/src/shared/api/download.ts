import { isAxiosError } from 'axios'
import { http } from './http'

/**
 * GET a file (a PDF) through the API client - it needs the login token, so
 * a plain <a href> won't do. Save the result with saveFile().
 */
export async function downloadBlob(url: string): Promise<Blob> {
  try {
    const { data } = await http.get<Blob>(url, { responseType: 'blob' })
    return data
  } catch (error) {
    // With responseType 'blob' an error's JSON arrives as a Blob too - turn
    // it back into JSON so toAppError can show the API's own message.
    if (isAxiosError(error) && error.response?.data instanceof Blob) {
      try {
        error.response.data = JSON.parse(await error.response.data.text())
      } catch {
        // not JSON - the generic message will do
      }
    }
    throw error
  }
}
