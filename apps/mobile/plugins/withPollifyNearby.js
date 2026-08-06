const { withAndroidManifest } = require('@expo/config-plugins');
const permissions=[
  ['android.permission.BLUETOOTH','30'],['android.permission.BLUETOOTH_ADMIN','30'],['android.permission.ACCESS_WIFI_STATE'],['android.permission.CHANGE_WIFI_STATE'],
  ['android.permission.ACCESS_COARSE_LOCATION','28'],['android.permission.ACCESS_FINE_LOCATION','32'],
  ['android.permission.BLUETOOTH_ADVERTISE'],['android.permission.BLUETOOTH_CONNECT'],['android.permission.BLUETOOTH_SCAN'],['android.permission.NEARBY_WIFI_DEVICES']
];
module.exports=function withPollifyNearby(config){ return withAndroidManifest(config,c=>{ const manifest=c.modResults.manifest; manifest['uses-permission']=manifest['uses-permission']||[]; for(const [name,max] of permissions){ if(!manifest['uses-permission'].some(p=>p.$['android:name']===name)){ const attrs={'android:name':name}; if(max) attrs['android:maxSdkVersion']=max; if(name==='android.permission.BLUETOOTH_SCAN'||name==='android.permission.NEARBY_WIFI_DEVICES') attrs['android:usesPermissionFlags']='neverForLocation'; manifest['uses-permission'].push({$:attrs}); } } return c; }); };
