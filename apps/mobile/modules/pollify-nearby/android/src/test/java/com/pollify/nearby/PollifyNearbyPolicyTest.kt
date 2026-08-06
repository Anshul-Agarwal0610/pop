package com.pollify.nearby
import org.junit.Assert.*
import org.junit.Test
class PollifyNearbyPolicyTest {
 @Test fun permissionMatrix(){ assertEquals(setOf("android.permission.ACCESS_FINE_LOCATION"),PollifyNearbyPolicy.permissions(30)); assertTrue(PollifyNearbyPolicy.permissions(31).contains("android.permission.BLUETOOTH_SCAN")); assertTrue(PollifyNearbyPolicy.permissions(33).contains("android.permission.NEARBY_WIFI_DEVICES")) }
 @Test fun spoofedAndOversizePayloadsAreRejected(){ assertFalse(PollifyNearbyPolicy.validPayload("{\"version\":1,\"invitationToken\":\"poll-4\"}".toByteArray())); assertFalse(PollifyNearbyPolicy.validPayload(ByteArray(513))); assertTrue(PollifyNearbyPolicy.validPayload("{\"version\":1,\"invitationToken\":\"${"A".repeat(43)}\"}".toByteArray())) }
 @Test fun cleanupIsIdempotent(){ var calls=0; val gate=CleanupGate{calls++}; gate.stop(); gate.stop(); assertEquals(1,calls) }
}
