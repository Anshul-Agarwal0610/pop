package com.pollify.nearby

import android.content.pm.PackageManager
import android.content.Context
import android.bluetooth.BluetoothManager
import android.net.wifi.WifiManager
import androidx.core.content.ContextCompat
import com.google.android.gms.nearby.Nearby
import com.google.android.gms.common.ConnectionResult
import com.google.android.gms.common.GoogleApiAvailability
import com.google.android.gms.nearby.connection.*
import expo.modules.kotlin.modules.Module
import expo.modules.kotlin.modules.ModuleDefinition
import expo.modules.kotlin.Promise
import expo.modules.interfaces.permissions.PermissionsStatus
import java.security.SecureRandom

class PollifyNearbyModule : Module() {
  private val serviceId="com.pollify.app.nearby.v1"
  private val client get()=Nearby.getConnectionsClient(requireNotNull(appContext.reactContext))
  private var selected:String?=null
  private var received=false
  private var gate=CleanupGate { client.stopAllEndpoints(); client.stopAdvertising(); client.stopDiscovery(); selected=null }
  override fun definition()=ModuleDefinition {
    Name("PollifyNearby")
    Events("onEndpointFound","onVerification","onConnected","onPayload","onDisconnected","onError")
    AsyncFunction("capabilities") {
      val context=requireNotNull(appContext.reactContext)
      val missing=PollifyNearbyPolicy.permissions(android.os.Build.VERSION.SDK_INT).filter { ContextCompat.checkSelfPermission(context,it)!=PackageManager.PERMISSION_GRANTED }
      val playServices=GoogleApiAvailability.getInstance().isGooglePlayServicesAvailable(context)==ConnectionResult.SUCCESS
      val bluetooth=try { (context.getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager).adapter?.isEnabled==true } catch (_:SecurityException) { true }
      val wifi=(context.applicationContext.getSystemService(Context.WIFI_SERVICE) as WifiManager).isWifiEnabled
      mapOf("supported" to true,"playServices" to playServices,"radiosAvailable" to (bluetooth&&wifi),"missingPermissions" to missing)
    }
    AsyncFunction("requestPermissions") { promise:Promise ->
      val required=PollifyNearbyPolicy.permissions(android.os.Build.VERSION.SDK_INT).toTypedArray()
      val manager=appContext.permissions
      if(manager==null) promise.reject("permissions_unavailable","Permissions service unavailable",null)
      else manager.askForPermissions({ result -> promise.resolve(required.all { result[it]?.status==PermissionsStatus.GRANTED }) },*required)
    }
    AsyncFunction("startAdvertising") { _:String -> reset(); val label="Pollify ${SecureRandom().nextInt(9000)+1000}"; client.startAdvertising(label,serviceId,lifecycle,AdvertisingOptions.Builder().setStrategy(Strategy.P2P_POINT_TO_POINT).build()) }
    AsyncFunction("startDiscovery") { reset(); client.startDiscovery(serviceId,discovery,DiscoveryOptions.Builder().setStrategy(Strategy.P2P_POINT_TO_POINT).build()) }
    AsyncFunction("selectEndpoint") { id:String -> selected=id; client.stopDiscovery(); client.requestConnection("Pollify player",id,lifecycle) }
    AsyncFunction("confirmVerification") { id:String, accepted:Boolean -> if (id!=selected) throw IllegalArgumentException("unknown endpoint"); if (accepted) client.acceptConnection(id,payload) else client.rejectConnection(id); Unit }
    AsyncFunction("sendPayload") { id:String, json:String -> val bytes=json.toByteArray(); require(PollifyNearbyPolicy.validPayload(bytes)); client.sendPayload(id,Payload.fromBytes(bytes)) }
    AsyncFunction("stop") { gate.stop() }
  }
  private fun reset(){ gate.stop(); received=false; gate=CleanupGate { client.stopAllEndpoints(); client.stopAdvertising(); client.stopDiscovery(); selected=null } }
  private val discovery=object:EndpointDiscoveryCallback(){ override fun onEndpointFound(id:String,info:DiscoveredEndpointInfo){ sendEvent("onEndpointFound",mapOf("endpointId" to id,"label" to info.endpointName)) }; override fun onEndpointLost(id:String){} }
  private val lifecycle=object:ConnectionLifecycleCallback(){ override fun onConnectionInitiated(id:String,info:ConnectionInfo){ selected=id; client.stopAdvertising(); client.stopDiscovery(); sendEvent("onVerification",mapOf("endpointId" to id,"code" to info.authenticationDigits)) }; override fun onConnectionResult(id:String,result:ConnectionResolution){ if(result.status.isSuccess) sendEvent("onConnected",mapOf("endpointId" to id)) else gate.stop() }; override fun onDisconnected(id:String){ gate.stop(); sendEvent("onDisconnected",mapOf("endpointId" to id)) } }
  private val payload=object:PayloadCallback(){ override fun onPayloadReceived(id:String,p:Payload){ if(received){ gate.stop(); return }; val bytes=p.asBytes(); if(p.type!=Payload.Type.BYTES||bytes==null||!PollifyNearbyPolicy.validPayload(bytes)){ gate.stop(); sendEvent("onError",mapOf("code" to "invalid_payload")); return }; received=true; sendEvent("onPayload",mapOf("endpointId" to id,"json" to bytes.toString(Charsets.UTF_8))); gate.stop() }; override fun onPayloadTransferUpdate(id:String,u:PayloadTransferUpdate){} }
}
