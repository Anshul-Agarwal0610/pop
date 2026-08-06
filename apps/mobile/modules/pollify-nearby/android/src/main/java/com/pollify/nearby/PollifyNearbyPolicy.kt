package com.pollify.nearby

object PollifyNearbyPolicy {
  const val MAX_PAYLOAD_BYTES = 512
  fun permissions(api: Int): Set<String> = when {
    api >= 33 -> setOf("android.permission.BLUETOOTH_ADVERTISE","android.permission.BLUETOOTH_CONNECT","android.permission.BLUETOOTH_SCAN","android.permission.NEARBY_WIFI_DEVICES")
    api >= 31 -> setOf("android.permission.BLUETOOTH_ADVERTISE","android.permission.BLUETOOTH_CONNECT","android.permission.BLUETOOTH_SCAN","android.permission.ACCESS_FINE_LOCATION")
    else -> setOf("android.permission.ACCESS_FINE_LOCATION")
  }
  fun validPayload(bytes: ByteArray): Boolean {
    if (bytes.size > MAX_PAYLOAD_BYTES) return false
    val value = bytes.toString(Charsets.UTF_8)
    return Regex("""\{"version":1,"invitationToken":"[A-Za-z0-9_-]{43}"\}""").matches(value)
  }
}

class CleanupGate(private val cleanup: () -> Unit) {
  private var stopped=false
  @Synchronized fun stop() { if (!stopped) { stopped=true; cleanup() } }
}
